import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:integration_test/integration_test.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_api.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_repository_impl.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_status_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_update_alert.dart';

import 'journey_lookup_integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets(
    'real snapshot and complete update history match the selected journey API responses',
    (tester) async {
      final seed = await readJourneyStatusLiveSeed();
      final selected = seed.candidates.first;
      final client = http.Client();
      late JourneyStatusSnapshot expectedSnapshot;
      late List<JourneyUpdate> expectedUpdates;
      try {
        final repository = JourneyStatusRepositoryImpl(
          JourneyStatusApi(
            HttpApiClient(baseUri: seed.config.apiBaseUri, client: client),
          ),
        );
        expectedSnapshot = await repository.getJourneySnapshot(
          journeyInstanceId: selected.journeyInstanceId,
        );
        expectedUpdates = await repository.getJourneyUpdates(
          journeyInstanceId: selected.journeyInstanceId,
        );
      } finally {
        client.close();
      }

      await launchJourneyLookup(tester, seed);
      await openJourneyStatusForCandidate(tester, seed, selected);
      await waitForJourneyUi(
        tester,
        find.byKey(const ValueKey('journey-status-value')),
      );
      await waitForJourneyUi(
        tester,
        find.text(expectedSnapshot.status.apiValue),
      );
      await waitForJourneyUi(tester, find.text('Published updates'));

      expect(find.byType(JourneyStatusScreen), findsOneWidget);
      expect(find.text(expectedSnapshot.status.apiValue), findsOneWidget);
      expect(
        find.text('Source: ${expectedSnapshot.source.apiValue}'),
        findsOneWidget,
      );
      expect(
        find.text('Revision ${expectedSnapshot.revision}'),
        findsOneWidget,
      );
      if (expectedSnapshot.publishedAt == null) {
        expect(
          find.textContaining('no operational publication time'),
          findsOneWidget,
        );
      } else {
        expect(
          find.text('Published ${expectedSnapshot.publishedAt!.rawValue}'),
          findsOneWidget,
        );
      }
      if (expectedSnapshot.lastUpdatedAt != null) {
        expect(
          find.text('Last updated ${expectedSnapshot.lastUpdatedAt!.rawValue}'),
          findsOneWidget,
        );
      }

      if (expectedSnapshot.eta == null) {
        expect(find.text('Estimated arrival unavailable'), findsOneWidget);
      } else {
        expect(
          find.text(
            'Estimated arrival ${expectedSnapshot.eta!.value.rawValue} · '
            '${expectedSnapshot.eta!.freshness.apiValue}',
          ),
          findsOneWidget,
        );
      }

      final stopPositions = <double>[];
      for (final stop in expectedSnapshot.stops) {
        final stopTitle = '${stop.stopOrder}. ${stop.stationName ?? 'Station'}';
        final titleFinder = find.text(stopTitle);
        expect(titleFinder, findsOneWidget);
        stopPositions.add(tester.getTopLeft(titleFinder).dy);

        _expectDisplayedTimestamp(
          tester,
          'Planned arrival',
          stop.plannedArrivalAt?.rawValue,
        );
        _expectDisplayedTimestamp(
          tester,
          'Planned departure',
          stop.plannedDepartureAt?.rawValue,
        );
        _expectDisplayedTimestamp(
          tester,
          'Operational arrival',
          stop.operationalArrivalAt?.rawValue,
        );
        _expectDisplayedTimestamp(
          tester,
          'Operational departure',
          stop.operationalDepartureAt?.rawValue,
        );
        if (stop.platform != null) {
          expect(
            find.textContaining('Platform ${stop.platform}'),
            findsOneWidget,
          );
        }
        if (stop.timingSource != null) {
          expect(
            find.textContaining('Timing source ${stop.timingSource}'),
            findsOneWidget,
          );
        }
        if (stop.timingUpdatedAt != null) {
          expect(
            find.textContaining(
              'Timing updated ${stop.timingUpdatedAt!.rawValue}',
            ),
            findsOneWidget,
          );
        }
      }
      for (var index = 1; index < stopPositions.length; index++) {
        expect(stopPositions[index], greaterThan(stopPositions[index - 1]));
      }

      if (expectedUpdates.isEmpty) {
        expect(find.text('No published updates.'), findsOneWidget);
      } else {
        final updatePositions = <double>[];
        for (final update in expectedUpdates) {
          final updateIdFinder = find.textContaining(
            'Update ${update.updateId}',
          );
          expect(updateIdFinder, findsOneWidget);
          expect(
            find.textContaining('Revision ${update.revision}'),
            findsWidgets,
          );
          expect(find.textContaining(update.source.apiValue), findsWidgets);
          expect(
            find.textContaining(update.publishedAt.rawValue),
            findsWidgets,
          );
          updatePositions.add(tester.getTopLeft(updateIdFinder).dy);
        }
        for (var index = 1; index < updatePositions.length; index++) {
          expect(
            updatePositions[index],
            greaterThan(updatePositions[index - 1]),
            reason: 'Public updates should remain in returned revision order.',
          );
        }
      }

      // The first successful history read establishes this app-session
      // baseline; historical changes do not become newly observed alerts.
      expect(find.byType(JourneyUpdateAlertBanner), findsNothing);
    },
  );
}

void _expectDisplayedTimestamp(
  WidgetTester tester,
  String label,
  String? rawValue,
) {
  if (rawValue == null) return;
  expect(
    find.textContaining('$label $rawValue'),
    findsOneWidget,
    reason:
        'Displayed scheduled/operational value must match the API verbatim.',
  );
}
