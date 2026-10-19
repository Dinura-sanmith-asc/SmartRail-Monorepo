import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/features/railway_search/application/retrieval_metadata.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/route_stops_list.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_summary.dart';

class ServiceDetailScreen extends ConsumerWidget {
  const ServiceDetailScreen({
    required this.choice,
    required this.travelDate,
    super.key,
  });

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final request = (choice: choice, travelDate: travelDate);
    final provider = serviceDetailNotifierProvider(request);
    final state = ref.watch(provider);
    final notifier = ref.read(provider.notifier);
    return Scaffold(
      appBar: AppBar(title: const Text('Scheduled service details')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: [
            ServiceDetailSummary(
              choice: choice,
              travelDate: travelDate,
              detail: state.serviceDetail,
            ),
            const SizedBox(height: 24),
            _section(
              context,
              heading: 'Service information',
              status: state.serviceStatus,
              message: state.serviceMessage,
              metadata: state.serviceMetadata,
              retryKey: const Key('retry-service-detail'),
              retry: notifier.retryService,
              loadingLabel: 'Loading service details',
              unavailableLabel: 'Service details are unavailable.',
              notFoundLabel: 'This service is no longer available.',
              invalidLabel:
                  'Service details are unavailable for this identifier.',
              accessLabel: 'Access to service details is unavailable.',
              child: state.serviceDetail == null
                  ? null
                  : Text(
                      'Service ${state.serviceDetail!.serviceCode} · '
                      'Route ${state.serviceDetail!.route.routeCode}',
                    ),
            ),
            const SizedBox(height: 20),
            _section(
              context,
              heading: 'Planned route stops',
              status: state.stopsStatus,
              message: state.stopsMessage,
              metadata: state.stopsMetadata,
              retryKey: const Key('retry-route-stops'),
              retry: notifier.retryStops,
              loadingLabel: 'Loading route stops',
              unavailableLabel: 'Route stops are unavailable.',
              notFoundLabel: 'This route is no longer available.',
              invalidLabel: 'Route stops are unavailable for this identifier.',
              accessLabel: 'Access to route stops is unavailable.',
              child: state.routeStops == null
                  ? null
                  : RouteStopsList(routeStops: state.routeStops!),
            ),
          ],
        ),
      ),
    );
  }

  Widget _section(
    BuildContext context, {
    required String heading,
    required DetailSectionStatus status,
    required String? message,
    required RetrievalMetadata? metadata,
    required Key retryKey,
    required Future<void> Function() retry,
    required String loadingLabel,
    required String unavailableLabel,
    required String notFoundLabel,
    required String invalidLabel,
    required String accessLabel,
    Widget? child,
  }) {
    final hasLoadedData = child != null && metadata != null;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(heading, style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        if (status == DetailSectionStatus.loading && !hasLoadedData)
          Semantics(
            liveRegion: true,
            label: loadingLabel,
            child: const LinearProgressIndicator(),
          )
        else if (hasLoadedData)
          child,
        if (hasLoadedData && metadata.isNotRefreshed)
          Text(
            'Not refreshed · retrieved at ${metadata.lastRetrievedAt.toIso8601String()}',
          ),
        if (status == DetailSectionStatus.loading && hasLoadedData)
          const LinearProgressIndicator(),
        if (status == DetailSectionStatus.unavailable && hasLoadedData)
          Text(message ?? unavailableLabel),
        if (!hasLoadedData &&
            status != DetailSectionStatus.loading &&
            status != DetailSectionStatus.ready &&
            status != DetailSectionStatus.empty) ...[
          Text(switch (status) {
            DetailSectionStatus.accessDenied => message ?? accessLabel,
            DetailSectionStatus.notFound => message ?? notFoundLabel,
            DetailSectionStatus.invalid => message ?? invalidLabel,
            _ => message ?? unavailableLabel,
          }, semanticsLabel: '$heading unavailable'),
          if (status == DetailSectionStatus.unavailable)
            Align(
              alignment: Alignment.centerLeft,
              child: TextButton(
                key: retryKey,
                onPressed: retry,
                child: const Text('Retry'),
              ),
            ),
        ],
        if (hasLoadedData && status == DetailSectionStatus.unavailable)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              key: retryKey,
              onPressed: retry,
              child: const Text('Retry'),
            ),
          ),
      ],
    );
  }
}
