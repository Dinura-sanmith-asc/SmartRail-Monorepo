import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_api.dart';

const _base = 'https://api.example.test';
const _station = '10000000-0000-0000-0000-000000000001';
const _journey = '45000000-0000-0000-0000-000000000019';

HttpApiClient _client(http.Client transport, {Duration? timeout}) =>
    HttpApiClient(
      baseUri: Uri.parse(_base),
      client: transport,
      timeout: timeout ?? const Duration(seconds: 1),
    );

void main() {
  test(
    'uses exact anonymous station-departure path and encoded date',
    () async {
      http.Request? captured;
      final api = JourneyStatusApi(
        _client(
          MockClient((request) async {
            captured = request;
            return http.Response('[]', 200);
          }),
        ),
      );

      expect(
        await api.getStationDepartures(stationId: _station, date: '2026-10-20'),
        isEmpty,
      );
      expect(captured!.method, 'GET');
      expect(captured!.url.path, '/api/stations/$_station/departures');
      expect(captured!.url.queryParameters, {'date': '2026-10-20'});
      expect(captured!.headers['authorization'], isNull);
    },
  );

  test('uses exact journey snapshot and public updates GET paths', () async {
    final paths = <String>[];
    final api = JourneyStatusApi(
      _client(
        MockClient((request) async {
          paths.add(request.url.path);
          if (request.url.path.endsWith('/updates')) {
            return http.Response('[]', 200);
          }
          return http.Response(
            jsonEncode({
              'journeyInstanceId': _journey,
              'serviceId': _station,
              'scheduleId': _station,
              'scheduleRevision': 1,
              'operationDate': '2026-10-20',
              'firstDepartureOccurrence': '08:30:00',
              'revision': 1,
              'status': 'Scheduled',
              'stops': [],
              'source': 'ScheduledBaseline',
              'publishedAt': null,
            }),
            200,
          );
        }),
      ),
    );

    expect(
      (await api.getJourneySnapshot(journeyInstanceId: _journey))
          .journeyInstanceId,
      _journey,
    );
    expect(await api.getJourneyUpdates(journeyInstanceId: _journey), isEmpty);
    expect(paths, [
      '/api/journeys/$_journey',
      '/api/journeys/$_journey/updates',
    ]);
  });

  test('decodes UTF-8 payloads without changing supplied text', () async {
    final api = JourneyStatusApi(
      _client(
        MockClient(
          (_) async => http.Response.bytes(
            utf8.encode(
              '[{"journeyInstanceId":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1","serviceId":"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1","operationDate":"2026-10-20","stationLocalDepartureDate":"2026-10-20","revision":1,"status":"Scheduled","stationId":"cccccccc-cccc-4ccc-8ccc-ccccccccccc1","stationName":"කොළඹ","stopOrder":1,"scheduledDepartureAt":null,"ambiguity":"none"}]',
            ),
            200,
          ),
        ),
      ),
    );

    final result = await api.getStationDepartures(
      stationId: _station,
      date: '2026-10-20',
    );
    expect(result.single.stationName, 'කොළඹ');
  });

  test('maps documented error status responses to typed failures', () async {
    for (final entry in <int, ApiFailureKind>{
      400: ApiFailureKind.invalidRequest,
      404: ApiFailureKind.notFound,
      503: ApiFailureKind.unavailable,
    }.entries) {
      final api = JourneyStatusApi(
        _client(
          MockClient(
            (_) async => http.Response(
              '{"type":"about:blank","title":"Safe","status":${entry.key}}',
              entry.key,
              headers: {'content-type': 'application/problem+json'},
            ),
          ),
        ),
      );
      await expectLater(
        api.getJourneySnapshot(journeyInstanceId: _journey),
        throwsA(
          isA<ApiFailure>().having(
            (failure) => failure.kind,
            'kind',
            entry.value,
          ),
        ),
      );
    }
  });

  test('maps malformed successful payload to protocol failure', () async {
    final api = JourneyStatusApi(
      _client(MockClient((_) async => http.Response('{broken', 200))),
    );
    await expectLater(
      api.getJourneyUpdates(journeyInstanceId: _journey),
      throwsA(
        isA<ApiFailure>().having(
          (failure) => failure.kind,
          'kind',
          ApiFailureKind.protocol,
        ),
      ),
    );
  });

  test(
    'maps network failure and timeout without sending authorization',
    () async {
      final networkApi = JourneyStatusApi(
        _client(
          MockClient((_) async {
            throw http.ClientException('synthetic offline');
          }),
        ),
      );
      await expectLater(
        networkApi.getJourneyUpdates(journeyInstanceId: _journey),
        throwsA(
          isA<ApiFailure>().having(
            (failure) => failure.kind,
            'kind',
            ApiFailureKind.network,
          ),
        ),
      );

      final timeoutApi = JourneyStatusApi(
        _client(
          MockClient(
            (_) => Future<http.Response>.delayed(
              const Duration(seconds: 1),
              () => http.Response('[]', 200),
            ),
          ),
          timeout: const Duration(milliseconds: 20),
        ),
      );
      await expectLater(
        timeoutApi.getJourneyUpdates(journeyInstanceId: _journey),
        throwsA(
          isA<ApiFailure>().having(
            (failure) => failure.kind,
            'kind',
            ApiFailureKind.timeout,
          ),
        ),
      );
    },
  );
}
