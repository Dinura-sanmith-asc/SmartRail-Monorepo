import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/core/widgets/async_feedback.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_controller.dart'
    as search;
import 'package:smartrail_passenger/features/railway_search/application/search_results_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_state.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/station_picker.dart';

class RailwaySearchScreen extends ConsumerWidget {
  const RailwaySearchScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(search.searchControllerProvider);
    final resultsState = ref.watch(searchResultsNotifierProvider);
    final controller = ref.read(search.searchControllerProvider.notifier);
    final origin = _stationFor(state.stations, state.criteria.originStationId);
    final destination = _stationFor(
      state.stations,
      state.criteria.destinationStationId,
    );

    return Scaffold(
      appBar: AppBar(title: const Text('SmartRail Passenger')),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Search railway services',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Choose your origin, destination, and travel date.',
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                  const SizedBox(height: 24),
                  _stationChoices(
                    context,
                    ref: ref,
                    state: state,
                    controller: controller,
                    origin: origin,
                    destination: destination,
                  ),
                  const SizedBox(height: 16),
                  OutlinedButton.icon(
                    onPressed: () =>
                        _chooseDate(context, state, controller, ref),
                    icon: const Icon(Icons.calendar_month_outlined),
                    label: Text(
                      _dateButtonLabel(context, state.criteria.travelDate),
                    ),
                    style: OutlinedButton.styleFrom(
                      alignment: Alignment.centerLeft,
                      padding: const EdgeInsets.symmetric(
                        horizontal: 16,
                        vertical: 18,
                      ),
                    ),
                  ),
                  if (state.validationError != null) ...[
                    const SizedBox(height: 16),
                    ValidationFeedback(message: state.validationError!.message),
                  ],
                  const SizedBox(height: 20),
                  FilledButton.icon(
                    onPressed:
                        resultsState.status == SearchResultsStatus.loading
                        ? null
                        : () {
                            if (!controller.validateCriteria()) return;
                            final criteria = ref
                                .read(search.searchControllerProvider)
                                .criteria;
                            unawaited(
                              ref
                                  .read(searchResultsNotifierProvider.notifier)
                                  .submit(criteria),
                            );
                            context.pushNamed('railway-results');
                          },
                    icon: const Icon(Icons.search),
                    label: const Text('Search services'),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _stationChoices(
    BuildContext context, {
    required WidgetRef ref,
    required SearchState state,
    required search.SearchController controller,
    required Station? origin,
    required Station? destination,
  }) => switch (state.stationStatus) {
    StationLoadStatus.loading => const LoadingFeedback(
      message: 'Loading stations',
    ),
    StationLoadStatus.empty => const EmptyFeedback(
      title: 'No stations available',
      message: 'No published stations are available.',
    ),
    StationLoadStatus.unavailable => UnavailableFeedback(
      message:
          state.unavailableMessage ??
          'Railway information is temporarily unavailable. Please try again.',
      onRetry: controller.retryStations,
    ),
    StationLoadStatus.ready => Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        StationPicker(
          label: 'origin',
          stations: state.stations,
          selectedStation: origin,
          enabled: true,
          onSelected: (stationId) {
            if (stationId == state.criteria.originStationId) return;
            ref.read(searchResultsNotifierProvider.notifier).clear();
            controller.selectOrigin(stationId);
          },
        ),
        const SizedBox(height: 12),
        StationPicker(
          label: 'destination',
          stations: state.stations,
          selectedStation: destination,
          enabled: true,
          onSelected: (stationId) {
            if (stationId == state.criteria.destinationStationId) return;
            ref.read(searchResultsNotifierProvider.notifier).clear();
            controller.selectDestination(stationId);
          },
        ),
      ],
    ),
  };

  Future<void> _chooseDate(
    BuildContext context,
    SearchState state,
    search.SearchController controller,
    WidgetRef ref,
  ) async {
    final selected = state.criteria.travelDate;
    final today = DateTime.now();
    final initialDate = selected == null
        ? DateTime(today.year, today.month, today.day)
        : DateTime(selected.year, selected.month, selected.day);
    final date = await showDatePicker(
      context: context,
      initialDate: initialDate,
      firstDate: DateTime(1),
      lastDate: DateTime(9999, 12, 31),
      helpText: 'Choose travel date',
    );
    if (date != null &&
        (date.year != selected?.year ||
            date.month != selected?.month ||
            date.day != selected?.day)) {
      ref.read(searchResultsNotifierProvider.notifier).clear();
      controller.selectTravelDate(date);
    }
  }

  String _dateButtonLabel(BuildContext context, TravelDate? date) {
    if (date == null) return 'Choose travel date';
    final localCalendarDate = DateTime(date.year, date.month, date.day);
    return 'Travel date: ${MaterialLocalizations.of(context).formatMediumDate(localCalendarDate)}';
  }

  Station? _stationFor(List<Station> stations, String? stationId) {
    if (stationId == null) return null;
    for (final station in stations) {
      if (station.id == stationId) return station;
    }
    return null;
  }
}
