import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

enum StationLoadStatus { loading, ready, empty, unavailable }

enum SearchValidationError {
  originRequired('Choose an origin station.'),
  destinationRequired('Choose a destination station.'),
  travelDateRequired('Choose a travel date.'),
  stationsMustDiffer('Choose different origin and destination stations.');

  const SearchValidationError(this.message);

  final String message;
}

class SearchState {
  SearchState({
    required this.stationStatus,
    required List<Station> stations,
    required this.criteria,
    this.unavailableMessage,
    this.validationError,
  }) : stations = List.unmodifiable(stations);

  final StationLoadStatus stationStatus;
  final List<Station> stations;
  final SearchCriteria criteria;
  final String? unavailableMessage;
  final SearchValidationError? validationError;
}
