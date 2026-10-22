import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_api.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_repository_impl.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_status_screen.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_search_repository_impl.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_screen.dart';

import 'journey_status_integration_config.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'real station departures enter through service details and require candidate choice when ambiguous',
    (tester) async {
      final seed = await readJourneyStatusLiveSeed();
      await launchJourneyLookup(tester, seed);

      expect(find.byType(ServiceDetailScreen), findsOneWidget);
      final selected = seed.candidates.first;
      await openJourneyStatusForCandidate(tester, seed, selected);
      expect(
        seed.candidates.map((item) => item.journeyInstanceId),
        contains(selected.journeyInstanceId),
      );
    },
  );
}

/// Opens the Feature 002 screens through their registered routes and selects
/// only the caller-provided live candidate identity.
Future<void> openJourneyStatusForCandidate(
  WidgetTester tester,
  JourneyStatusLiveSeed seed,
  JourneyCandidate selected,
) async {
  await tester.ensureVisible(find.byKey(const Key('open-journey-status')));
  await tester.tap(find.byKey(const Key('open-journey-status')));
  await tester.pumpAndSettle();
  await waitForJourneyUi(
    tester,
    find.byKey(const ValueKey('journey-option-1')),
  );

  final choices = find.byWidgetPredicate(
    (widget) =>
        widget.key is ValueKey<String> &&
        (widget.key! as ValueKey<String>).value.startsWith('journey-option-'),
  );
  expect(choices, findsNWidgets(seed.candidates.length));
  expect(
    appRouter.routeInformationProvider.value.uri.path,
    AppRoutes.journeyLookup,
  );
  expect(find.byType(JourneyStatusScreen), findsNothing);

  final selectedIndex = seed.candidates.indexOf(selected);
  expect(selectedIndex, greaterThanOrEqualTo(0));
  if (seed.candidates.length == 1 &&
      selected.ambiguity == JourneyAmbiguity.none) {
    await tester.tap(find.byKey(const Key('open-journey-candidate')));
  } else {
    await tester.tap(
      find.byKey(ValueKey('journey-option-${selectedIndex + 1}')),
    );
  }
  await tester.pumpAndSettle();
  await waitForJourneyUi(tester, find.byType(JourneyStatusScreen));

  expect(
    appRouter.routeInformationProvider.value.uri.path,
    AppRoutes.journeyStatus.replaceAll(
      ':journeyInstanceId',
      selected.journeyInstanceId,
    ),
  );
}

/// A real backend seed assembled only from public API responses.
class JourneyStatusLiveSeed {
  const JourneyStatusLiveSeed({
    required this.config,
    required this.origin,
    required this.destination,
    required this.choice,
    required this.travelDate,
    required this.candidates,
  });

  final JourneyStatusIntegrationConfig config;
  final Station origin;
  final Station destination;
  final ScheduledServiceChoice choice;
  final TravelDate travelDate;
  final List<JourneyCandidate> candidates;
}

/// Reads real published service, route, and journey candidates. It has no
/// fixture or mocked-data branch; missing or inconsistent seed data fails.
Future<JourneyStatusLiveSeed> readJourneyStatusLiveSeed() async {
  final config = JourneyStatusIntegrationConfig.fromEnvironment();
  final client = http.Client();
  try {
    final httpApi = HttpApiClient(baseUri: config.apiBaseUri, client: client);
    final railwayApi = RailwayNetworkApi(httpApi);
    final travelDate = TravelDate.parse(config.testDate);
    final stations = await railwayApi.listStations();
    final origin = stations.items.singleWhere(
      (station) => station.stationId == config.originStationId,
      orElse: () => throw StateError(
        'Configured origin station is not returned by the real public station API.',
      ),
    );
    final detail = await railwayApi.getService(config.serviceId);
    final route = await railwayApi.getRouteStops(detail.route.routeId);
    final originOrder = route.stops
        .where((stop) => stop.stationId == config.originStationId)
        .map((stop) => stop.stopOrder)
        .firstOrNull;
    if (originOrder == null) {
      throw StateError(
        'Configured origin is not a stop on the configured published service route.',
      );
    }

    final laterStops = route.stops
        .where((stop) => stop.stopOrder > originOrder)
        .toList(growable: false);
    if (laterStops.isEmpty) {
      throw StateError(
        'The configured service has no route stop after its origin.',
      );
    }

    ScheduledServiceChoice? choice;
    Station? destination;
    for (final routeStop in laterStops) {
      final search = await RailwaySearchRepositoryImpl(railwayApi).search(
        SearchCriteria(
          originStationId: config.originStationId,
          destinationStationId: routeStop.stationId,
          travelDate: travelDate,
        ),
      );
      final matches = search.services
          .where((service) => service.serviceId == config.serviceId)
          .toList(growable: false);
      if (matches.isNotEmpty) {
        choice = matches.first;
        destination = Station(
          id: routeStop.stationId,
          name: routeStop.stationName,
        );
        break;
      }
    }
    if (choice == null || destination == null) {
      throw StateError(
        'No real published search result matched configured service/date and a later route stop; no integration fixture was substituted.',
      );
    }

    final statusRepository = JourneyStatusRepositoryImpl(
      JourneyStatusApi(httpApi),
    );
    final response = await statusRepository.getStationDepartures(
      stationId: config.originStationId,
      date: travelDate.toIsoDate(),
    );
    final matching = response
        .where(
          (candidate) =>
              candidate.serviceId == config.serviceId &&
              candidate.stationLocalDepartureDate == travelDate.toIsoDate() &&
              candidate.stationId == config.originStationId,
        )
        .toList(growable: false);
    if (matching.isEmpty) {
      throw StateError(
        'No real station-departure candidate matched configured service/date/origin; no journey was synthesized.',
      );
    }

    return JourneyStatusLiveSeed(
      config: config,
      origin: Station(id: origin.stationId, name: origin.name),
      destination: destination,
      choice: choice,
      travelDate: travelDate,
      candidates: matching,
    );
  } finally {
    client.close();
  }
}

/// Boots the production app and router, then drives Feature 001 search to the
/// actual service details route using real station and service API results.
Future<void> launchJourneyLookup(
  WidgetTester tester,
  JourneyStatusLiveSeed seed,
) async {
  final appConfig = AppConfig.fromValue(seed.config.apiBaseUri.toString());
  if (!appConfig.isConfigured) {
    throw StateError('Configured real API URL was rejected by AppConfig.');
  }

  await tester.pumpWidget(
    ProviderScope(
      overrides: [appConfigProvider.overrideWithValue(appConfig)],
      child: SmartRailApp(config: appConfig),
    ),
  );
  appRouter.go(AppRoutes.search);
  await tester.pumpAndSettle();
  await waitForJourneyUi(tester, find.text('Select origin station'));
  await _selectStation(tester, label: 'origin', name: seed.origin.name);
  await _selectStation(
    tester,
    label: 'destination',
    name: seed.destination.name,
  );
  await _selectDate(tester, seed.travelDate);

  await tester.tap(find.text('Search services'));
  await tester.pump();
  await waitForJourneyUi(tester, find.text(seed.choice.serviceCode));
  await tester.tap(find.text(seed.choice.serviceCode));
  await tester.pumpAndSettle();
  await waitForJourneyUi(tester, find.byType(ServiceDetailScreen));
}

Future<void> waitForJourneyUi(WidgetTester tester, Finder finder) async {
  for (var attempt = 0; attempt < 120 && finder.evaluate().isEmpty; attempt++) {
    await tester.pump(const Duration(milliseconds: 250));
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 10)),
    );
  }
  expect(
    finder,
    findsWidgets,
    reason:
        'Timed out waiting for real-backend UI $finder; no mock fallback is used.',
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
  final localDate = DateTime(date.year, date.month, date.day);
  final compactDate = MaterialLocalizations.of(tester.element(dateField))
      .formatCompactDate(localDate);
  await tester.enterText(dateField, compactDate);
  await tester.pumpAndSettle();
  await tester.tap(find.text('OK'));
  await tester.pumpAndSettle();
}
