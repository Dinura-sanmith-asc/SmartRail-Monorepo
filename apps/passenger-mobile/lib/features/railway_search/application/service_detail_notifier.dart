import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/retrieval_metadata.dart';
import 'package:smartrail_passenger/features/railway_search/application/recovery_message_mapper.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

typedef ServiceDetailRequest = ({
  ScheduledServiceChoice choice,
  TravelDate travelDate,
});
typedef DetailClock = DateTime Function();

final serviceDetailClockProvider = Provider<DetailClock>((ref) => DateTime.now);

final serviceDetailNotifierProvider = NotifierProvider.autoDispose
    .family<ServiceDetailNotifier, ServiceDetailState, ServiceDetailRequest>(
      ServiceDetailNotifier.new,
    );

/// Owns independent details reads for a single route-scoped provider family entry.
class ServiceDetailNotifier extends Notifier<ServiceDetailState> {
  ServiceDetailNotifier(this.request);

  final ServiceDetailRequest request;
  late DetailClock _clock;
  int _serviceGeneration = 0;
  int _stopsGeneration = 0;
  bool _servicePending = false;
  bool _stopsPending = false;

  @override
  ServiceDetailState build() {
    final repository = ref.watch(railwaySearchRepositoryProvider);
    _clock = ref.watch(serviceDetailClockProvider);
    ref.onDispose(() {
      _serviceGeneration++;
      _stopsGeneration++;
    });
    final initial = ServiceDetailState(
      choice: request.choice,
      travelDate: request.travelDate,
      serviceStatus: DetailSectionStatus.loading,
      stopsStatus: DetailSectionStatus.loading,
    );
    scheduleMicrotask(() {
      if (!ref.mounted) return;
      unawaited(_loadService(repository));
      unawaited(_loadStops(repository));
    });
    return initial;
  }

  Future<void> retryService() async {
    if (_servicePending ||
        (!state.canRetryService && state.serviceDetail == null) ||
        !ref.mounted) {
      return;
    }
    await _loadService(ref.read(railwaySearchRepositoryProvider));
  }

  Future<void> retryStops() async {
    if (_stopsPending ||
        (!state.canRetryStops && state.routeStops == null) ||
        !ref.mounted) {
      return;
    }
    await _loadStops(ref.read(railwaySearchRepositoryProvider));
  }

  Future<void> _loadService(RailwaySearchRepository repository) async {
    final generation = ++_serviceGeneration;
    _servicePending = true;
    state = state.copyWith(
      serviceStatus: DetailSectionStatus.loading,
      clearServiceMessage: true,
    );
    try {
      final detail = await repository.getService(request.choice.serviceId);
      if (!_isCurrent(generation, _serviceGeneration)) return;
      _servicePending = false;
      state = state.copyWith(
        serviceStatus: DetailSectionStatus.ready,
        serviceDetail: detail,
        serviceMetadata: RetrievalMetadata.success(_clock()),
        clearServiceMessage: true,
      );
    } catch (error) {
      if (!_isCurrent(generation, _serviceGeneration)) return;
      _servicePending = false;
      state = state.copyWith(
        serviceStatus: _statusFor(error),
        serviceMessage: _messageFor(error),
        serviceMetadata: state.serviceMetadata?.afterFailedRefresh(),
      );
    }
  }

  Future<void> _loadStops(RailwaySearchRepository repository) async {
    final generation = ++_stopsGeneration;
    _stopsPending = true;
    state = state.copyWith(
      stopsStatus: DetailSectionStatus.loading,
      clearStopsMessage: true,
    );
    try {
      final stops = await repository.getRouteStops(request.choice.routeId);
      if (!_isCurrent(generation, _stopsGeneration)) return;
      _stopsPending = false;
      state = state.copyWith(
        stopsStatus: stops.stops.isEmpty
            ? DetailSectionStatus.empty
            : DetailSectionStatus.ready,
        routeStops: stops,
        stopsMetadata: RetrievalMetadata.success(_clock()),
        clearStopsMessage: true,
      );
    } catch (error) {
      if (!_isCurrent(generation, _stopsGeneration)) return;
      _stopsPending = false;
      state = state.copyWith(
        stopsStatus: _statusFor(error),
        stopsMessage: _messageFor(error),
        stopsMetadata: state.stopsMetadata?.afterFailedRefresh(),
      );
    }
  }

  bool _isCurrent(int generation, int current) =>
      ref.mounted && generation == current;

  DetailSectionStatus _statusFor(Object error) {
    if (error is! ApiFailure) return DetailSectionStatus.unavailable;
    return switch (error.kind) {
      ApiFailureKind.accessDenied => DetailSectionStatus.accessDenied,
      ApiFailureKind.notFound => DetailSectionStatus.notFound,
      ApiFailureKind.invalidRequest => DetailSectionStatus.invalid,
      _ => DetailSectionStatus.unavailable,
    };
  }

  String _messageFor(Object error) => error is ApiFailure
      ? RecoveryMessageMapper.map(error).message
      : 'Railway information is temporarily unavailable. Please try again.';
}
