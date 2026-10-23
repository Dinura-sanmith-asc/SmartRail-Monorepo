import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_api_dtos.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

import 'integration_test_config.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  late IntegrationTestConfig config;
  late AppConfig appConfig;
  late http.Client verificationClient;
  late RailwayNetworkApi api;

  setUpAll(() {
    config = IntegrationTestConfig.fromEnvironment();
    appConfig = AppConfig.fromValue(config.apiBaseUri.toString());
    verificationClient = http.Client();
    api = RailwayNetworkApi(
      HttpApiClient(baseUri: config.apiBaseUri, client: verificationClient),
    );
  });

  tearDownAll(() => verificationClient.close());

  testWidgets(
    'anonymous passenger details match the selected backend service and route',
    (tester) async {
      final stations = await api.listStations();
      final origin = stations.items.singleWhere(
        (station) => station.stationId == config.originStationId,
      );
      final destination = stations.items.singleWhere(
        (station) => station.stationId == config.destinationStationId,
      );
      final searchResponse = await api.searchServices(
        origin: config.originStationId,
        destination: config.destinationStationId,
        date: config.travelDate,
      );
      expect(searchResponse.services, isNotEmpty);
      final choice = searchResponse.services.first;
      final expectedDetail = await api.getService(choice.serviceId);
      final expectedStops = await api.getRouteStops(choice.routeId);
      expect(expectedDetail.serviceId, choice.serviceId);
      expect(expectedDetail.route.routeId, choice.routeId);
      expect(expectedStops.routeId, choice.routeId);

      await tester.pumpWidget(
        ProviderScope(
          overrides: [appConfigProvider.overrideWithValue(appConfig)],
          child: SmartRailApp(config: appConfig),
        ),
      );
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('home-search-action')));
      await tester.pumpAndSettle();
      await _waitFor(tester, find.text('Select origin station'));
      await _selectStation(tester, label: 'origin', name: origin.name);
      await _selectStation(
        tester,
        label: 'destination',
        name: destination.name,
      );
      await _selectDate(tester, config.travelDate);
      await tester.tap(find.text('Search services'));
      await tester.pump();
      await _waitFor(tester, find.text(choice.serviceCode));
      await tester.tap(find.text(choice.serviceCode).first);

      await _waitFor(tester, find.text('Scheduled service details'));
      await _waitFor(tester, find.text('Planned route stops'));
      await _waitForDetails(tester, expectedStops.stops);

      expect(find.text('Service ${expectedDetail.serviceCode}'), findsWidgets);
      expect(
        find.text('Route ${expectedDetail.route.routeCode}'),
        findsWidgets,
      );
      expect(find.text(expectedDetail.route.name), findsOneWidget);
      expect(
        find.text(
          'Planned first-call departure: '
          '${expectedDetail.schedule.firstDepartureTimeLocal}',
        ),
        findsOneWidget,
      );
      expect(
        find.text('Travel date: ${config.travelDate.toIsoDate()}'),
        findsOneWidget,
      );

      if (expectedStops.stops.isEmpty) {
        expect(
          find.text('No published stop details are available.'),
          findsOneWidget,
        );
      } else {
        expect(
          find.text('${expectedStops.name} · ${expectedStops.routeCode}'),
          findsOneWidget,
        );
        final renderedStops = tester
            .widgetList<ListTile>(find.byType(ListTile))
            .map((tile) => (tile.title! as Text).data!)
            .toList();
        expect(
          renderedStops,
          expectedStops.stops.map((stop) => stop.stationName).toList(),
          reason: 'The details view must preserve API stop order.',
        );
        for (final stop in expectedStops.stops) {
          expect(find.text(stop.stationName), findsWidgets);
        }
      }

      // Independent partial failures cannot be induced safely by this live
      // contract test. Their retry/retention behavior belongs to deterministic
      // repository/controller tests; both real public reads are verified here.
    },
  );
}

Future<void> _selectStation(
  WidgetTester tester, {
  required String label,
  required String name,
}) async {
  await tester.tap(find.text('Select $label station'));
  await tester.pumpAndSettle();
  await tester.enterText(find.byType(TextField), name);
  await tester.pumpAndSettle();
  await tester.tap(find.text(name).last);
  await tester.pumpAndSettle();
}

Future<void> _selectDate(WidgetTester tester, TravelDate date) async {
  await tester.tap(find.text('Choose travel date'));
  await tester.pumpAndSettle();
  final switchToInput = find.byTooltip('Switch to input');
  if (switchToInput.evaluate().isNotEmpty) {
    await tester.tap(switchToInput);
    await tester.pumpAndSettle();
  }
  final dateField = find.byType(TextField).last;
  final compactDate = MaterialLocalizations.of(tester.element(dateField))
      .formatCompactDate(DateTime(date.year, date.month, date.day));
  await tester.enterText(dateField, compactDate);
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
}

Future<void> _waitForDetails(
  WidgetTester tester,
  List<RouteStopDto> stops,
) async {
  final noStops = find.text('No published stop details are available.');
  for (var attempt = 0; attempt < 160; attempt++) {
    if ((stops.isEmpty && noStops.evaluate().isNotEmpty) ||
        tester.widgetList<ListTile>(find.byType(ListTile)).length >=
            stops.length) {
      return;
    }
    await tester.pump(const Duration(milliseconds: 250));
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 10)),
    );
  }
  fail('Timed out waiting for live route-stop response.');
}

Future<void> _waitFor(WidgetTester tester, Finder finder) async {
  for (var attempt = 0; attempt < 160 && finder.evaluate().isEmpty; attempt++) {
    await tester.pump(const Duration(milliseconds: 250));
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 10)),
    );
  }
  expect(finder, findsWidgets, reason: 'Timed out waiting for $finder');
}
