import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/scheduled_timestamp.dart';

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
    'anonymous passenger search displays the configured backend response',
    (tester) async {
      // These independent public reads are the comparison baseline; they use
      // the real configured API and a regular HTTP client, never a mock.
      final stations = await api.listStations();
      final origin = stations.items.singleWhere(
        (station) => station.stationId == config.originStationId,
      );
      final destination = stations.items.singleWhere(
        (station) => station.stationId == config.destinationStationId,
      );
      final expected = await api.searchServices(
        origin: config.originStationId,
        destination: config.destinationStationId,
        date: config.travelDate,
      );
      expect(expected.originStationId, config.originStationId);
      expect(expected.destinationStationId, config.destinationStationId);
      expect(expected.travelDate, config.travelDate.toIsoDate());
      expect(
        expected.services,
        isNotEmpty,
        reason: 'Configured seed must include a published matching service.',
      );

      await _openApp(tester, appConfig);
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
      await _waitFor(tester, find.byType(ListView));
      await _waitFor(tester, find.text(expected.services.first.serviceCode));

      expect(
        find.text(
          'Scheduled services for ${origin.name} to ${destination.name} on '
          '${config.travelDate.toIsoDate()}',
        ),
        findsOneWidget,
      );

      // Service and route codes plus both planned calls are passenger-visible
      // identifiers/data from the API response. The service UUID remains a
      // navigation key and is checked through the detail endpoint test.
      for (final choice in expected.services) {
        final resultCard = find.ancestor(
          of: find.text(choice.serviceCode),
          matching: find.byType(Card),
        );
        final originTime = ScheduledTimestamp.parse(choice.origin.scheduledAt);
        final destinationTime = ScheduledTimestamp.parse(
          choice.destination.scheduledAt,
        );
        expect(find.text(choice.serviceCode), findsOneWidget);
        expect(resultCard, findsOneWidget);
        expect(find.text('Route ${choice.routeCode}'), findsWidgets);
        expect(find.text(choice.origin.stationName), findsWidgets);
        expect(find.text(originTime.localDate), findsWidgets);
        expect(
          find.text('${originTime.localTime} ${originTime.offset}'),
          findsWidgets,
        );
        expect(find.text(choice.destination.stationName), findsWidgets);
        expect(find.text(destinationTime.localDate), findsWidgets);
        expect(
          find.text('${destinationTime.localTime} ${destinationTime.offset}'),
          findsWidgets,
        );
        expect(
          find.descendant(of: resultCard, matching: find.text('next day')),
          destinationTime.isLaterCalendarDayThan(config.travelDate.toIsoDate())
              ? findsOneWidget
              : findsNothing,
        );
      }
    },
  );
}

Future<void> _openApp(WidgetTester tester, AppConfig config) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [appConfigProvider.overrideWithValue(config)],
      child: SmartRailApp(config: config),
    ),
  );
}

Future<void> _selectStation(
  WidgetTester tester, {
  required String label,
  required String name,
}) async {
  await tester.tap(find.text('Select $label station'));
  await tester.pumpAndSettle();
  final searchField = find.byType(TextField);
  await tester.enterText(searchField, name);
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
  final localDate = DateTime(date.year, date.month, date.day);
  final compactDate = MaterialLocalizations.of(tester.element(dateField))
      .formatCompactDate(localDate);
  await tester.enterText(dateField, compactDate);
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
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
