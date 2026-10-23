import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/search_results_screen.dart';

void main() {
  testWidgets('submits valid criteria and clears results immediately on edit', (
    tester,
  ) async {
    final repository = _Repository();
    final container = ProviderContainer(
      overrides: [
        railwaySearchRepositoryProvider.overrideWithValue(repository),
      ],
    );
    final router = GoRouter(
      initialLocation: '/search',
      routes: [
        GoRoute(
          name: 'railway-search',
          path: '/search',
          builder: (_, _) => const RailwaySearchScreen(),
        ),
        GoRoute(
          name: 'railway-results',
          path: '/results',
          builder: (_, _) => const SearchResultsScreen(),
        ),
        GoRoute(
          name: 'railway-service-details',
          path: '/services/:serviceId',
          builder: (_, _) => const Scaffold(body: Text('Details')),
        ),
      ],
    );
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MaterialApp.router(routerConfig: router),
      ),
    );
    await tester.pumpAndSettle();

    await _selectStation(tester, 'Select origin station', 'Central');
    await _selectStation(tester, 'Select destination station', 'Harbour');
    await tester.tap(find.text('Choose travel date'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Search services'));
    await tester.tap(find.text('Search services'));
    await tester.pumpAndSettle();

    expect(repository.searchCalls, 1);
    expect(repository.queries.single.originStationId, 'origin');
    expect(repository.queries.single.destinationStationId, 'destination');
    expect(find.text('SR-14'), findsOneWidget);
    expect(
      container.read(searchResultsNotifierProvider).status,
      SearchResultsStatus.populated,
    );

    router.pop();
    await tester.pumpAndSettle();
    await _selectStation(tester, 'Select destination station', 'Airport');

    final cleared = container.read(searchResultsNotifierProvider);
    expect(cleared.status, SearchResultsStatus.notSearched);
    expect(cleared.response, isNull);
    expect(repository.searchCalls, 1);

    await tester.ensureVisible(find.text('Search services'));
    await tester.tap(find.text('Search services'));
    await tester.pumpAndSettle();
    expect(repository.searchCalls, 2);
    expect(repository.queries.last.destinationStationId, 'airport');
    router.dispose();
    container.dispose();
  });
}

Future<void> _selectStation(
  WidgetTester tester,
  String field,
  String station,
) async {
  final fieldFinder = find.text(field);
  await tester.tap(
    fieldFinder.evaluate().isNotEmpty
        ? fieldFinder
        : find.textContaining('destination station:'),
  );
  await tester.pumpAndSettle();
  await tester.tap(find.text(station).last);
  await tester.pumpAndSettle();
}

class _Repository implements RailwaySearchRepository {
  final List<SearchCriteria> queries = [];
  int searchCalls = 0;

  @override
  Future<List<Station>> listStations() async => [
    Station(id: 'origin', name: 'Central'),
    Station(id: 'destination', name: 'Harbour'),
    Station(id: 'airport', name: 'Airport'),
  ];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async {
    searchCalls++;
    queries.add(criteria);
    return ServiceSearchResponse(
      originStationId: criteria.originStationId!,
      destinationStationId: criteria.destinationStationId!,
      travelDate: criteria.travelDate!,
      services: [
        ScheduledServiceChoice(
          serviceId: 'service-$searchCalls',
          serviceCode: 'SR-14',
          routeId: 'route-1',
          routeCode: 'R1',
          scheduleRevision: 1,
          origin: const ScheduledCall(
            stationId: 'origin',
            stationName: 'Central',
            stopOrder: 1,
            scheduledAt: '2026-10-19T23:55:00+05:30',
          ),
          destination: const ScheduledCall(
            stationId: 'destination',
            stationName: 'Harbour',
            stopOrder: 4,
            scheduledAt: '2026-10-20T00:30:00+05:30',
          ),
        ),
      ],
    );
  }

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();
  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
