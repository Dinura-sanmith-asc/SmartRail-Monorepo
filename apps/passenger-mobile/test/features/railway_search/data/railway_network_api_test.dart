import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

const _origin = '11111111-1111-4111-8111-111111111111';
const _destination = '22222222-2222-4222-8222-222222222222';
const _resourceId = '33333333-3333-4333-8333-333333333333';

void main() {
  final baseUri = Uri.parse('https://railway.example.test/base');

  test(
    'GET stations uses exact path, JSON accept, and no authorization',
    () async {
      final api = _api(baseUri, (request) async {
        expect(request.method, 'GET');
        expect(
          request.url,
          Uri.parse('https://railway.example.test/api/stations'),
        );
        expect(
          request.headers['accept'],
          'application/json, application/problem+json',
        );
        expect(request.headers.containsKey('authorization'), isFalse);
        return http.Response(
          '{"items":[]}',
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      });
      expect((await api.listStations()).items, isEmpty);
    },
  );

  test(
    'GET route stops encodes the documented identifier path segment',
    () async {
      final api = _api(baseUri, (request) async {
        expect(request.url.path, '/api/routes/$_resourceId/stops');
        return http.Response(
          '{"routeId":"$_resourceId","routeCode":"R","name":"Line","stops":[]}',
          200,
        );
      });
      expect((await api.getRouteStops(_resourceId)).stops, isEmpty);
    },
  );

  test('GET service search sends exact query names and selected full date', () async {
    final api = _api(baseUri, (request) async {
      expect(request.url.path, '/api/services');
      expect(request.url.queryParameters, {
        'origin': _origin,
        'destination': _destination,
        'date': '2026-10-18',
      });
      return http.Response(
        '{"originStationId":"$_origin","destinationStationId":"$_destination","travelDate":"2026-10-18","services":[]}',
        200,
      );
    });
    expect(
      (await api.searchServices(
        origin: _origin,
        destination: _destination,
        date: TravelDate(year: 2026, month: 10, day: 18),
      )).services,
      isEmpty,
    );
  });

  test('GET service detail uses exact path and decodes UTF-8 response bytes', () async {
    final api = _api(baseUri, (request) async {
      expect(request.url.path, '/api/services/$_resourceId');
      final body =
          '{"serviceId":"$_resourceId","serviceCode":"電車","route":{"routeId":"$_origin","routeCode":"R","name":"Line"},"schedule":{"revision":1,"firstDepartureTimeLocal":"05:00:00"}}';
      return http.Response.bytes(
        utf8.encode(body),
        200,
        headers: {'content-type': 'application/json; charset=utf-8'},
      );
    });
    expect((await api.getService(_resourceId)).serviceCode, '電車');
  });

  test(
    '200 empty collections are success and status failures are typed',
    () async {
      final empty = _api(
        baseUri,
        (_) async => http.Response('{"items":[]}', 200),
      );
      expect((await empty.listStations()).items, isEmpty);
      final unavailable = _api(
        baseUri,
        (_) async => http.Response(
          '{}',
          503,
          headers: {'content-type': 'application/problem+json'},
        ),
      );
      await expectLater(
        unavailable.listStations(),
        throwsA(
          isA<ApiFailure>().having(
            (e) => e.kind,
            'kind',
            ApiFailureKind.unavailable,
          ),
        ),
      );
    },
  );

  test('maps HTTP outcomes by endpoint operation', () async {
    Future<http.Response> response(int status) async => http.Response(
      '{"type":"about:blank","title":"Unavailable","status":$status}',
      status,
      headers: {'content-type': 'application/problem+json; charset=utf-8'},
    );
    final search400 = _api(baseUri, (_) => response(400));
    await expectLater(
      search400.searchServices(
        origin: _origin,
        destination: _destination,
        date: TravelDate(year: 2026, month: 10, day: 18),
      ),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.validation,
        ),
      ),
    );
    final detail400 = _api(baseUri, (_) => response(400));
    await expectLater(
      detail400.getService(_resourceId),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.invalidRequest,
        ),
      ),
    );
    final accessDenied = _api(baseUri, (_) => response(403));
    await expectLater(
      accessDenied.listStations(),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.accessDenied,
        ),
      ),
    );
    final missing = _api(baseUri, (_) => response(404));
    await expectLater(
      missing.getRouteStops(_resourceId),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.notFound,
        ),
      ),
    );
    final unexpected = _api(baseUri, (_) => response(500));
    await expectLater(
      unexpected.listStations(),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.unexpected,
        ),
      ),
    );
    final unavailable = _api(baseUri, (_) => response(503));
    await expectLater(
      unavailable.listStations(),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.unavailable,
        ),
      ),
    );
  });

  test('classifies malformed successful JSON as a protocol failure', () async {
    final api = _api(baseUri, (_) async => http.Response('not json', 200));
    await expectLater(
      api.listStations(),
      throwsA(
        isA<ApiFailure>().having(
          (e) => e.kind,
          'kind',
          ApiFailureKind.protocol,
        ),
      ),
    );
  });

  test('classifies transport errors and bounded request timeouts', () async {
    final offline = _api(
      baseUri,
      (_) async => throw http.ClientException('offline'),
    );
    await expectLater(
      offline.listStations(),
      throwsA(
        isA<ApiFailure>().having((e) => e.kind, 'kind', ApiFailureKind.network),
      ),
    );

    final slowClient = HttpApiClient(
      baseUri: baseUri,
      client: MockClient((_) async {
        await Future<void>.delayed(const Duration(milliseconds: 30));
        return http.Response('{"items":[]}', 200);
      }),
      timeout: const Duration(milliseconds: 5),
    );
    await expectLater(
      RailwayNetworkApi(slowClient).listStations(),
      throwsA(
        isA<ApiFailure>().having((e) => e.kind, 'kind', ApiFailureKind.timeout),
      ),
    );
  });
}

RailwayNetworkApi _api(
  Uri baseUri,
  Future<http.Response> Function(http.Request) handler,
) => RailwayNetworkApi(
  HttpApiClient(
    baseUri: baseUri,
    client: MockClient(handler),
    timeout: const Duration(seconds: 2),
  ),
);
