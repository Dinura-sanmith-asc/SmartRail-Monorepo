import 'dart:ui' show Tristate;

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/widgets/async_feedback.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/station_picker.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

void main() {
  const stationId = '11111111-1111-4111-8111-111111111111';
  final station = Station(id: stationId, name: 'Colombo Fort');

  testWidgets('station picker has a descriptive, operable control name', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StationPicker(
            label: 'origin',
            stations: [station],
            selectedStation: station,
            enabled: true,
            onSelected: (_) {},
          ),
        ),
      ),
    );

    expect(
      find.bySemanticsLabel('origin station: Colombo Fort'),
      findsOneWidget,
    );
    final button = tester.widget<OutlinedButton>(find.byType(OutlinedButton));
    expect(button.onPressed, isNotNull);
    semantics.dispose();
  });

  testWidgets('station selection exposes selected state to semantics', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StationPicker(
            label: 'origin',
            stations: [station],
            selectedStation: station,
            enabled: true,
            onSelected: (_) {},
          ),
        ),
      ),
    );
    await tester.tap(find.bySemanticsLabel('origin station: Colombo Fort'));
    await tester.pumpAndSettle();

    final selected = tester.getSemantics(find.byType(ListTile));
    expect(selected.flagsCollection.isButton, isTrue);
    expect(selected.flagsCollection.isSelected, Tristate.isTrue);
    semantics.dispose();
  });

  testWidgets('disabled station picker is announced as unavailable', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: StationPicker(
            label: 'destination',
            stations: [station],
            selectedStation: null,
            enabled: false,
            onSelected: (_) {},
          ),
        ),
      ),
    );

    expect(find.bySemanticsLabel('Select destination station'), findsOneWidget);
    expect(
      tester.widget<OutlinedButton>(find.byType(OutlinedButton)).onPressed,
      isNull,
    );
    semantics.dispose();
  });

  testWidgets('loading and validation feedback use live announcements', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(
          body: Column(
            children: [
              LoadingFeedback(message: 'Loading stations'),
              ValidationFeedback(message: 'Choose an origin station.'),
            ],
          ),
        ),
      ),
    );

    expect(
      tester
          .getSemantics(find.byType(LoadingFeedback))
          .flagsCollection
          .isLiveRegion,
      isTrue,
    );
    semantics.dispose();
    expect(
      tester
          .getSemantics(find.byType(ValidationFeedback))
          .flagsCollection
          .isLiveRegion,
      isTrue,
    );
  });

  testWidgets('station picker remains operable at enlarged text scale', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: MediaQuery(
          data: const MediaQueryData(textScaler: TextScaler.linear(2)),
          child: Scaffold(
            body: StationPicker(
              label: 'origin',
              stations: [station],
              selectedStation: station,
              enabled: true,
              onSelected: (_) {},
            ),
          ),
        ),
      ),
    );

    expect(
      tester.getSize(find.byType(OutlinedButton)).height,
      greaterThanOrEqualTo(48),
    );
    await tester.tap(find.bySemanticsLabel('origin station: Colombo Fort'));
    await tester.pumpAndSettle();
    expect(find.text('Choose origin station'), findsOneWidget);
  });

  testWidgets('search form follows the visible order for keyboard focus', (
    tester,
  ) async {
    final origin = Station(id: stationId, name: 'Colombo Fort');
    final destination = Station(
      id: '22222222-2222-4222-8222-222222222222',
      name: 'Badulla',
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          railwaySearchRepositoryProvider.overrideWithValue(
            _AccessibilityRepository([origin, destination]),
          ),
        ],
        child: const MaterialApp(home: RailwaySearchScreen()),
      ),
    );
    await tester.pumpAndSettle();

    final orderedControls = [
      'Select origin station',
      'Select destination station',
      'Choose travel date',
      'Search services',
    ];
    for (final label in orderedControls) {
      await tester.sendKeyEvent(LogicalKeyboardKey.tab);
      await tester.pump();
      expect(
        Focus.of(
          tester.element(find.text(label)),
          createDependency: false,
        ).hasPrimaryFocus,
        isTrue,
        reason: '$label should be the next focusable search control',
      );
    }
  });
}

class _AccessibilityRepository implements RailwaySearchRepository {
  _AccessibilityRepository(this.stations);
  final List<Station> stations;

  @override
  Future<List<Station>> listStations() async => stations;

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async =>
      throw UnimplementedError();

  @override
  Future<ServiceDetail> getService(String serviceId) async =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) async =>
      throw UnimplementedError();
}
