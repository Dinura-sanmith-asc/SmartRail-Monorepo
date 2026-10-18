import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_search_repository_impl.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

const _origin = '11111111-1111-4111-8111-111111111111';
const _destination = '22222222-2222-4222-8222-222222222222';
const _serviceA = '33333333-3333-4333-8333-333333333333';
const _serviceB = '55555555-5555-4555-8555-555555555555';
const _route = '44444444-4444-4444-8444-444444444444';

void main() {
  late List<Uri> requested;
  late RailwaySearchRepository repository;

  setUp(() {
    requested = [];
    final api = RailwayNetworkApi(
      HttpApiClient(
        baseUri: Uri.parse('https://railway.example.test'),
        timeout: const Duration(seconds: 2),
        client: MockClient((request) async {
          requested.add(request.url);
          if (request.url.path == '/api/stations') {
            return http.Response(
              '{"items":[{"stationId":"$_origin","name":"Central"}]}',
              200,
            );
          }
          if (request.url.path == '/api/services' &&
              request.url.queryParameters.isNotEmpty) {
            return http.Response(
              '''{"originStationId":"$_origin","destinationStationId":"$_destination","travelDate":"2026-10-18","services":[
            {"serviceId":"$_serviceB","serviceCode":"B","routeId":"$_route","routeCode":"R","scheduleRevision":1,"origin":{"stationId":"$_origin","stationName":"Central","stopOrder":1,"scheduledAt":"2026-10-18T08:00:00+05:30"},"destination":{"stationId":"$_destination","stationName":"Harbour","stopOrder":2,"scheduledAt":"2026-10-18T10:00:00+05:30"}},
            {"serviceId":"$_serviceA","serviceCode":"A","routeId":"$_route","routeCode":"R","scheduleRevision":2,"origin":{"stationId":"$_origin","stationName":"Central","stopOrder":1,"scheduledAt":"2026-10-18T09:00:00+05:30"},"destination":{"stationId":"$_destination","stationName":"Harbour","stopOrder":2,"scheduledAt":"2026-10-18T11:00:00+05:30"}}]}''',
              200,
            );
          }
          if (request.url.path == '/api/services/$_serviceA') {
            return http.Response(
              '{"serviceId":"$_serviceA","serviceCode":"A","route":{"routeId":"$_route","routeCode":"R","name":"Coastal"},"schedule":{"revision":2,"firstDepartureTimeLocal":"05:15:00"}}',
              200,
            );
          }
          if (request.url.path == '/api/routes/$_route/stops') {
            return http.Response(
              '{"routeId":"$_route","routeCode":"R","name":"Coastal","stops":[{"stopOrder":1,"stationId":"$_origin","stationName":"Central"}]}',
              200,
            );
          }
          return http.Response('{"items":[]}', 200);
        }),
      ),
    );
    repository = RailwaySearchRepositoryImpl(api);
  });

  test('maps stations to immutable domain values', () async {
    final stations = await repository.listStations();
    expect(stations.single.id, _origin);
    expect(stations.single.name, 'Central');
    expect(
      () => stations.add(Station(id: _destination, name: 'Harbour')),
      throwsUnsupportedError,
    );
  });

  test(
    'maps valid search criteria and preserves returned service order',
    () async {
      final result = await repository.search(
        SearchCriteria(
          originStationId: _origin,
          destinationStationId: _destination,
          travelDate: TravelDate(year: 2026, month: 10, day: 18),
        ),
      );
      expect(result.services.map((service) => service.serviceId), [
        _serviceB,
        _serviceA,
      ]);
      expect(
        result.services.first.origin.scheduledAt,
        '2026-10-18T08:00:00+05:30',
      );
      expect(requested.single.queryParameters['date'], '2026-10-18');
    },
  );

  test(
    'rejects incomplete or identical criteria before making a request',
    () async {
      for (final criteria in [
        const SearchCriteria(),
        SearchCriteria(
          originStationId: _origin,
          destinationStationId: _origin,
          travelDate: TravelDate(year: 2026, month: 10, day: 18),
        ),
      ]) {
        await expectLater(repository.search(criteria), throwsArgumentError);
      }
      expect(requested, isEmpty);
    },
  );

  test(
    'maps service details and route stops as independent domain results',
    () async {
      final detail = await repository.getService(_serviceA);
      final stops = await repository.getRouteStops(_route);
      expect(detail.schedule.firstDepartureTimeLocal, '05:15:00');
      expect(stops.stops.single.stopOrder, 1);
      expect(requested, hasLength(2));
    },
  );

  test('a route-stop failure does not prevent a later service-detail read', () async {
    final api = RailwayNetworkApi(
      HttpApiClient(
        baseUri: Uri.parse('https://railway.example.test'),
        timeout: const Duration(seconds: 2),
        client: MockClient((request) async {
          if (request.url.path == '/api/routes/$_route/stops') {
            return http.Response(
              '{"type":"about:blank","title":"Unavailable","status":503}',
              503,
            );
          }
          return http.Response(
            '{"serviceId":"$_serviceA","serviceCode":"A","route":{"routeId":"$_route","routeCode":"R","name":"Coastal"},"schedule":{"revision":2,"firstDepartureTimeLocal":"05:15:00"}}',
            200,
          );
        }),
      ),
    );
    final recoverableRepository = RailwaySearchRepositoryImpl(api);
    await expectLater(
      recoverableRepository.getRouteStops(_route),
      throwsA(
        isA<ApiFailure>().having(
          (failure) => failure.kind,
          'kind',
          ApiFailureKind.unavailable,
        ),
      ),
    );
    expect(
      (await recoverableRepository.getService(_serviceA)).serviceCode,
      'A',
    );
  });
}
