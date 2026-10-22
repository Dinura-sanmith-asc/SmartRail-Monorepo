using Microsoft.EntityFrameworkCore;
using SmartRail.Application.RailwayNetwork.Stations;
using SmartRail.Domain.RailwayNetwork.Entities;
using SmartRail.Infrastructure.RailwayNetwork;

namespace SmartRail.Infrastructure.RailwayNetwork.Persistence;

public sealed class StationImportValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException("Station import was rejected: " + string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

/// <summary>Persists validated station imports and their minimal audit evidence atomically.</summary>
public sealed class StationImportRepository(SmartRailDbContext dbContext) : IStationImportRepository
{
    public async Task<StationImportRun> ApplyAsync(StationImportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Stations);
        var validation = ImportStationsValidator.Validate(request.Stations.ToArray());
        if (!validation.IsValid) throw new StationImportValidationException(validation.Errors);
        if (string.IsNullOrWhiteSpace(request.SourceRevision) || string.IsNullOrWhiteSpace(request.ContentHash) ||
            string.IsNullOrWhiteSpace(request.SubmittedBy) || string.IsNullOrWhiteSpace(request.ApprovedBy))
            throw new ArgumentException("Source revision, content hash, submitting identity and approving identity are required.", nameof(request));

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var existingStations = await dbContext.Stations.ToListAsync(cancellationToken);
        var bySourceKey = existingStations.ToDictionary(station => station.SourceStationKey, StringComparer.Ordinal);
        var existingById = existingStations.ToDictionary(station => station.StationId);
        var resultingStationIds = existingStations.Select(station => station.StationId).ToHashSet();

        foreach (var input in request.Stations)
        {
            if (bySourceKey.ContainsKey(input.SourceStationKey.Trim())) continue;
            if (existingById.TryGetValue(input.StationId, out var collision) &&
                !string.Equals(collision.SourceStationKey, input.SourceStationKey.Trim(), StringComparison.Ordinal))
                throw new StationImportValidationException([$"Station ID '{input.StationId}' is already assigned to another source identity."]);
            resultingStationIds.Add(input.StationId);
        }

        await ValidateExistingRailwayReferences(resultingStationIds, cancellationToken);

        var changes = new List<StationChange>();
        var createdCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var runId = Guid.NewGuid();

        foreach (var input in request.Stations)
        {
            var sourceKey = input.SourceStationKey.Trim();
            if (!bySourceKey.TryGetValue(sourceKey, out var station))
            {
                station = new Station(input.StationId, sourceKey, input.Name, input.IsPublished, input.VerifiedDetails);
                dbContext.Stations.Add(station);
                bySourceKey.Add(sourceKey, station);
                existingById.Add(station.StationId, station);
                createdCount++;
                var createdFields = new[] { "station_id", "name", "is_published" }
                    .Concat(input.VerifiedDetails.Keys.Select(key => $"verified_details.{key}"))
                    .ToArray();
                changes.Add(new StationChange(Guid.NewGuid(), runId, station.StationId, StationChangeAction.Created,
                    createdFields, DateTimeOffset.UtcNow, request.ApprovedBy));
                continue;
            }

            var changedFields = new List<string>();
            var mergedDetails = station.VerifiedDetails.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
            foreach (var (key, value) in input.VerifiedDetails)
            {
                if (!mergedDetails.TryGetValue(key, out var oldValue) || !string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    changedFields.Add($"verified_details.{key}");
                    mergedDetails[key] = value;
                }
            }
            if (!string.Equals(station.Name, input.Name.Trim(), StringComparison.Ordinal)) changedFields.Add("name");
            if (station.IsPublished != input.IsPublished) changedFields.Add("is_published");

            if (changedFields.Count == 0)
            {
                unchangedCount++;
                continue;
            }

            if (changedFields.Any(field => field == "name" || field.StartsWith("verified_details.", StringComparison.Ordinal)))
                station.UpdateVerifiedDetails(input.Name, mergedDetails);
            if (station.IsPublished != input.IsPublished)
            {
                if (input.IsPublished) station.Publish();
                else station.Unpublish();
            }
            updatedCount++;
            changes.Add(new StationChange(Guid.NewGuid(), runId, station.StationId, StationChangeAction.Updated,
                changedFields, DateTimeOffset.UtcNow, request.ApprovedBy));
        }

        var completedAt = DateTimeOffset.UtcNow;
        var importRun = new StationImportRun(runId, request.SourceRevision, request.ContentHash,
            request.SubmittedBy, request.ApprovedBy, request.StartedAt, completedAt,
            createdCount, updatedCount, unchangedCount);
        dbContext.StationImportRuns.Add(importRun);
        dbContext.StationChanges.AddRange(changes);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return importRun;
    }

    private async Task ValidateExistingRailwayReferences(HashSet<Guid> resultingStationIds, CancellationToken cancellationToken)
    {
        var routeIds = await dbContext.Routes.Select(route => route.RouteId).ToListAsync(cancellationToken);
        var serviceRouteIds = await dbContext.TrainServices.Select(service => service.RouteId).Distinct().ToListAsync(cancellationToken);
        var referencedRouteIds = await dbContext.RouteStops.Select(stop => stop.RouteId).Distinct().ToListAsync(cancellationToken);
        var referencedStationIds = await dbContext.RouteStops.Select(stop => stop.StationId).Distinct().ToListAsync(cancellationToken);
        if (serviceRouteIds.Any(routeId => !routeIds.Contains(routeId)) || referencedRouteIds.Any(routeId => !routeIds.Contains(routeId)))
            throw new StationImportValidationException(["The current railway data contains a service or route-stop reference to an unknown route."]);
        if (referencedStationIds.Any(stationId => !resultingStationIds.Contains(stationId)))
            throw new StationImportValidationException(["The import would leave a route stop referencing an unknown station."]);
    }
}
