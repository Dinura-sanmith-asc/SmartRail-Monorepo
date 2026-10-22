import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';

final journeyLookupNotifierProvider = NotifierProvider.autoDispose
    .family<JourneyLookupNotifier, JourneyLookupState, JourneyLookupRequest>(
      JourneyLookupNotifier.new,
    );

class JourneyLookupNotifier extends Notifier<JourneyLookupState> {
  JourneyLookupNotifier(this.request);

  final JourneyLookupRequest request;
  late JourneyStatusRepository _repository;
  int _generation = 0;

  @override
  JourneyLookupState build() {
    _repository = ref.watch(journeyStatusRepositoryProvider);
    ref.onDispose(() => _generation++);
    return JourneyLookupState();
  }

  Future<void> load() async {
    final generation = ++_generation;
    state = JourneyLookupState(status: JourneyLookupStatus.loading);
    try {
      final response = await _repository.getStationDepartures(
        stationId: request.originStationId,
        date: request.stationLocalDepartureDate,
      );
      if (!ref.mounted || generation != _generation) return;
      final matching = response
          .where(
            (candidate) =>
                candidate.serviceId == request.serviceId &&
                candidate.stationLocalDepartureDate ==
                    request.stationLocalDepartureDate,
          )
          .toList(growable: false);
      state = JourneyLookupState(
        status: matching.isEmpty
            ? JourneyLookupStatus.empty
            : JourneyLookupStatus.ready,
        candidates: matching,
      );
    } on ApiFailure catch (failure) {
      if (!ref.mounted || generation != _generation) return;
      state = JourneyLookupState(status: _statusFor(failure), failure: failure);
    } catch (error) {
      if (!ref.mounted || generation != _generation) return;
      final failure = ApiFailure.protocol(
        RailwayApiOperation.stationDepartures,
        error,
      );
      state = JourneyLookupState(
        status: JourneyLookupStatus.unavailable,
        failure: failure,
      );
    }
  }

  Future<void> retry() async {
    if (state.canRetry) await load();
  }

  /// Selects only one of the exact candidates returned for this lookup.
  void select(JourneyCandidate candidate) {
    if (!state.candidates.any((item) => identical(item, candidate))) {
      throw ArgumentError(
        'The selected journey was not returned by this lookup.',
      );
    }
    state = JourneyLookupState(
      status: JourneyLookupStatus.ready,
      candidates: state.candidates,
      selectedCandidate: candidate,
    );
  }

  /// Allows the single unambiguous-match path specified by the feature.
  void openUniqueUnambiguous() {
    if (state.candidates.length != 1 ||
        state.candidates.single.ambiguity != JourneyAmbiguity.none) {
      return;
    }
    select(state.candidates.single);
  }

  JourneyLookupStatus _statusFor(ApiFailure failure) => switch (failure.kind) {
    ApiFailureKind.invalidRequest => JourneyLookupStatus.invalid,
    ApiFailureKind.accessDenied => JourneyLookupStatus.accessDenied,
    ApiFailureKind.notFound => JourneyLookupStatus.notFound,
    ApiFailureKind.unavailable ||
    ApiFailureKind.unexpected ||
    ApiFailureKind.timeout ||
    ApiFailureKind.network ||
    ApiFailureKind.protocol => JourneyLookupStatus.unavailable,
    ApiFailureKind.validation => JourneyLookupStatus.invalid,
  };
}
