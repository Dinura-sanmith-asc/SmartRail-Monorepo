import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_status_screen.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';
const serviceId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1';

class ScreenRepository implements JourneyStatusRepository {
  ScreenRepository(this.status);
  final JourneyStatus status;
  bool failUpdates = false;
  List<JourneyUpdate> updates = const [];
  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => [];
  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) async => JourneyStatusSnapshot(
    journeyInstanceId: journeyId,
    serviceId: serviceId,
    scheduleId: 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1',
    scheduleRevision: 1,
    operationDate: '2026-10-19',
    firstDepartureOccurrence: '08:30:00',
    revision: 4,
    status: status,
    stops: [
      JourneyStopStatus(
        journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
        stopOrder: 1,
        stationId: 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1',
        stationName: 'Central Station',
        ambiguity: JourneyAmbiguity.none,
        plannedDepartureAt: JourneyTimestamp.parse('2026-10-19T08:30:00+05:30'),
        operationalDepartureAt: JourneyTimestamp.parse(
          '2026-10-19T08:35:00+05:30',
        ),
      ),
    ],
    source: JourneyDataSource.scheduledBaseline,
    publishedAt: null,
  );
  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    if (failUpdates) throw StateError('synthetic history failure');
    return updates;
  }
}

void main() {
  testWidgets('shows every official status distinctly as text', (tester) async {
    for (final status in JourneyStatus.values) {
      await tester.pumpWidget(
        ProviderScope(
          key: ValueKey(status),
          overrides: [
            journeyStatusRepositoryProvider.overrideWithValue(
              ScreenRepository(status),
            ),
          ],
          child: const MaterialApp(
            home: JourneyStatusScreen(journeyInstanceId: journeyId),
          ),
        ),
      );
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));
      expect(find.text(status.apiValue), findsOneWidget);
      expect(find.textContaining('ScheduledBaseline'), findsOneWidget);
      expect(
        find.textContaining('no operational publication time'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Estimated arrival unavailable'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Planned departure 2026-10-19T08:30:00+05:30'),
        findsOneWidget,
      );
      expect(
        find.textContaining('Operational departure 2026-10-19T08:35:00+05:30'),
        findsOneWidget,
      );
    }
  });

  testWidgets(
    'manual refresh retains history on partial failure and offers section retry',
    (tester) async {
      final repository = ScreenRepository(JourneyStatus.running);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            journeyStatusRepositoryProvider.overrideWithValue(repository),
          ],
          child: const MaterialApp(
            home: JourneyStatusScreen(journeyInstanceId: journeyId),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('No published updates.'), findsOneWidget);
      repository.failUpdates = true;
      await tester.tap(find.byTooltip('Refresh journey information'));
      await tester.pumpAndSettle();
      expect(find.text('No published updates.'), findsOneWidget);
      expect(
        find.text('Not refreshed · showing previously retrieved updates'),
        findsOneWidget,
      );
      expect(find.text('Updates could not be refreshed.'), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);
    },
  );
}
