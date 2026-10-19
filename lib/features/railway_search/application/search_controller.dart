import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

final searchControllerProvider =
    NotifierProvider<SearchController, SearchState>(SearchController.new);

class SearchController extends Notifier<SearchState> {
  late RailwaySearchRepository _repository;
  int _stationRequestGeneration = 0;

  @override
  SearchState build() {
    _repository = ref.watch(railwaySearchRepositoryProvider);
    ref.onDispose(() => _stationRequestGeneration++);
    final initialState = SearchState(
      stationStatus: StationLoadStatus.loading,
      stations: const [],
      criteria: const SearchCriteria(),
    );
    unawaited(_loadStations());
    return initialState;
  }

  Future<void> retryStations() => _loadStations(showLoading: true);

  void selectOrigin(String? stationId) {
    if (!_isPublishedChoice(stationId)) return;
    state = SearchState(
      stationStatus: state.stationStatus,
      stations: state.stations,
      criteria: SearchCriteria(
        originStationId: stationId,
        destinationStationId: state.criteria.destinationStationId,
        travelDate: state.criteria.travelDate,
      ),
    );
  }

  void selectDestination(String? stationId) {
    if (!_isPublishedChoice(stationId)) return;
    state = SearchState(
      stationStatus: state.stationStatus,
      stations: state.stations,
      criteria: SearchCriteria(
        originStationId: state.criteria.originStationId,
        destinationStationId: stationId,
        travelDate: state.criteria.travelDate,
      ),
    );
  }

  void selectTravelDate(DateTime? date) {
    state = SearchState(
      stationStatus: state.stationStatus,
      stations: state.stations,
      criteria: SearchCriteria(
        originStationId: state.criteria.originStationId,
        destinationStationId: state.criteria.destinationStationId,
        travelDate: date == null
            ? null
            : TravelDate(year: date.year, month: date.month, day: date.day),
      ),
    );
  }

  bool validateCriteria() {
    final criteria = state.criteria;
    final error = criteria.originStationId == null
        ? SearchValidationError.originRequired
        : criteria.destinationStationId == null
        ? SearchValidationError.destinationRequired
        : criteria.travelDate == null
        ? SearchValidationError.travelDateRequired
        : criteria.originStationId == criteria.destinationStationId
        ? SearchValidationError.stationsMustDiffer
        : null;
    state = SearchState(
      stationStatus: state.stationStatus,
      stations: state.stations,
      criteria: state.criteria,
      unavailableMessage: state.unavailableMessage,
      validationError: error,
    );
    return error == null;
  }

  bool _isPublishedChoice(String? stationId) =>
      stationId == null ||
      state.stations.any((station) => station.id == stationId);

  Future<void> _loadStations({bool showLoading = false}) async {
    final generation = ++_stationRequestGeneration;
    if (showLoading) {
      state = SearchState(
        stationStatus: StationLoadStatus.loading,
        stations: state.stations,
        criteria: state.criteria,
        validationError: state.validationError,
      );
    }
    try {
      final stations = await _repository.listStations();
      if (!ref.mounted || generation != _stationRequestGeneration) return;
      state = SearchState(
        stationStatus: stations.isEmpty
            ? StationLoadStatus.empty
            : StationLoadStatus.ready,
        stations: stations,
        criteria: state.criteria,
        validationError: state.validationError,
      );
    } catch (error) {
      if (!ref.mounted || generation != _stationRequestGeneration) return;
      state = SearchState(
        stationStatus: StationLoadStatus.unavailable,
        stations: const [],
        criteria: state.criteria,
        unavailableMessage: error is ApiFailure ? error.passengerMessage : 'Railway information is temporarily unavailable. Please try again.',
        validationError: state.validationError,
      );
    }
  }
}
