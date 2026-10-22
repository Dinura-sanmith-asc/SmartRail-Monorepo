import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

enum SearchResultsStatus {
  notSearched,
  loading,
  populated,
  empty,
  validation,
  accessDenied,
  unavailable,
}

class SearchResultsState {
  const SearchResultsState({
    this.status = SearchResultsStatus.notSearched,
    this.criteria,
    this.response,
    this.failure,
  });

  final SearchResultsStatus status;
  final SearchCriteria? criteria;
  final ServiceSearchResponse? response;
  final ApiFailure? failure;

  bool get canRetry => status == SearchResultsStatus.unavailable;
}
