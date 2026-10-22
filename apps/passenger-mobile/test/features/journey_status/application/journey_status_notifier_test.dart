import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';
const serviceId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1';
const scheduleId = 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1';
const stopId = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1';

JourneyStatusSnapshot snapshot({
  JourneyStatus status = JourneyStatus.scheduled,
}) => JourneyStatusSnapshot(
  journeyInstanceId: journeyId,
  serviceId: serviceId,
  scheduleId: scheduleId,
  scheduleRevision: 1,
  operationDate: '2026-10-19',
  firstDepartureOccurrence: '08:30:00',
  revision: 1,
  status: status,
  stops: [
    JourneyStopStatus(
      journeyStopId: stopId,
      stopOrder: 1,
      stationId: 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1',
      ambiguity: JourneyAmbiguity.none,
      plannedDepartureAt: JourneyTimestamp.parse('2026-10-19T08:30:00+05:30'),
    ),
  ],
  source: JourneyDataSource.scheduledBaseline,
  publishedAt: null,
);

JourneyUpdate update(
  String id,
  int revision, {
  JourneyStatus status = JourneyStatus.running,
}) => JourneyUpdate(
  updateId: id,
  journeyInstanceId: journeyId,
  revision: revision,
  kind: JourneyUpdateKind.status,
  status: status,
  source: JourneyOperationalSource.controllerManual,
  publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
  publicChange: JourneyStatusChange(status),
);

class FakeJourneyRepository implements JourneyStatusRepository {
  FakeJourneyRepository({this.snapshotValue, this.updatesValue = const []});
  JourneyStatusSnapshot? snapshotValue;
  List<JourneyUpdate> updatesValue;
  Object? snapshotError;
  Object? updatesError;
  int snapshotCalls = 0;
  int updatesCalls = 0;
  Future<JourneyStatusSnapshot> Function()? snapshotPending;
  Future<List<JourneyUpdate>> Function()? updatesPending;

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => [];

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) async {
    snapshotCalls++;
    if (snapshotPending != null) return snapshotPending!();
    if (snapshotError != null) throw snapshotError!;
    return snapshotValue ?? snapshot();
  }

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    updatesCalls++;
    if (updatesPending != null) return updatesPending!();
    if (updatesError != null) throw updatesError!;
    return updatesValue;
  }
}

void main() {
  test(
    'loads snapshot and history independently and retains exact response data',
    () async {
      final repository = FakeJourneyRepository(
        snapshotValue: snapshot(),
        updatesValue: [update('ffffffff-ffff-4fff-8fff-fffffffffff1', 4)],
      );
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      addTearDown(container.dispose);
      final subscription = container.listen(
        journeyStatusNotifierProvider(journeyId),
        (_, _) {},
      );
      addTearDown(subscription.close);
      await container
          .read(journeyStatusNotifierProvider(journeyId).notifier)
          .load();
      final state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(state.snapshot, same(repository.snapshotValue));
      expect(state.updates, equals(repository.updatesValue));
      expect(state.snapshot!.publishedAt, isNull);
      expect(state.snapshot!.source, JourneyDataSource.scheduledBaseline);
      expect(state.snapshot!.revision, 1);
      expect(
        state.snapshot!.stops.single.plannedDepartureAt!.rawValue,
        '2026-10-19T08:30:00+05:30',
      );
      expect(state.updates!.single.revision, 4);
      expect(state.snapshot!.status, JourneyStatus.scheduled);
    },
  );

  test(
    'all seven official statuses remain distinct in loaded snapshot',
    () async {
      for (final status in JourneyStatus.values) {
        final repository = FakeJourneyRepository(
          snapshotValue: snapshot(status: status),
        );
        final container = ProviderContainer(
          overrides: [
            journeyStatusRepositoryProvider.overrideWithValue(repository),
          ],
        );
        final subscription = container.listen(
          journeyStatusNotifierProvider(journeyId),
          (_, _) {},
        );
        await container
            .read(journeyStatusNotifierProvider(journeyId).notifier)
            .load();
        expect(
          container
              .read(journeyStatusNotifierProvider(journeyId))
              .snapshot!
              .status,
          status,
        );
        subscription.close();
        container.dispose();
      }
    },
  );

  test(
    'empty successful history differs from failed history and missing snapshot',
    () async {
      final repository = FakeJourneyRepository(
        snapshotValue: snapshot(),
        updatesValue: [],
      );
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      addTearDown(container.dispose);
      final sub = container.listen(
        journeyStatusNotifierProvider(journeyId),
        (_, _) {},
      );
      addTearDown(sub.close);
      await container
          .read(journeyStatusNotifierProvider(journeyId).notifier)
          .load();
      var state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(state.updates, isEmpty);
      expect(state.updatesFailure, isNull);
      expect(state.snapshot, isNotNull);
      repository.updatesError = ApiFailure.network(
        RailwayApiOperation.journeyUpdates,
      );
      await container
          .read(journeyStatusNotifierProvider(journeyId).notifier)
          .retryUpdates();
      state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(state.updates, isEmpty);
      expect(state.updatesFailure, isNotNull);
      expect(state.snapshot, isNotNull);
    },
  );

  test(
    'does not merge a snapshot returned for a different dated journey',
    () async {
      final wrong = JourneyStatusSnapshot(
        journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2',
        serviceId: serviceId,
        scheduleId: scheduleId,
        scheduleRevision: 1,
        operationDate: '2026-10-20',
        firstDepartureOccurrence: '12:00:00',
        revision: 99,
        status: JourneyStatus.cancelled,
        stops: const [],
        source: JourneyDataSource.controllerManual,
        publishedAt: null,
      );
      final repository = FakeJourneyRepository(snapshotValue: wrong);
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      addTearDown(container.dispose);
      final sub = container.listen(
        journeyStatusNotifierProvider(journeyId),
        (_, _) {},
      );
      addTearDown(sub.close);
      await container
          .read(journeyStatusNotifierProvider(journeyId).notifier)
          .load();
      final state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(state.snapshot, isNull);
      expect(state.snapshotFailure?.kind, ApiFailureKind.protocol);
    },
  );
}
