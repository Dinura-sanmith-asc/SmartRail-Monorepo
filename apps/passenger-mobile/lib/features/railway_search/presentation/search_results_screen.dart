import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_state.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_controller.dart'
    as search;
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_result_card.dart';

class SearchResultsScreen extends ConsumerWidget {
  const SearchResultsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(searchResultsNotifierProvider);
    final searchState = ref.watch(search.searchControllerProvider);
    final notifier = ref.read(searchResultsNotifierProvider.notifier);
    final originName = _stationName(
      searchState.stations,
      searchState.criteria.originStationId,
      fallback: 'Selected origin',
    );
    final destinationName = _stationName(
      searchState.stations,
      searchState.criteria.destinationStationId,
      fallback: 'Selected destination',
    );
    return PopScope<Object?>(
      onPopInvokedWithResult: (didPop, _) {
        if (didPop) notifier.abandon();
      },
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Search results'),
          leading: IconButton(
            tooltip: 'Back',
            onPressed: () => context.pop(),
            icon: const Icon(Icons.arrow_back),
          ),
        ),
        body: SafeArea(
          child: _ResultsBody(
            state: state,
            originName: originName,
            destinationName: destinationName,
            onRetry: notifier.retry,
            onChangeSearch: () => context.pop(),
          ),
        ),
      ),
    );
  }
}

class _ResultsBody extends StatelessWidget {
  const _ResultsBody({
    required this.state,
    required this.originName,
    required this.destinationName,
    required this.onRetry,
    required this.onChangeSearch,
  });

  final SearchResultsState state;
  final String originName;
  final String destinationName;
  final Future<void> Function() onRetry;
  final VoidCallback onChangeSearch;

  @override
  Widget build(BuildContext context) => switch (state.status) {
    SearchResultsStatus.notSearched => _message(
      title: 'Search not submitted',
      message: 'Return to search and submit your travel details.',
      actionLabel: 'Change search',
      onAction: onChangeSearch,
    ),
    SearchResultsStatus.loading => Center(
      child: Semantics(
        liveRegion: true,
        label: 'Searching scheduled services',
        child: const Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircularProgressIndicator(),
            SizedBox(height: 16),
            Text('Searching scheduled services'),
          ],
        ),
      ),
    ),
    SearchResultsStatus.populated => _populated(context),
    SearchResultsStatus.empty => _message(
      title: 'No services found',
      message: 'There are no published services matching this search.',
      actionLabel: 'Change search',
      onAction: onChangeSearch,
    ),
    SearchResultsStatus.validation => _message(
      title: 'Check your search',
      message: _validationMessage(),
      actionLabel: 'Change search',
      onAction: onChangeSearch,
    ),
    SearchResultsStatus.accessDenied => _message(
      title: 'Access unavailable',
      message:
          state.failure?.passengerMessage ??
          'Access to this railway information is unavailable.',
      actionLabel: 'Back',
      onAction: onChangeSearch,
    ),
    SearchResultsStatus.unavailable => _message(
      title: 'Information unavailable',
      message:
          state.failure?.passengerMessage ??
          'Railway information is temporarily unavailable. Please try again.',
      actionLabel: 'Change search',
      onAction: onChangeSearch,
      secondaryLabel: 'Search again',
      onSecondary: state.canRetry ? onRetry : null,
    ),
  };

  String _validationMessage() {
    final errors = state.failure?.problem?.errors;
    if (errors == null || errors.isEmpty) {
      return 'Check the search details and try again.';
    }
    return errors.entries
        .expand(
          (entry) => entry.value.map((message) => '${entry.key}: $message'),
        )
        .join('\n');
  }

  Widget _populated(BuildContext context) {
    final response = state.response!;
    final criteria = state.criteria!;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Semantics(
          header: true,
          child: Text(
            'Scheduled services for $originName to $destinationName on '
            '${criteria.travelDate!.toIsoDate()}',
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ),
        const SizedBox(height: 12),
        for (final choice in response.services)
          ServiceResultCard(
            choice: choice,
            travelDate: response.travelDate,
            onTap: () => context.pushNamed(
              'railway-service-details',
              pathParameters: {'serviceId': choice.serviceId},
              extra: (choice: choice, travelDate: response.travelDate),
            ),
          ),
        const SizedBox(height: 8),
        OutlinedButton(
          onPressed: onChangeSearch,
          child: const Text('Change search'),
        ),
      ],
    );
  }

  Widget _message({
    required String title,
    required String message,
    required String actionLabel,
    required VoidCallback onAction,
    String? secondaryLabel,
    Future<void> Function()? onSecondary,
  }) => Center(
    child: SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 520),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              title,
              style: const TextStyle(fontSize: 22),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 12),
            Semantics(
              liveRegion: true,
              child: Text(message, textAlign: TextAlign.center),
            ),
            const SizedBox(height: 20),
            FilledButton(onPressed: onAction, child: Text(actionLabel)),
            if (secondaryLabel != null && onSecondary != null) ...[
              const SizedBox(height: 8),
              OutlinedButton(
                onPressed: onSecondary,
                child: Text(secondaryLabel),
              ),
            ],
          ],
        ),
      ),
    ),
  );
}

String _stationName(
  List<Station> stations,
  String? stationId, {
  required String fallback,
}) {
  for (final station in stations) {
    if (station.id == stationId) return station.name;
  }
  return fallback;
}
