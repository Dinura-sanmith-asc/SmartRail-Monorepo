using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartRail.Application.Identity.Authorization;
using SmartRail.Application.Identity.Queries;
using SmartRail.Api.Contracts.Identity;
using SmartRail.DevSeeder;
using SmartRail.Domain.Identity;
using SmartRail.Infrastructure.RailwayNetwork;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.DevelopmentSeed;

[Collection("Development seed PostgreSQL")]
public sealed class CognitoTestRoleBootstrapTests(PostgreSqlTestDatabaseFixture database)
{
    private const string Operator = "S-1-5-21-100-200-300-1001";
    // Synthetic test IDs only. Real Cognito subjects are supplied outside Git at execution.
    private static readonly CognitoTestRoleApproval Approval = new(Operator,
        "10000000-aaaa-4000-8000-000000000001", "10000000-aaaa-4000-8000-000000000002", "DEV-COGNITO-TEST-001");

    [Theory]
    [InlineData("")]
    [InlineData("not-a-cognito-sub")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData(" 10000000-aaaa-4000-8000-000000000001")]
    public void Invalid_subject_is_rejected(string value) =>
        Assert.Throws<SeedRefusalException>(() => (Approval with { ControllerSubject = value }).Validate(Operator));

    [Fact]
    public void Duplicate_subjects_and_unapproved_operator_are_rejected()
    {
        Assert.Throws<SeedRefusalException>(() => (Approval with { StationStaffSubject = Approval.ControllerSubject }).Validate(Operator));
        Assert.Throws<SeedRefusalException>(() => Approval.Validate("S-1-5-21-100-200-300-2002"));
    }

    [Fact]
    public void Windows_sid_authorities_are_supported_only_when_the_actual_operator_matches()
    {
        const string syntheticEntraSid = "S-1-12-1-100-200-300-400";
        var approval = Approval with { ApprovedOperatorSid = syntheticEntraSid };
        approval.Validate(syntheticEntraSid);
        Assert.Throws<SeedRefusalException>(() => approval.Validate("S-1-12-1-100-200-300-401"));
        Assert.Throws<SeedRefusalException>(() => approval.Validate(Operator));
    }

    [Theory]
    [InlineData(RoleCode.Driver)]
    [InlineData(RoleCode.Passenger)]
    [InlineData((RoleCode)999)]
    [InlineData(RoleCode.StationStaff)]
    public void Unsupported_or_cross_assigned_role_is_rejected(RoleCode role) =>
        Assert.Throws<SeedRefusalException>(() => Approval.ValidatePair(Approval.ControllerSubject, role));

    [Fact]
    public async Task Unauthorized_targets_are_denied_before_any_database_access()
    {
        await using var provider = CognitoTestRoleBootstrap.CreateServices("Host=unused", Operator, MaintenanceAction.GrantRole);
        await Assert.ThrowsAsync<SeedRefusalException>(() => CognitoTestRoleBootstrap.ExecuteAsync(provider, Approval, Operator,
            "10000000-aaaa-4000-8000-000000000099", RoleCode.Controller, MaintenanceAction.GrantRole));
        await Assert.ThrowsAsync<SeedRefusalException>(() => CognitoTestRoleBootstrap.ExecuteAsync(provider, Approval,
            "S-1-5-21-100-200-300-9999", Approval.ControllerSubject, RoleCode.Controller, MaintenanceAction.GrantRole));
    }

    [Fact]
    public async Task Tool_authority_permits_only_its_operator_and_selected_action()
    {
        var authority = new CognitoTestRoleAuthority("windows:" + Operator, MaintenanceAction.GrantRole);
        Assert.Equal(MaintenanceAuthorityOutcome.Allowed, (await authority.AuthorizeAsync("windows:" + Operator, MaintenanceAction.GrantRole)).Outcome);
        Assert.Equal(MaintenanceAuthorityOutcome.Denied, (await authority.AuthorizeAsync("freely-supplied-actor", MaintenanceAction.GrantRole)).Outcome);
        Assert.Equal(MaintenanceAuthorityOutcome.Denied, (await authority.AuthorizeAsync("windows:" + Operator, MaintenanceAction.GrantStationScope)).Outcome);
        Assert.Equal(MaintenanceAuthorityOutcome.Denied, (await authority.AuthorizeAsync("windows:" + Operator, MaintenanceAction.RevokeRole)).Outcome);
    }

    [Fact]
    public async Task Grant_repeat_revoke_repeat_and_regrant_preserve_audit_and_current_identity_labels()
    {
        var target = new Npgsql.NpgsqlConnectionStringBuilder(database.ConnectionString);
        Assert.Equal(5433, target.Port);
        Assert.StartsWith("smartrail_it_", target.Database);
        await using var db = new SmartRailDbContext(new DbContextOptionsBuilder<SmartRailDbContext>().UseNpgsql(database.ConnectionString).Options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        await using var grant = CognitoTestRoleBootstrap.CreateServices(database.ConnectionString, Operator, MaintenanceAction.GrantRole);
        foreach (var pair in new[] { (Approval.ControllerSubject, RoleCode.Controller), (Approval.StationStaffSubject, RoleCode.StationStaff) })
        {
            Assert.Equal("granted with audit evidence", await Execute(grant, pair.Item1, pair.Item2, MaintenanceAction.GrantRole));
            Assert.Equal("already active (unchanged)", await Execute(grant, pair.Item1, pair.Item2, MaintenanceAction.GrantRole));
        }
        Assert.Equal(2, await db.RoleAssignments.CountAsync());
        var audit = await db.RoleAssignmentAudits.ToArrayAsync();
        Assert.Equal(2, audit.Length);
        Assert.All(audit, item =>
        {
            Assert.Equal("windows:" + Operator, item.ActorSubject);
            Assert.Equal(CognitoTestRoleBootstrap.EvidencePrefix + Approval.ApprovalReference, item.EvidenceReference);
        });
        await using (var scope = grant.CreateAsyncScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<GetCurrentIdentityHandler>();
            var controller = await handler.HandleAsync(new(new VerifiedPrincipal(Approval.ControllerSubject)));
            var staff = await handler.HandleAsync(new(new VerifiedPrincipal(Approval.StationStaffSubject)));
            Assert.Equal(new[] { "Controller" }, controller.Roles.Select(CurrentIdentityResponse.ToExternalRoleLabel));
            Assert.Equal(new[] { "Station Staff" }, staff.Roles.Select(CurrentIdentityResponse.ToExternalRoleLabel));
        }
        await using var revoke = CognitoTestRoleBootstrap.CreateServices(database.ConnectionString, Operator, MaintenanceAction.RevokeRole);
        var wrongApproval = Approval with { ApprovalReference = "OTHER-APPROVAL" };
        await Assert.ThrowsAsync<SeedRefusalException>(() => CognitoTestRoleBootstrap.ExecuteAsync(revoke, wrongApproval, Operator,
            Approval.ControllerSubject, RoleCode.Controller, MaintenanceAction.RevokeRole));
        Assert.Equal("revoked with audit evidence", await Execute(revoke, Approval.ControllerSubject, RoleCode.Controller, MaintenanceAction.RevokeRole));
        Assert.Equal("no active assignment (unchanged)", await Execute(revoke, Approval.ControllerSubject, RoleCode.Controller, MaintenanceAction.RevokeRole));
        Assert.Equal(3, await db.RoleAssignmentAudits.CountAsync());
        await using (var scope = revoke.CreateAsyncScope())
        {
            var current = await scope.ServiceProvider.GetRequiredService<GetCurrentIdentityHandler>().HandleAsync(new(new VerifiedPrincipal(Approval.ControllerSubject)));
            Assert.Empty(current.Roles);
        }
        Assert.Equal("granted with audit evidence", await Execute(grant, Approval.ControllerSubject, RoleCode.Controller, MaintenanceAction.GrantRole));
        Assert.Equal(3, await db.RoleAssignments.CountAsync());
        Assert.Equal(4, await db.RoleAssignmentAudits.CountAsync());
    }

    private static Task<string> Execute(IServiceProvider provider, string subject, RoleCode role, MaintenanceAction action) =>
        CognitoTestRoleBootstrap.ExecuteAsync(provider, Approval, Operator, subject, role, action);
}
