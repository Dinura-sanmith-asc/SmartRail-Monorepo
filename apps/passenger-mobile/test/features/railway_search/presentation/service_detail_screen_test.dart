import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_screen.dart';

void main() {
  testWidgets('shows supplied planned timestamps and next-day cue literally', (
    tester,
  ) async {
    await tester.pumpWidget(_app(_Repository(), _choice, _travelDate));
    await tester.pumpAndSettle();

    expect(find.text('Planned schedule'), findsWidgets);
    expect(find.text('2026-10-19 23:30:00 (+05:30)'), findsOneWidget);
    expect(find.text('2026-10-20 08:15:00 (+05:30)'), findsOneWidget);
    expect(find.text('Next day'), findsOneWidget);
    expect(find.text('Planned first-call departure: 22:55:00'), findsOneWidget);
  });

  testWidgets('shows an explicit empty route-stop state', (tester) async {
    await tester.pumpWidget(
      _app(_Repository(emptyStops: true), _choice, _travelDate),
    );
    await tester.pumpAndSettle();
    expect(
      find.text('No published stop details are available.'),
      findsOneWidget,
    );
  });

  testWidgets(
    'partial failure keeps the good section and retries only failed one',
    (tester) async {
      final repository = _Repository(failStops: true);
      await tester.pumpWidget(_app(repository, _choice, _travelDate));
      await tester.pumpAndSettle();
      expect(find.text('Service SR42'), findsOneWidget);
      expect(
        find.text(
          'Railway information is temporarily unavailable. Please try again.',
        ),
        findsOneWidget,
      );
      repository.failStops = false;
      await tester.tap(find.byKey(const Key('retry-route-stops')));
      await tester.pumpAndSettle();
      expect(repository.serviceCalls, 1);
      expect(repository.routeCalls, 2);
      expect(find.text('Kandy'), findsOneWidget);
    },
  );

  testWidgets('service failure keeps ordered stops and retries only service', (
    tester,
  ) async {
    final repository = _Repository(failService: true);
    await tester.pumpWidget(_app(repository, _choice, _travelDate));
    await tester.pumpAndSettle();
    expect(
      find.text(
        'Railway information is temporarily unavailable. Please try again.',
      ),
      findsOneWidget,
    );
    expect(find.byType(ListTile), findsNWidgets(2));
    expect(
      tester.getTopLeft(find.byType(ListTile).first).dy,
      lessThan(tester.getTopLeft(find.byType(ListTile).last).dy),
    );
    await tester.tap(find.byKey(const Key('retry-service-detail')));
    await tester.pumpAndSettle();
    expect(repository.serviceCalls, 2);
    expect(repository.routeCalls, 1);
    expect(find.text('Service SR42'), findsOneWidget);
  });

  testWidgets('labels retained route data not refreshed after failed refresh', (
    tester,
  ) async {
    final repository = _Repository();
    final container = ProviderContainer(
      overrides: [
        railwaySearchRepositoryProvider.overrideWithValue(repository),
        serviceDetailClockProvider.overrideWithValue(
          () => DateTime.utc(2026, 10, 19, 8),
        ),
      ],
    );
    addTearDown(container.dispose);
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp(
          home: ServiceDetailScreen(choice: _choice, travelDate: _travelDate),
        ),
      ),
    );
    await tester.pumpAndSettle();
    repository.failStops = true;
    await container
        .read(
          serviceDetailNotifierProvider((
            choice: _choice,
            travelDate: _travelDate,
          )).notifier,
        )
        .retryStops();
    await tester.pumpAndSettle();
    expect(
      find.text('Not refreshed · retrieved at 2026-10-19T08:00:00.000Z'),
      findsOneWidget,
    );
    expect(find.byType(ListTile), findsNWidgets(2));
    expect(find.byKey(const Key('retry-route-stops')), findsOneWidget);
  });
}

Widget _app(
  RailwaySearchRepository repository,
  ScheduledServiceChoice choice,
  TravelDate travelDate,
) => ProviderScope(
  overrides: [railwaySearchRepositoryProvider.overrideWithValue(repository)],
  child: MaterialApp(
    home: ServiceDetailScreen(choice: choice, travelDate: travelDate),
  ),
);

final _choice = ScheduledServiceChoice(
  serviceId: 'service-id',
  serviceCode: 'SR42',
  routeId: 'route-id',
  routeCode: 'R7',
  scheduleRevision: 3,
  origin: const ScheduledCall(
    stationId: 'origin-id',
    stationName: 'Colombo Fort',
    stopOrder: 1,
    scheduledAt: '2026-10-19T23:30:00+05:30',
  ),
  destination: const ScheduledCall(
    stationId: 'destination-id',
    stationName: 'Badulla',
    stopOrder: 8,
    scheduledAt: '2026-10-20T08:15:00+05:30',
  ),
);
final _travelDate = TravelDate(year: 2026, month: 10, day: 19);

class _Repository implements RailwaySearchRepository {
  _Repository({
    this.failStops = false,
    this.failService = false,
    this.emptyStops = false,
  });
  bool failStops;
  bool failService;
  final bool emptyStops;
  int serviceCalls = 0;
  int routeCalls = 0;

  @override
  Future<ServiceDetail> getService(String serviceId) async {
    serviceCalls++;
    if (failService) throw Exception('offline');
    return ServiceDetail(
      serviceId: serviceId,
      serviceCode: 'SR42',
      route: const ServiceRoute(
        routeId: 'route-id',
        routeCode: 'R7',
        name: 'Main Line',
      ),
      schedule: ServiceSchedule(
        revision: 3,
        firstDepartureTimeLocal: '22:55:00',
      ),
    );
  }

  @override
  Future<RouteStops> getRouteStops(String routeId) async {
    routeCalls++;
    if (failStops) throw Exception('offline');
    return RouteStops(
      routeId: routeId,
      routeCode: 'R7',
      name: 'Main Line',
      stops: emptyStops
          ? const []
          : [
              RouteStop(stopOrder: 2, stationId: 'b', stationName: 'Kandy'),
              RouteStop(
                stopOrder: 1,
                stationId: 'a',
                stationName: 'Colombo Fort',
              ),
            ],
    );
  }

  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();
}
