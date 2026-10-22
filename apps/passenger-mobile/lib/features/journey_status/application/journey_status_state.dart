import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_read_metadata.dart';

enum JourneyReadStatus {
  loading,
  ready,
  unavailable,
  invalid,
  accessDenied,
  notFound,
}

class JourneyStatusState {
  const JourneyStatusState({
    required this.journeyInstanceId,
    this.snapshotStatus = JourneyReadStatus.loading,
    this.updatesStatus = JourneyReadStatus.loading,
    this.snapshot,
    this.updates,
    this.snapshotFailure,
    this.updatesFailure,
    this.snapshotMetadata,
    this.updatesMetadata,
    this.newAlerts = const [],
    this.isSnapshotPending = false,
    this.isUpdatesPending = false,
  });

  final String journeyInstanceId;
  final JourneyReadStatus snapshotStatus;
  final JourneyReadStatus updatesStatus;
  final JourneyStatusSnapshot? snapshot;
  final List<JourneyUpdate>? updates;
  final ApiFailure? snapshotFailure;
  final ApiFailure? updatesFailure;
  final JourneyReadMetadata? snapshotMetadata;
  final JourneyReadMetadata? updatesMetadata;
  final List<JourneyUpdateAlert> newAlerts;
  final bool isSnapshotPending;
  final bool isUpdatesPending;

  bool get isLoading => isSnapshotPending || isUpdatesPending;
  bool get hasRevisionMismatch =>
      snapshot != null &&
      updates != null &&
      updates!.isNotEmpty &&
      updates!.last.revision != snapshot!.revision;

  JourneyStatusState copyWith({
    JourneyReadStatus? snapshotStatus,
    JourneyReadStatus? updatesStatus,
    JourneyStatusSnapshot? snapshot,
    List<JourneyUpdate>? updates,
    ApiFailure? snapshotFailure,
    ApiFailure? updatesFailure,
    JourneyReadMetadata? snapshotMetadata,
    JourneyReadMetadata? updatesMetadata,
    List<JourneyUpdateAlert>? newAlerts,
    bool? isSnapshotPending,
    bool? isUpdatesPending,
    bool clearSnapshotFailure = false,
    bool clearUpdatesFailure = false,
  }) => JourneyStatusState(
    journeyInstanceId: journeyInstanceId,
    snapshotStatus: snapshotStatus ?? this.snapshotStatus,
    updatesStatus: updatesStatus ?? this.updatesStatus,
    snapshot: snapshot ?? this.snapshot,
    updates: updates ?? this.updates,
    snapshotFailure: clearSnapshotFailure
        ? null
        : snapshotFailure ?? this.snapshotFailure,
    updatesFailure: clearUpdatesFailure
        ? null
        : updatesFailure ?? this.updatesFailure,
    snapshotMetadata: snapshotMetadata ?? this.snapshotMetadata,
    updatesMetadata: updatesMetadata ?? this.updatesMetadata,
    newAlerts: newAlerts ?? this.newAlerts,
    isSnapshotPending: isSnapshotPending ?? this.isSnapshotPending,
    isUpdatesPending: isUpdatesPending ?? this.isUpdatesPending,
  );
}
