import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

/// Owns the two independent reads for one pushed details route.
class ServiceDetailController extends ChangeNotifier {
  ServiceDetailController(
    this._repository, {
    required ScheduledServiceChoice choice,
    required TravelDate travelDate,
  }) : state = ServiceDetailState(
         choice: choice,
         travelDate: travelDate,
         serviceStatus: DetailSectionStatus.loading,
         stopsStatus: DetailSectionStatus.loading,
       ) {
    unawaited(_loadService());
    unawaited(_loadStops());
  }

  final RailwaySearchRepository _repository;
  ServiceDetailState state;
  int _serviceGeneration = 0;
  int _stopsGeneration = 0;
  bool _servicePending = true;
  bool _stopsPending = true;
  bool _disposed = false;

  Future<void> retryService() {
    if (_servicePending || !state.canRetryService || _disposed) {
      return Future<void>.value();
    }
    return _loadService();
  }

  Future<void> retryStops() {
    if (_stopsPending || !state.canRetryStops || _disposed) {
      return Future<void>.value();
    }
    return _loadStops();
  }

  Future<void> _loadService() async {
    final generation = ++_serviceGeneration;
    _servicePending = true;
    _update(
      state.copyWith(
        serviceStatus: DetailSectionStatus.loading,
        clearServiceMessage: true,
      ),
    );
    try {
      final detail = await _repository.getService(state.choice.serviceId);
      if (!_active(generation, _serviceGeneration)) return;
      _servicePending = false;
      _update(
        state.copyWith(
          serviceStatus: DetailSectionStatus.ready,
          serviceDetail: detail,
          clearServiceMessage: true,
        ),
      );
    } catch (error) {
      if (!_active(generation, _serviceGeneration)) return;
      _servicePending = false;
      _update(
        state.copyWith(
          serviceStatus: _statusFor(error),
          serviceMessage: _messageFor(error),
        ),
      );
    }
  }

  Future<void> _loadStops() async {
    final generation = ++_stopsGeneration;
    _stopsPending = true;
    _update(
      state.copyWith(
        stopsStatus: DetailSectionStatus.loading,
        clearStopsMessage: true,
      ),
    );
    try {
      final stops = await _repository.getRouteStops(state.choice.routeId);
      if (!_active(generation, _stopsGeneration)) return;
      _stopsPending = false;
      _update(
        state.copyWith(
          stopsStatus: stops.stops.isEmpty
              ? DetailSectionStatus.empty
              : DetailSectionStatus.ready,
          routeStops: stops,
          clearStopsMessage: true,
        ),
      );
    } catch (error) {
      if (!_active(generation, _stopsGeneration)) return;
      _stopsPending = false;
      _update(
        state.copyWith(
          stopsStatus: _statusFor(error),
          stopsMessage: _messageFor(error),
        ),
      );
    }
  }

  bool _active(int generation, int current) =>
      !_disposed && generation == current;

  void _update(ServiceDetailState next) {
    if (_disposed) return;
    state = next;
    notifyListeners();
  }

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
      ? error.passengerMessage
      : 'Railway information is temporarily unavailable. Please try again.';

  @override
  void dispose() {
    _disposed = true;
    _serviceGeneration++;
    _stopsGeneration++;
    super.dispose();
  }
}
