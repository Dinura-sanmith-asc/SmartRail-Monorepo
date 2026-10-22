import 'journey_models.dart';

abstract interface class JourneyStatusRepository {
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  });

  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  });

  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  });
}
