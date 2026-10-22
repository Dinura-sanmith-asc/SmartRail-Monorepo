import 'journey_api_dtos.dart';
import 'journey_status_api.dart';
import '../domain/journey_models.dart';
import '../domain/journey_status_repository.dart';
import '../domain/journey_timestamp.dart';

class JourneyStatusRepositoryImpl implements JourneyStatusRepository {
  const JourneyStatusRepositoryImpl(this._api);

  final JourneyStatusApiClient _api;

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => List.unmodifiable(
    (await _api.getStationDepartures(
      stationId: stationId,
      date: date,
    )).map(_mapCandidate),
  );

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) async => _mapSnapshot(
    await _api.getJourneySnapshot(journeyInstanceId: journeyInstanceId),
  );

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async => List.unmodifiable(
    (await _api.getJourneyUpdates(journeyInstanceId: journeyInstanceId))
        .map(_mapUpdate),
  );

  JourneyCandidate _mapCandidate(StationDepartureDto dto) => JourneyCandidate(
    journeyInstanceId: dto.journeyInstanceId,
    serviceId: dto.serviceId,
    operationDate: dto.operationDate,
    stationLocalDepartureDate: dto.stationLocalDepartureDate,
    revision: dto.revision,
    status: JourneyStatus.parse(dto.status),
    stationId: dto.stationId,
    stopOrder: dto.stopOrder,
    scheduledDepartureAt: _timestamp(dto.scheduledDepartureAt),
    ambiguity: JourneyAmbiguity.parse(dto.ambiguity),
    stationName: dto.stationName,
    operationalDepartureAt: _timestamp(dto.operationalDepartureAt),
    platform: dto.platform,
    source: dto.source == null ? null : JourneyDataSource.parse(dto.source!),
    updatedAt: _timestamp(dto.updatedAt),
  );

  JourneyStatusSnapshot _mapSnapshot(JourneySnapshotDto dto) =>
      JourneyStatusSnapshot(
        journeyInstanceId: dto.journeyInstanceId,
        serviceId: dto.serviceId,
        scheduleId: dto.scheduleId,
        scheduleRevision: dto.scheduleRevision,
        operationDate: dto.operationDate,
        firstDepartureOccurrence: dto.firstDepartureOccurrence,
        revision: dto.revision,
        status: JourneyStatus.parse(dto.status),
        stops: dto.stops.map(_mapStop).toList(),
        source: JourneyDataSource.parse(dto.source),
        publishedAt: _timestamp(dto.publishedAt),
        lastUpdatedAt: _timestamp(dto.lastUpdatedAt),
        eta: dto.eta == null ? null : _mapEstimate(dto.eta!),
      );

  JourneyStopStatus _mapStop(JourneyStopDto dto) => JourneyStopStatus(
    journeyStopId: dto.journeyStopId,
    stopOrder: dto.stopOrder,
    stationId: dto.stationId,
    ambiguity: JourneyAmbiguity.parse(dto.ambiguity),
    stationName: dto.stationName,
    plannedArrivalAt: _timestamp(dto.plannedArrivalAt),
    plannedDepartureAt: _timestamp(dto.plannedDepartureAt),
    operationalArrivalAt: _timestamp(dto.operationalArrivalAt),
    operationalDepartureAt: _timestamp(dto.operationalDepartureAt),
    platform: dto.platform,
    timingSource: dto.timingSource,
    timingUpdatedAt: _timestamp(dto.timingUpdatedAt),
  );

  JourneyEstimate _mapEstimate(JourneyEstimateDto dto) => JourneyEstimate(
    value: JourneyTimestamp.parse(dto.value),
    source: JourneyDataSource.parse(dto.source),
    updatedAt: JourneyTimestamp.parse(dto.updatedAt),
    freshness: JourneyEstimateFreshness.parse(dto.freshness),
  );

  JourneyUpdate _mapUpdate(JourneyUpdateDto dto) => JourneyUpdate(
    updateId: dto.updateId,
    journeyInstanceId: dto.journeyInstanceId,
    revision: dto.revision,
    kind: JourneyUpdateKind.parse(dto.kind),
    status: JourneyStatus.parse(dto.status),
    source: JourneyOperationalSource.parse(dto.source),
    publishedAt: JourneyTimestamp.parse(dto.publishedAt),
    correctionOfUpdateId: dto.correctionOfUpdateId,
    publicChange: _mapChange(dto.publicChange),
  );

  JourneyPublicChange _mapChange(PublicChangeDto dto) =>
      switch (dto.changeType) {
        'status' => JourneyStatusChange(JourneyStatus.parse(dto.status!)),
        'stopChange' => JourneyStopChange(
          journeyStopId: dto.journeyStopId!,
          platform: dto.platform,
          operationalArrivalAt: _timestamp(dto.operationalArrivalAt),
          operationalDepartureAt: _timestamp(dto.operationalDepartureAt),
        ),
        'routeDiversion' => JourneyRouteDiversion(
          dto.divertedStops.map(_mapDivertedStop).toList(),
        ),
        'correction' => JourneyCorrectionChange(
          _mapChange(dto.correctedChange!),
        ),
        _ => throw FormatException('Unknown public change type.'),
      };

  JourneyDivertedStop _mapDivertedStop(PublicDivertedStopDto dto) =>
      JourneyDivertedStop(
        journeyStopId: dto.journeyStopId,
        stationId: dto.stationId,
        stopOrder: dto.stopOrder,
        scheduledArrivalAt: _timestamp(dto.scheduledArrivalAt),
        scheduledDepartureAt: _timestamp(dto.scheduledDepartureAt),
        operationalArrivalAt: _timestamp(dto.operationalArrivalAt),
        operationalDepartureAt: _timestamp(dto.operationalDepartureAt),
        platform: dto.platform,
      );

  JourneyTimestamp? _timestamp(String? value) =>
      value == null ? null : JourneyTimestamp.parse(value);
}
