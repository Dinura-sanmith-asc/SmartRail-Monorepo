import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';

void main() {
  testWidgets('search route loads public stations without auth redirect', (
    tester,
  ) async {
    final repository = _NavigationRepository(
      stations: [Station(id: 'central', name: 'Central')],
    );
    final router = GoRouter(
      initialLocation: '/railway/search',
      routes: [
        GoRoute(
          path: '/railway/search',
          builder: (context, state) => const RailwaySearchScreen(),
        ),
        GoRoute(
          path: '/sign-in',
          builder: (context, state) =>
              const Scaffold(body: Text('Unexpected sign in route')),
        ),
      ],
    );
    addTearDown(router.dispose);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          railwaySearchRepositoryProvider.overrideWithValue(repository),
        ],
        child: MaterialApp.router(routerConfig: router),
      ),
    );
    await tester.pumpAndSettle();
    expect(router.routeInformationProvider.value.uri.path, '/railway/search');
    expect(find.text('Select origin station'), findsOneWidget);
    expect(find.text('Unexpected sign in route'), findsNothing);
    expect(repository.stationCalls, 1);
    expect(repository.searchCalls, 0);
  });

  testWidgets(
    'retry restores the selector state and leaves search route stable',
    (tester) async {
      final repository = _NavigationRepository(failFirstStationRead: true);
      final router = GoRouter(
        initialLocation: '/railway/search',
        routes: [
          GoRoute(
            path: '/railway/search',
            builder: (context, state) => const RailwaySearchScreen(),
          ),
        ],
      );
      addTearDown(router.dispose);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            railwaySearchRepositoryProvider.overrideWithValue(repository),
          ],
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Retry'), findsOneWidget);
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();
      expect(router.routeInformationProvider.value.uri.path, '/railway/search');
      expect(find.text('No published stations are available.'), findsOneWidget);
      expect(find.text('Loading stations'), findsNothing);
      expect(repository.stationCalls, 2);
    },
  );
}

class _NavigationRepository implements RailwaySearchRepository {
  _NavigationRepository({
    this.stations = const [],
    this.failFirstStationRead = false,
  });

  final List<Station> stations;
  final bool failFirstStationRead;
  int stationCalls = 0;
  int searchCalls = 0;

  @override
  Future<List<Station>> listStations() async {
    stationCalls++;
    if (failFirstStationRead && stationCalls == 1) {
      throw Exception('unavailable');
    }
    return stations;
  }

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async {
    searchCalls++;
    throw UnimplementedError();
  }

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
