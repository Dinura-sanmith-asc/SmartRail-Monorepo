using SmartRail.Application.JourneysStatus.Persistence;

namespace SmartRail.Application.JourneysStatus.Initialization;

public interface IJourneyInitializationRepository
{
    Task<JourneyInitializationStoreResult> InitializeAsync(JourneyInitializationRecord record, CancellationToken cancellationToken);
    Task AppendConflictAuditAsync(SmartRail.Domain.JourneysStatus.Entities.JourneyInitializationAudit audit, CancellationToken cancellationToken);
}
