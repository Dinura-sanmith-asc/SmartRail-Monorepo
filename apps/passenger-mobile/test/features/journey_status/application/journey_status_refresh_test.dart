import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';
const serviceId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1';

JourneyStatusSnapshot makeSnapshot(int revision) => JourneyStatusSnapshot(
  journeyInstanceId: journeyId,
  serviceId: serviceId,
  scheduleId: 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1',
  scheduleRevision: 1,
  operationDate: '2026-10-19',
  firstDepartureOccurrence: '08:30:00',
  revision: revision,
  status: JourneyStatus.running,
  stops: const [],
  source: JourneyDataSource.controllerManual,
  publishedAt: null,
);

JourneyUpdate makeUpdate() => JourneyUpdate(
  updateId: 'ffffffff-ffff-4fff-8fff-fffffffffff1',
  journeyInstanceId: journeyId,
  revision: 1,
  kind: JourneyUpdateKind.status,
  status: JourneyStatus.running,
  source: JourneyOperationalSource.controllerManual,
  publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
  publicChange: const JourneyStatusChange(JourneyStatus.running),
);

class ControllableJourneyRepository implements JourneyStatusRepository {
  int snapshotCalls = 0;
  int updateCalls = 0;
  Future<JourneyStatusSnapshot> Function()? snapshotRequest;
  Future<List<JourneyUpdate>> Function()? updateRequest;
  JourneyStatusSnapshot currentSnapshot = makeSnapshot(1);
  List<JourneyUpdate> currentUpdates = const [];
  Object? snapshotError;
  Object? updatesError;

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
    if (snapshotRequest != null) return snapshotRequest!();
    if (snapshotError != null) throw snapshotError!;
    return currentSnapshot;
  }

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    updateCalls++;
    if (updateRequest != null) return updateRequest!();
    if (updatesError != null) throw updatesError!;
    return currentUpdates;
  }
}

ProviderContainer makeContainer(ControllableJourneyRepository repo) =>
    ProviderContainer(
      overrides: [journeyStatusRepositoryProvider.overrideWithValue(repo)],
    );

void main() {
  test('full refresh reads both independent sections and suppresses duplicate calls', () async {
    final repo = ControllableJourneyRepository();
    final snapshotGate = Completer<JourneyStatusSnapshot>();
    final updateGate = Completer<List<JourneyUpdate>>();
    repo.snapshotRequest = () => snapshotGate.future;
    repo.updateRequest = () => updateGate.future;
    final container = makeContainer(repo);
    addTearDown(container.dispose);
    final sub = container.listen(
      journeyStatusNotifierProvider(journeyId),
      (_, _) {},
    );
    addTearDown(sub.close);
    final notifier = container.read(
      journeyStatusNotifierProvider(journeyId).notifier,
    );
    final first = notifier.refresh();
    await Future<void>.delayed(Duration.zero);
    final duplicate = notifier.refresh();
    expect(repo.snapshotCalls, 1);
    expect(repo.updateCalls, 1);
    snapshotGate.complete(makeSnapshot(2));
    updateGate.complete(const []);
    await Future.wait([first, duplicate]);
    final state = container.read(journeyStatusNotifierProvider(journeyId));
    expect(state.snapshot!.revision, 2);
    expect(state.updates, isEmpty);
    expect(state.snapshotStatus, JourneyReadStatus.ready);
    expect(state.updatesStatus, JourneyReadStatus.ready);
  });

  test(
    'partial refresh failure retains healthy sibling and retry is section-only',
    () async {
      final repo = ControllableJourneyRepository()..currentUpdates = const [];
      final container = makeContainer(repo);
      addTearDown(container.dispose);
      final sub = container.listen(
        journeyStatusNotifierProvider(journeyId),
        (_, _) {},
      );
      addTearDown(sub.close);
      final notifier = container.read(
        journeyStatusNotifierProvider(journeyId).notifier,
      );
      await notifier.load();
      repo.snapshotError = ApiFailure.network(
        RailwayApiOperation.journeySnapshot,
      );
      await notifier.refresh();
      var state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(state.snapshot, isNotNull);
      expect(state.snapshotMetadata!.notRefreshed, isTrue);
      expect(state.updates, isEmpty);
      expect(state.updatesStatus, JourneyReadStatus.ready);
      final snapshotCalls = repo.snapshotCalls;
      final updateCalls = repo.updateCalls;
      repo.snapshotError = null;
      repo.currentSnapshot = makeSnapshot(3);
      await notifier.retrySnapshot();
      state = container.read(journeyStatusNotifierProvider(journeyId));
      expect(repo.snapshotCalls, snapshotCalls + 1);
      expect(repo.updateCalls, updateCalls);
      expect(state.snapshot!.revision, 3);
      expect(state.snapshotMetadata!.notRefreshed, isFalse);
    },
  );

  test('independently pending reads do not hide a completed sibling or revision mismatch', () async {
    final repo = ControllableJourneyRepository()
      ..currentUpdates = [makeUpdate()];
    final gate = Completer<JourneyStatusSnapshot>();
    repo.snapshotRequest = () => gate.future;
    final container = makeContainer(repo);
    addTearDown(container.dispose);
    final sub = container.listen(
      journeyStatusNotifierProvider(journeyId),
      (_, _) {},
    );
    addTearDown(sub.close);
    final pending = container
        .read(journeyStatusNotifierProvider(journeyId).notifier)
        .refresh();
    await Future<void>.delayed(Duration.zero);
    var state = container.read(journeyStatusNotifierProvider(journeyId));
    expect(state.isSnapshotPending, isTrue);
    expect(state.updatesStatus, JourneyReadStatus.ready);
    expect(state.updates!.single.revision, 1);
    gate.complete(makeSnapshot(2));
    await pending;
    state = container.read(journeyStatusNotifierProvider(journeyId));
    expect(state.hasRevisionMismatch, isTrue);
  });

  test('dispose during pending reads ignores their late completions', () async {
    final repo = ControllableJourneyRepository();
    final snapshotGate = Completer<JourneyStatusSnapshot>();
    final updateGate = Completer<List<JourneyUpdate>>();
    repo.snapshotRequest = () => snapshotGate.future;
    repo.updateRequest = () => updateGate.future;
    final container = makeContainer(repo);
    final sub = container.listen(
      journeyStatusNotifierProvider(journeyId),
      (_, _) {},
    );
    final notifier = container.read(
      journeyStatusNotifierProvider(journeyId).notifier,
    );
    final pending = notifier.load();
    await Future<void>.delayed(Duration.zero);
    sub.close();
    container.dispose();
    snapshotGate.complete(makeSnapshot(9));
    updateGate.complete(const []);
    await pending;
  });
}
