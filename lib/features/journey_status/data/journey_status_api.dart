import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';

import 'journey_api_dtos.dart';

/// Read-only client for the three approved public Journeys & Status operations.
abstract interface class JourneyStatusApiClient {
  Future<List<StationDepartureDto>> getStationDepartures({
    required String stationId,
    required String date,
  });

  Future<JourneySnapshotDto> getJourneySnapshot({
    required String journeyInstanceId,
  });

  Future<List<JourneyUpdateDto>> getJourneyUpdates({
    required String journeyInstanceId,
  });
}

class JourneyStatusApi implements JourneyStatusApiClient {
  JourneyStatusApi(this._client);

  final HttpApiClient _client;

  @override
  Future<List<StationDepartureDto>> getStationDepartures({
    required String stationId,
    required String date,
  }) async {
    _requireUuid(stationId, 'stationId');
    _requireDate(date);
    final body = await _get(
      _client.endpoint(
        '/api/stations/$stationId/departures',
        queryParameters: {'date': date},
      ),
      RailwayApiOperation.stationDepartures,
    );
    return StationDeparturesDto.fromJson(body).items;
  }

  @override
  Future<JourneySnapshotDto> getJourneySnapshot({
    required String journeyInstanceId,
  }) async {
    _requireUuid(journeyInstanceId, 'journeyInstanceId');
    final body = await _get(
      _client.endpoint('/api/journeys/$journeyInstanceId'),
      RailwayApiOperation.journeySnapshot,
    );
    return JourneySnapshotDto.fromJson(body);
  }

  @override
  Future<List<JourneyUpdateDto>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    _requireUuid(journeyInstanceId, 'journeyInstanceId');
    final body = await _get(
      _client.endpoint('/api/journeys/$journeyInstanceId/updates'),
      RailwayApiOperation.journeyUpdates,
    );
    return JourneyUpdatesDto.fromJson(body).items;
  }

  Future<Object?> _get(Uri uri, RailwayApiOperation operation) async {
    try {
      final body = await _client.getText(uri, operation: operation);
      return decodeJourneyJson(body);
    } on ApiFailure {
      rethrow;
    } on FormatException catch (error) {
      throw ApiFailure.protocol(operation, error);
    } on Exception catch (error) {
      throw ApiFailure.protocol(operation, error);
    }
  }

  void _requireUuid(String value, String name) {
    if (!RegExp(
      r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89ABab][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
    ).hasMatch(value)) {
      throw ArgumentError.value(value, name, 'Must be a UUID.');
    }
  }

  void _requireDate(String value) {
    if (!RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(value)) {
      throw ArgumentError.value(value, 'date', 'Must be YYYY-MM-DD.');
    }
    final parts = value.split('-').map(int.parse).toList();
    final parsed = DateTime.utc(parts[0], parts[1], parts[2]);
    if (parsed.year != parts[0] ||
        parsed.month != parts[1] ||
        parsed.day != parts[2]) {
      throw ArgumentError.value(
        value,
        'date',
        'Must be a valid calendar date.',
      );
    }
  }
}
