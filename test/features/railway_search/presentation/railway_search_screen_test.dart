import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';

void main() {
  testWidgets('shows initial selectors, loads stations, and stays anonymous', (
    tester,
  ) async {
    final repository = _WidgetRepository(
      stations: [
        Station(id: 'origin', name: 'Central'),
        Station(id: 'destination', name: 'Harbour'),
      ],
    );
    await _pumpScreen(tester, repository);

    expect(find.text('Search railway services'), findsOneWidget);
    expect(find.text('Loading stations'), findsOneWidget);
    await tester.pumpAndSettle();
    expect(find.text('Select origin station'), findsOneWidget);
    expect(find.text('Select destination station'), findsOneWidget);
    expect(find.text('Choose travel date'), findsOneWidget);
    expect(find.text('Sign in'), findsNothing);
    expect(repository.searchCalls, 0);
  });

  testWidgets('shows empty and unavailable station states with safe retry', (
    tester,
  ) async {
    final empty = _WidgetRepository();
    await _pumpScreen(tester, empty);
    await tester.pumpAndSettle();
    expect(find.text('No published stations are available.'), findsOneWidget);

    final failing = _WidgetRepository(
      stations: [
        Station(id: 'origin', name: 'Central'),
        Station(id: 'destination', name: 'Harbour'),
      ],
      failFirstStationRead: true,
    );
    await _pumpScreen(tester, failing);
    await tester.pumpAndSettle();
    expect(
      find.text(
        'Railway information is temporarily unavailable. Please try again.',
      ),
      findsOneWidget,
    );
    expect(find.textContaining('private backend diagnostic'), findsNothing);
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('Select origin station'), findsOneWidget);
    expect(failing.stationCalls, 2);
    expect(failing.searchCalls, 0);
  });

  testWidgets(
    'selects different published stations and retains calendar date',
    (tester) async {
      final repository = _WidgetRepository(
        stations: [
          Station(
            id: 'origin',
            name: 'Central',
            locationDetails: ['City centre'],
          ),
          Station(id: 'destination', name: 'Harbour'),
        ],
      );
      await _pumpScreen(tester, repository);
      await tester.pumpAndSettle();

      await tester.tap(find.text('Select origin station'));
      await tester.pumpAndSettle();
      expect(find.text('City centre'), findsOneWidget);
      await tester.enterText(find.byType(TextField), 'Central');
      await tester.pumpAndSettle();
      expect(find.text('Harbour'), findsNothing);
      await tester.tap(find.text('Central').last);
      await tester.pumpAndSettle();
      expect(find.text('origin station: Central'), findsOneWidget);

      await tester.tap(find.text('Select destination station'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Harbour').last);
      await tester.pumpAndSettle();
      expect(find.text('destination station: Harbour'), findsOneWidget);

      await tester.tap(find.text('Choose travel date'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('OK'));
      await tester.pumpAndSettle();
      expect(find.textContaining('Travel date:'), findsOneWidget);
      expect(repository.searchCalls, 0);
    },
  );

  testWidgets('invalid input is explained and never submits a search', (
    tester,
  ) async {
    final repository = _WidgetRepository(
      stations: [
        Station(id: 'origin', name: 'Central'),
        Station(id: 'destination', name: 'Harbour'),
      ],
    );
    await _pumpScreen(tester, repository);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Search services'));
    await tester.pumpAndSettle();
    expect(find.text('Choose an origin station.'), findsOneWidget);
    expect(repository.searchCalls, 0);
  });

  testWidgets('identical origin and destination shows pair validation', (
    tester,
  ) async {
    final repository = _WidgetRepository(
      stations: [
        Station(id: 'origin', name: 'Central'),
        Station(id: 'destination', name: 'Harbour'),
      ],
    );
    await _pumpScreen(tester, repository);
    await tester.pumpAndSettle();

    await tester.tap(find.text('Select origin station'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Central').last);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Select destination station'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Central').last);
    await tester.pumpAndSettle();
    expect(find.text('destination station: Central'), findsOneWidget);
    await tester.tap(find.text('Choose travel date'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('OK'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Search services'));
    await tester.pumpAndSettle();

    expect(
      find.text('Choose different origin and destination stations.'),
      findsOneWidget,
    );
    expect(repository.searchCalls, 0);
  });
}

Future<void> _pumpScreen(
  WidgetTester tester,
  _WidgetRepository repository,
) async {
  await tester.pumpWidget(
    ProviderScope(
      key: ObjectKey(repository),
      overrides: [
        railwaySearchRepositoryProvider.overrideWithValue(repository),
      ],
      child: const MaterialApp(home: RailwaySearchScreen()),
    ),
  );
}

class _WidgetRepository implements RailwaySearchRepository {
  _WidgetRepository({
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
      throw Exception('private backend diagnostic');
    }
    return stations;
  }

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async {
    searchCalls++;
    throw UnimplementedError('Phase 3 must not submit service searches.');
  }

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
