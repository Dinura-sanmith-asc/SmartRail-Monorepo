import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

final searchResultsNotifierProvider =
    NotifierProvider<SearchResultsNotifier, SearchResultsState>(
      SearchResultsNotifier.new,
    );

class SearchResultsNotifier extends Notifier<SearchResultsState> {
  late RailwaySearchRepository _repository;
  int _requestGeneration = 0;

  @override
  SearchResultsState build() {
    _repository = ref.watch(railwaySearchRepositoryProvider);
    ref.onDispose(() => _requestGeneration++);
    return const SearchResultsState();
  }

  Future<void> submit(SearchCriteria criteria) async {
    if (!criteria.isSubmittable) return;
    if (state.status == SearchResultsStatus.loading &&
        state.criteria == criteria) {
      return;
    }
    final generation = ++_requestGeneration;
    state = SearchResultsState(
      status: SearchResultsStatus.loading,
      criteria: criteria,
    );
    try {
      final response = await _repository.search(criteria);
      if (!ref.mounted || generation != _requestGeneration) return;
      state = SearchResultsState(
        status: response.services.isEmpty
            ? SearchResultsStatus.empty
            : SearchResultsStatus.populated,
        criteria: criteria,
        response: response,
      );
    } on ApiFailure catch (failure) {
      if (!ref.mounted || generation != _requestGeneration) return;
      state = SearchResultsState(
        status: switch (failure.kind) {
          ApiFailureKind.validation => SearchResultsStatus.validation,
          ApiFailureKind.accessDenied => SearchResultsStatus.accessDenied,
          _ => SearchResultsStatus.unavailable,
        },
        criteria: criteria,
        failure: failure,
      );
    } catch (_) {
      if (!ref.mounted || generation != _requestGeneration) return;
      state = SearchResultsState(
        status: SearchResultsStatus.unavailable,
        criteria: criteria,
        failure: ApiFailure.protocol(RailwayApiOperation.serviceSearch),
      );
    }
  }

  Future<void> retry() async {
    final criteria = state.criteria;
    if (state.canRetry && criteria != null) await submit(criteria);
  }

  /// Invalidates in-flight responses and clears results when criteria change.
  void clear() {
    ++_requestGeneration;
    state = const SearchResultsState();
  }

  /// Called when Results is popped while a request is pending.
  void abandon() {
    if (state.status == SearchResultsStatus.loading) clear();
  }
}
