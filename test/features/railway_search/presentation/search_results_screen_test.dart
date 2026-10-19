import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_notifier.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/search_results_screen.dart';

void main() {
  testWidgets('shows backend result order and scheduled values', (
    tester,
  ) async {
    final repository = _Repository()
      ..result = _response([
        _choice('SR-14', '2026-10-20T00:30:00+05:30'),
        _choice('SR-22', '2026-10-19T21:30:00+05:30'),
      ]);
    await _pump(tester, repository);

    expect(find.text('Search results'), findsOneWidget);
    expect(find.text('SR-14'), findsOneWidget);
    expect(find.text('SR-22'), findsOneWidget);
    expect(
      tester.getTopLeft(find.text('SR-14')).dy,
      lessThan(tester.getTopLeft(find.text('SR-22')).dy),
    );
    expect(find.text('next day'), findsOneWidget);
    expect(find.text('No services found'), findsNothing);
  });

  testWidgets('shows a successful empty collection as no results', (
    tester,
  ) async {
    final repository = _Repository()..result = _response([]);
    await _pump(tester, repository);

    expect(find.text('No services found'), findsOneWidget);
    expect(
      find.textContaining('There are no published services'),
      findsOneWidget,
    );
    expect(find.text('Try again'), findsNothing);
    expect(find.text('Change search'), findsOneWidget);
  });

  testWidgets('keeps validation distinct and offers correction, not retry', (
    tester,
  ) async {
    final repository = _Repository()
      ..error = const ApiFailure(
        kind: ApiFailureKind.validation,
        operation: RailwayApiOperation.serviceSearch,
        statusCode: 400,
      );
    await _pump(tester, repository);

    expect(
      find.text('Check the search details and try again.'),
      findsOneWidget,
    );
    expect(find.text('Search again'), findsNothing);
    expect(find.text('Change search'), findsOneWidget);
  });

  testWidgets('offers an explicit retry after unavailable backend response', (
    tester,
  ) async {
    final repository = _Repository()
      ..error = const ApiFailure(
        kind: ApiFailureKind.unavailable,
        operation: RailwayApiOperation.serviceSearch,
        statusCode: 503,
      );
    await _pump(tester, repository);

    expect(
      find.text(
        'Railway information is temporarily unavailable. Please try again.',
      ),
      findsOneWidget,
    );
    expect(find.text('Search again'), findsOneWidget);
    repository.result = _response([
      _choice('RETRY', '2026-10-19T12:00:00+05:30'),
    ]);
    await tester.tap(find.text('Search again'));
    await tester.pumpAndSettle();
    expect(repository.searchCalls, 2);
    expect(find.text('RETRY'), findsOneWidget);
  });

  testWidgets('opens selected service with its id and selected travel date', (
    tester,
  ) async {
    final repository = _Repository()
      ..result = _response([_choice('SR-14', '2026-10-20T00:30:00+05:30')]);
    final container = _container(repository);
    unawaited(
      container.read(searchResultsNotifierProvider.notifier).submit(_criteria),
    );
    final router = GoRouter(
      initialLocation: '/results',
      routes: [
        GoRoute(
          name: 'railway-search',
          path: '/search',
          builder: (_, _) => const Scaffold(body: Text('Search screen')),
        ),
        GoRoute(
          name: 'railway-results',
          path: '/results',
          builder: (_, _) => const SearchResultsScreen(),
        ),
        GoRoute(
          name: 'railway-service-details',
          path: '/services/:serviceId',
          builder: (_, state) {
            final selection =
                state.extra!
                    as ({ScheduledServiceChoice choice, TravelDate travelDate});
            return Scaffold(
              body: Text(
                '${state.pathParameters['serviceId']}|${selection.choice.serviceCode}|${selection.travelDate}',
              ),
            );
          },
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
    await tester.tap(find.text('SR-14'));
    await tester.pumpAndSettle();

    expect(find.text('SR-14|SR-14|2026-10-19'), findsOneWidget);
    router.dispose();
    container.dispose();
  });

  testWidgets(
    'keeps Back available while request is loading and ignores late response',
    (tester) async {
      final repository = _Repository();
      final container = _container(repository);
      unawaited(
        container
            .read(searchResultsNotifierProvider.notifier)
            .submit(_criteria),
      );
      final router = GoRouter(
        initialLocation: '/results',
        routes: [
          GoRoute(
            name: 'railway-search',
            path: '/search',
            builder: (_, _) => const Scaffold(body: Text('Search screen')),
          ),
          GoRoute(
            name: 'railway-results',
            path: '/results',
            builder: (_, _) => const SearchResultsScreen(),
          ),
        ],
      );
      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await tester.pump();
      expect(find.text('Searching scheduled services'), findsOneWidget);
      expect(find.byTooltip('Back'), findsOneWidget);
      router.go('/search');
      await tester.pumpAndSettle();
      repository.pending.single.complete(
        _response([_choice('LATE', '2026-10-19T12:00:00+05:30')]),
      );
      await tester.pumpAndSettle();
      expect(find.text('Search screen'), findsOneWidget);
      expect(find.text('LATE'), findsNothing);
      router.dispose();
      container.dispose();
    },
  );
}

Future<void> _pump(WidgetTester tester, _Repository repository) async {
  final container = _container(repository);
  unawaited(
    container.read(searchResultsNotifierProvider.notifier).submit(_criteria),
  );
  final router = GoRouter(
    initialLocation: '/results',
    routes: [
      GoRoute(
        name: 'railway-search',
        path: '/search',
        builder: (_, _) => const Scaffold(body: Text('Search screen')),
      ),
      GoRoute(
        name: 'railway-results',
        path: '/results',
        builder: (_, _) => const SearchResultsScreen(),
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
  addTearDown(router.dispose);
  addTearDown(container.dispose);
}

final _criteria = SearchCriteria(
  originStationId: 'origin',
  destinationStationId: 'destination',
  travelDate: TravelDate(year: 2026, month: 10, day: 19),
);

ProviderContainer _container(_Repository repository) {
  final container = ProviderContainer(
    overrides: [railwaySearchRepositoryProvider.overrideWithValue(repository)],
  );
  container.listen(searchResultsNotifierProvider, (previous, next) {});
  return container;
}

ServiceSearchResponse _response(List<ScheduledServiceChoice> services) =>
    ServiceSearchResponse(
      originStationId: 'origin',
      destinationStationId: 'destination',
      travelDate: TravelDate(year: 2026, month: 10, day: 19),
      services: services,
    );

ScheduledServiceChoice _choice(String code, String destinationAt) =>
    ScheduledServiceChoice(
      serviceId: code,
      serviceCode: code,
      routeId: 'route-1',
      routeCode: 'R1',
      scheduleRevision: 2,
      origin: const ScheduledCall(
        stationId: 'origin',
        stationName: 'Central',
        stopOrder: 1,
        scheduledAt: '2026-10-19T23:55:00+05:30',
      ),
      destination: ScheduledCall(
        stationId: 'destination',
        stationName: 'Harbour',
        stopOrder: 4,
        scheduledAt: destinationAt,
      ),
    );

class _Repository implements RailwaySearchRepository {
  ServiceSearchResponse? result;
  Object? error;
  int searchCalls = 0;
  final List<Completer<ServiceSearchResponse>> pending = [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) {
    searchCalls++;
    if (error case final value?) {
      error = null;
      return Future.error(value);
    }
    if (result case final value?) {
      result = null;
      return Future.value(value);
    }
    final completer = Completer<ServiceSearchResponse>();
    pending.add(completer);
    return completer.future;
  }

  @override
  Future<List<Station>> listStations() async => const [];
  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();
  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
