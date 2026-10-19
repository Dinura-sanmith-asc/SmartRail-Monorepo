using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartRail.Domain.Identity;
using SmartRail.IntegrationTests.Identity;
using SmartRail.IntegrationTests.RailwayNetwork;

namespace SmartRail.IntegrationTests.Incidents;

[Collection(IncidentPostgresCollection.Name)]
public sealed class IncidentReadPrivacyTests(PostgreSqlTestDatabaseFixture database, LocalOidcFixture oidc)
{
    [Fact]
    public async Task Response_and_captured_logs_exclude_private_identity_audit_and_credentials()
    {
        var logs = new CapturingLoggerProvider();
        await using var factory = await IncidentApiTestHost.CreateAsync(database, oidc, services =>
        {
            IncidentCreationTestSupport.ConfigureValidTestAuthorities(services);
            services.AddSingleton<ILoggerProvider>(logs);
        });
        using var client = factory.CreateClient();
        const string subject = "private-reporter-f004-read-" + nameof(Response_and_captured_logs_exclude_private_identity_audit_and_credentials);
        await IncidentCreationTestSupport.GrantRoleAsync(factory.Services, subject, RoleCode.Controller);
        var stationId = await IncidentCreationTestSupport.AddPublishedStationAsync(factory.Services);
        var token = oidc.CreateToken(subject);
        var description = "PRIVATE-DESCRIPTION-DO-NOT-LOG-" + Guid.NewGuid();
        using var created = await client.SendAsync(IncidentCreationTestSupport.Post(stationId.ToString(),
            description: description, token: token, key: "privacy-create-" + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await client.SendAsync(IncidentReadTestSupport.Get(token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var json = body.RootElement.GetRawText();
        Assert.Contains(description, json, StringComparison.Ordinal);
        Assert.DoesNotContain(subject, json, StringComparison.Ordinal);
        Assert.DoesNotContain(token, json, StringComparison.Ordinal);
        Assert.DoesNotContain("reporterSubject", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actorSubject", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auditEvidence", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("associationEvidence", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grantedBySubject", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("claims", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(subject, logs.Output, StringComparison.Ordinal);
        Assert.DoesNotContain(token, logs.Output, StringComparison.Ordinal);
        Assert.DoesNotContain(description, logs.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("password", logs.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", logs.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", logs.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT ", logs.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT ", logs.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", logs.Output, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new();
        public string Output => string.Join(Environment.NewLine, _messages);
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);
        public void Dispose() { }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                messages.Enqueue(formatter(state, exception));
                if (exception is not null) messages.Enqueue(exception.Message);
            }
        }
    }
}
