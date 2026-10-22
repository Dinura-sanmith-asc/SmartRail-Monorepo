import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_state.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';
import 'package:smartrail_passenger/features/journey_status/application/seen_journey_update_tracker.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_read_metadata.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';

typedef JourneyStatusClock = DateTime Function();

final journeyStatusClockProvider = Provider<JourneyStatusClock>(
  (ref) => DateTime.now,
);
final seenJourneyUpdateTrackerProvider = Provider<SeenJourneyUpdateTracker>(
  (ref) => SeenJourneyUpdateTracker(),
);

final journeyStatusNotifierProvider = NotifierProvider.autoDispose
    .family<JourneyStatusNotifier, JourneyStatusState, String>(
      JourneyStatusNotifier.new,
    );

class JourneyStatusNotifier extends Notifier<JourneyStatusState> {
  JourneyStatusNotifier(this.journeyInstanceId);

  final String journeyInstanceId;
  late JourneyStatusRepository _repository;
  late JourneyStatusClock _clock;
  late SeenJourneyUpdateTracker _tracker;
  int _snapshotGeneration = 0;
  int _updatesGeneration = 0;
  bool _snapshotPending = false;
  bool _updatesPending = false;

  @override
  JourneyStatusState build() {
    _repository = ref.watch(journeyStatusRepositoryProvider);
    _clock = ref.watch(journeyStatusClockProvider);
    _tracker = ref.watch(seenJourneyUpdateTrackerProvider);
    ref.onDispose(() {
      _snapshotGeneration++;
      _updatesGeneration++;
    });
    return JourneyStatusState(journeyInstanceId: journeyInstanceId);
  }

  Future<void> load() => refresh();

  Future<void> refresh() async {
    if (_snapshotPending && _updatesPending) return;
    await Future.wait([_readSnapshot(), _readUpdates()]);
  }

  Future<void> retrySnapshot() => _readSnapshot();
  Future<void> retryUpdates() => _readUpdates();

  Future<void> _readSnapshot() async {
    if (_snapshotPending || !ref.mounted) return;
    _snapshotPending = true;
    final generation = ++_snapshotGeneration;
    state = state.copyWith(
      snapshotStatus: JourneyReadStatus.loading,
      isSnapshotPending: true,
      clearSnapshotFailure: true,
      snapshotMetadata: state.snapshotMetadata,
    );
    try {
      final result = await _repository.getJourneySnapshot(
        journeyInstanceId: journeyInstanceId,
      );
      if (!_currentSnapshot(generation)) return;
      if (result.journeyInstanceId != journeyInstanceId) {
        throw ApiFailure.protocol(RailwayApiOperation.journeySnapshot);
      }
      state = state.copyWith(
        snapshotStatus: JourneyReadStatus.ready,
        snapshot: result,
        snapshotMetadata: state.snapshotMetadata == null
            ? JourneyReadMetadata.success(_clock())
            : state.snapshotMetadata!.afterSuccess(_clock()),
        isSnapshotPending: false,
        clearSnapshotFailure: true,
      );
    } on ApiFailure catch (failure) {
      if (!_currentSnapshot(generation)) return;
      state = state.copyWith(
        snapshotStatus: _status(failure),
        snapshotFailure: failure,
        snapshotMetadata: state.snapshotMetadata?.afterFailure,
        isSnapshotPending: false,
      );
    } catch (_) {
      if (!_currentSnapshot(generation)) return;
      final failure = ApiFailure.protocol(RailwayApiOperation.journeySnapshot);
      state = state.copyWith(
        snapshotStatus: JourneyReadStatus.unavailable,
        snapshotFailure: failure,
        snapshotMetadata: state.snapshotMetadata?.afterFailure,
        isSnapshotPending: false,
      );
    } finally {
      if (_currentSnapshot(generation)) _snapshotPending = false;
    }
  }

  Future<void> _readUpdates() async {
    if (_updatesPending || !ref.mounted) return;
    _updatesPending = true;
    final generation = ++_updatesGeneration;
    state = state.copyWith(
      updatesStatus: JourneyReadStatus.loading,
      isUpdatesPending: true,
      clearUpdatesFailure: true,
    );
    try {
      final result = await _repository.getJourneyUpdates(
        journeyInstanceId: journeyInstanceId,
      );
      if (!_currentUpdates(generation)) return;
      if (result.any((item) => item.journeyInstanceId != journeyInstanceId)) {
        throw ApiFailure.protocol(RailwayApiOperation.journeyUpdates);
      }
      final newlyObserved = _tracker.observe(journeyInstanceId, result);
      final alerts = newlyObserved
          .map(JourneyUpdateAlerts.classify)
          .whereType<JourneyUpdateAlert>()
          .toList();
      state = state.copyWith(
        updatesStatus: JourneyReadStatus.ready,
        updates: List.unmodifiable(result),
        updatesMetadata: state.updatesMetadata == null
            ? JourneyReadMetadata.success(_clock())
            : state.updatesMetadata!.afterSuccess(_clock()),
        newAlerts: alerts,
        isUpdatesPending: false,
        clearUpdatesFailure: true,
      );
    } on ApiFailure catch (failure) {
      if (!_currentUpdates(generation)) return;
      state = state.copyWith(
        updatesStatus: _status(failure),
        updatesFailure: failure,
        updatesMetadata: state.updatesMetadata?.afterFailure,
        isUpdatesPending: false,
        newAlerts: const [],
      );
    } catch (_) {
      if (!_currentUpdates(generation)) return;
      final failure = ApiFailure.protocol(RailwayApiOperation.journeyUpdates);
      state = state.copyWith(
        updatesStatus: JourneyReadStatus.unavailable,
        updatesFailure: failure,
        updatesMetadata: state.updatesMetadata?.afterFailure,
        isUpdatesPending: false,
        newAlerts: const [],
      );
    } finally {
      if (_currentUpdates(generation)) _updatesPending = false;
    }
  }

  bool _currentSnapshot(int generation) =>
      ref.mounted && generation == _snapshotGeneration;
  bool _currentUpdates(int generation) =>
      ref.mounted && generation == _updatesGeneration;

  JourneyReadStatus _status(ApiFailure failure) => switch (failure.kind) {
    ApiFailureKind.invalidRequest ||
    ApiFailureKind.validation => JourneyReadStatus.invalid,
    ApiFailureKind.accessDenied => JourneyReadStatus.accessDenied,
    ApiFailureKind.notFound => JourneyReadStatus.notFound,
    _ => JourneyReadStatus.unavailable,
  };
}
