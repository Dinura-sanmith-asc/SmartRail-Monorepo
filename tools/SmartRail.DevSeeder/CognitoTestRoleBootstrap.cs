using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Maintenance;
using SmartRail.Application.Identity.Persistence;
using SmartRail.Domain.Identity;
using SmartRail.Domain.Identity.Entities;
using SmartRail.Infrastructure;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.DevSeeder;

internal sealed record CognitoTestRoleApproval(string ApprovedOperatorSid, string ControllerSubject,
    string StationStaffSubject, string ApprovalReference)
{
    internal void Validate(string actualOperatorSid)
    {
        if (string.IsNullOrWhiteSpace(ApprovedOperatorSid) ||
            !Regex.IsMatch(ApprovedOperatorSid, @"^S-1-\d+(?:-\d+)+$", RegexOptions.CultureInvariant) ||
            !string.Equals(ApprovedOperatorSid, actualOperatorSid, StringComparison.Ordinal))
            throw new SeedRefusalException("The current OS operator is not approved by this local execution manifest.");
        if (!IsSubject(ControllerSubject) || !IsSubject(StationStaffSubject) ||
            string.Equals(ControllerSubject, StationStaffSubject, StringComparison.OrdinalIgnoreCase))
            throw new SeedRefusalException("Exactly two distinct, non-empty Cognito user-pool sub UUIDs are required.");
        if (ApprovalReference is null || !Regex.IsMatch(ApprovalReference, @"^[A-Za-z0-9][A-Za-z0-9._/-]{0,99}$", RegexOptions.CultureInvariant))
            throw new SeedRefusalException("A development-test approval reference of 1–100 safe characters is required.");
    }

    internal void ValidatePair(string subject, RoleCode role)
    {
        if (!((role == RoleCode.Controller && subject == ControllerSubject) ||
            (role == RoleCode.StationStaff && subject == StationStaffSubject)))
            throw new SeedRefusalException("This target subject/role pair is not approved for this local bootstrap.");
    }

    private static bool IsSubject(string? value) => value is not null &&
        Guid.TryParseExact(value, "D", out var id) && id != Guid.Empty && value.Length == 36;
}

internal sealed class CognitoTestRoleAuthority(string actor, MaintenanceAction allowedAction) : IIdentityMaintenanceAuthority
{
    public Task<MaintenanceAuthorityDecision> AuthorizeAsync(string verifiedActorSubject, MaintenanceAction action, CancellationToken cancellationToken = default) =>
        Task.FromResult(new MaintenanceAuthorityDecision(verifiedActorSubject == actor && action == allowedAction
            ? MaintenanceAuthorityOutcome.Allowed : MaintenanceAuthorityOutcome.Denied));
}

internal static class CognitoTestRoleBootstrap
{
    internal const string EvidencePrefix = "local-cognito-test-bootstrap:";

    internal static async Task<int> RunCliAsync(string[] args)
    {
        if (args.Length != 5 || args[2] != "--approval-file" || args[4] != "--confirm-local-cognito-test-roles" ||
            args[1] is not ("grant" or "revoke"))
            throw new SeedRefusalException("Use --cognito-test-roles grant|revoke --approval-file <external JSON file> --confirm-local-cognito-test-roles. No --actor or arbitrary role option is supported.");
        SeedSafety.ValidateEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"),
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
        if (!OperatingSystem.IsWindows()) throw new SeedRefusalException("This isolated tool requires a verified Windows process identity.");
        var operatorSid = ReadWindowsOperatorSid();
        if (new FileInfo(args[3]).Length > 8192) throw new SeedRefusalException("Approval manifest is too large.");
        var approval = JsonSerializer.Deserialize<CognitoTestRoleApproval>(await File.ReadAllTextAsync(args[3]),
            new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
            ?? throw new SeedRefusalException("Approval manifest is missing.");
        approval.Validate(operatorSid);
        await using var configured = new SmartRailDbContextFactory().CreateDbContext([]);
        var connection = configured.Database.GetConnectionString()!;
        SeedSafety.ValidateTarget(connection);
        var action = args[1] == "grant" ? MaintenanceAction.GrantRole : MaintenanceAction.RevokeRole;
        await using var services = CreateServices(connection, operatorSid, action);
        foreach (var (subject, role) in new[] { (approval.ControllerSubject, RoleCode.Controller), (approval.StationStaffSubject, RoleCode.StationStaff) })
        {
            var outcome = await ExecuteAsync(services, approval, operatorSid, subject, role, action);
            Console.WriteLine($"{(role == RoleCode.StationStaff ? "Station Staff" : "Controller")}: {outcome}");
        }
        return 0;
    }

    [SupportedOSPlatform("windows")]
    private static string ReadWindowsOperatorSid()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return identity.User?.Value ?? throw new SeedRefusalException("The OS process user SID cannot be verified.");
    }

    // Only the CLI derives this value from Windows. Tests can inject synthetic SIDs into this internal composition.
    internal static ServiceProvider CreateServices(string connection, string actualOperatorSid, MaintenanceAction action)
    {
        if (action is not (MaintenanceAction.GrantRole or MaintenanceAction.RevokeRole))
            throw new SeedRefusalException("Only role grants and revocations are supported.");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmartRailInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SmartRailDbContext"] = connection
        }).Build());
        services.RemoveAll<IIdentityMaintenanceAuthority>();
        services.AddSingleton<IIdentityMaintenanceAuthority>(new CognitoTestRoleAuthority("windows:" + actualOperatorSid, action));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    internal static async Task<string> ExecuteAsync(IServiceProvider services, CognitoTestRoleApproval approval,
        string actualOperatorSid, string subject, RoleCode role, MaintenanceAction action)
    {
        approval.Validate(actualOperatorSid);
        approval.ValidatePair(subject, role);
        if (action is not (MaintenanceAction.GrantRole or MaintenanceAction.RevokeRole))
            throw new SeedRefusalException("Unsupported maintenance action.");
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<SmartRailDbContext>();
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new SeedRefusalException("Required migrations are missing. This tool never applies migrations.");
        var actor = "windows:" + actualOperatorSid;
        // Check authority before lookup, including no-op/retry paths.
        var authority = await provider.GetRequiredService<IIdentityMaintenanceAuthority>().AuthorizeAsync(actor, action);
        if (authority.Outcome != MaintenanceAuthorityOutcome.Allowed) throw new SeedRefusalException("Operator execution is not authorized.");
        var assignments = await provider.GetRequiredService<IRoleAssignmentRepository>().GetActiveAssignmentsAsync(subject);
        var active = assignments.SingleOrDefault(item => item.RoleCode == role);
        var evidence = EvidencePrefix + approval.ApprovalReference;
        if (action == MaintenanceAction.GrantRole)
        {
            if (active is not null) return "already active (unchanged)";
            var granted = await provider.GetRequiredService<GrantRoleAssignmentHandler>().HandleAsync(new(actor, subject, role,
                DateTimeOffset.UtcNow, Guid.NewGuid(), Guid.NewGuid(), evidence));
            if (granted.Outcome != MaintenanceCommandOutcome.Completed) throw new SeedRefusalException("Audited role grant did not complete; retry after checking local availability.");
            return "granted with audit evidence";
        }
        if (active is null) return "no active assignment (unchanged)";
        var owned = await db.RoleAssignmentAudits.AsNoTracking().AnyAsync(item => item.RoleAssignmentId == active.Id &&
            item.Action == AssignmentAuditAction.Granted && item.ActorSubject == actor && item.EvidenceReference == evidence);
        if (!owned) throw new SeedRefusalException("Refusing to revoke an assignment outside this operator's approved bootstrap evidence.");
        var revoked = await provider.GetRequiredService<RevokeRoleAssignmentHandler>().HandleAsync(new(actor, active.Id,
            DateTimeOffset.UtcNow, Guid.NewGuid(), evidence));
        if (revoked.Outcome != MaintenanceCommandOutcome.Completed) throw new SeedRefusalException("Audited role revocation did not complete.");
        return "revoked with audit evidence";
    }
}
