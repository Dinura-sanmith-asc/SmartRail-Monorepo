import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/seen_journey_update_tracker.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';

JourneyUpdate update({
  required int revision,
  required JourneyUpdateKind kind,
  required JourneyStatus status,
  required JourneyPublicChange change,
  String? correctionOf,
}) => JourneyUpdate(
  updateId: 'ffffffff-ffff-4fff-8fff-fffffffffff$revision',
  journeyInstanceId: journeyId,
  revision: revision,
  kind: kind,
  status: status,
  source: JourneyOperationalSource.controllerManual,
  publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
  correctionOfUpdateId: correctionOf,
  publicChange: change,
);

void main() {
  test(
    'classifies supported status, stop, diversion and correction changes',
    () {
      final delayed = update(
        revision: 2,
        kind: JourneyUpdateKind.status,
        status: JourneyStatus.delayed,
        change: const JourneyStatusChange(JourneyStatus.delayed),
      );
      final cancelled = update(
        revision: 3,
        kind: JourneyUpdateKind.status,
        status: JourneyStatus.cancelled,
        change: const JourneyStatusChange(JourneyStatus.cancelled),
      );
      final platform = update(
        revision: 4,
        kind: JourneyUpdateKind.stopChange,
        status: JourneyStatus.running,
        change: JourneyStopChange(
          journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
          platform: '4',
        ),
      );
      final operationalTime = update(
        revision: 8,
        kind: JourneyUpdateKind.stopChange,
        status: JourneyStatus.running,
        change: JourneyStopChange(
          journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
          operationalDepartureAt: JourneyTimestamp.parse(
            '2026-10-19T09:15:00+05:30',
          ),
        ),
      );
      final diversion = update(
        revision: 5,
        kind: JourneyUpdateKind.routeDiversion,
        status: JourneyStatus.running,
        change: JourneyRouteDiversion([
          JourneyDivertedStop(
            journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee2',
            stationId: 'cccccccc-cccc-4ccc-8ccc-ccccccccccc2',
            stopOrder: 2,
          ),
        ]),
      );
      final correction = update(
        revision: 6,
        kind: JourneyUpdateKind.correction,
        status: JourneyStatus.delayed,
        correctionOf: 'ffffffff-ffff-4fff-8fff-fffffffffff4',
        change: JourneyCorrectionChange(
          JourneyStopChange(
            journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
            operationalArrivalAt: JourneyTimestamp.parse(
              '2026-10-19T10:20:00+05:30',
            ),
          ),
        ),
      );
      for (final item in [
        delayed,
        cancelled,
        platform,
        operationalTime,
        diversion,
        correction,
      ]) {
        expect(JourneyUpdateAlerts.classify(item), isNotNull);
        expect(JourneyUpdateAlerts.classify(item)!.updateId, item.updateId);
        expect(JourneyUpdateAlerts.classify(item)!.revision, item.revision);
      }
    },
  );

  test('routine status values and unsupported public details do not generate notices', () {
    final scheduled = update(
      revision: 2,
      kind: JourneyUpdateKind.status,
      status: JourneyStatus.running,
      change: const JourneyStatusChange(JourneyStatus.running),
    );
    expect(JourneyUpdateAlerts.classify(scheduled), isNull);
    final other = update(
      revision: 7,
      kind: JourneyUpdateKind.correction,
      status: JourneyStatus.running,
      correctionOf: 'ffffffff-ffff-4fff-8fff-fffffffffff4',
      change: const JourneyCorrectionChange(
        JourneyStatusChange(JourneyStatus.running),
      ),
    );
    expect(JourneyUpdateAlerts.classify(other), isNull);
  });

  test('no read occurs without view entry or manual refresh; failed read does not advance baseline', () async {
    final first = update(
      revision: 2,
      kind: JourneyUpdateKind.status,
      status: JourneyStatus.running,
      change: const JourneyStatusChange(JourneyStatus.running),
    );
    final newDelayed = update(
      revision: 3,
      kind: JourneyUpdateKind.status,
      status: JourneyStatus.delayed,
      change: const JourneyStatusChange(JourneyStatus.delayed),
    );
    final repository = _AlertRepository(first);
    final container = ProviderContainer(
      overrides: [
        journeyStatusRepositoryProvider.overrideWithValue(repository),
        seenJourneyUpdateTrackerProvider.overrideWithValue(
          SeenJourneyUpdateTracker(),
        ),
      ],
    );
    final sub = container.listen(
      journeyStatusNotifierProvider(journeyId),
      (_, _) {},
    );
    final notifier = container.read(
      journeyStatusNotifierProvider(journeyId).notifier,
    );
    await Future<void>.delayed(Duration.zero);
    expect(repository.historyCalls, 0);
    await notifier.load();
    expect(repository.historyCalls, 1);
    repository.failHistory = true;
    await notifier.retryUpdates();
    expect(repository.historyCalls, 2);
    repository.failHistory = false;
    repository.history = [first, newDelayed];
    await notifier.retryUpdates();
    expect(repository.historyCalls, 3);
    final state = container.read(journeyStatusNotifierProvider(journeyId));
    expect(state.newAlerts.map((alert) => alert.updateId), [
      newDelayed.updateId,
    ]);
    await notifier.retryUpdates();
    expect(
      container.read(journeyStatusNotifierProvider(journeyId)).newAlerts,
      isEmpty,
    );
    sub.close();
    container.dispose();
  });
}

class _AlertRepository implements JourneyStatusRepository {
  _AlertRepository(this.initial);
  final JourneyUpdate initial;
  List<JourneyUpdate> history = [];
  int historyCalls = 0;
  bool failHistory = false;
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
    serviceId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1',
    scheduleId: 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1',
    scheduleRevision: 1,
    operationDate: '2026-10-19',
    firstDepartureOccurrence: '08:00:00',
    revision: 3,
    status: JourneyStatus.delayed,
    stops: const [],
    source: JourneyDataSource.controllerManual,
    publishedAt: null,
  );
  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    historyCalls++;
    if (failHistory) throw StateError('synthetic read failure');
    if (history.isEmpty) history = [initial];
    return List.of(history);
  }
}
