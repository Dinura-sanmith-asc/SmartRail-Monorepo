import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/route_stops_list.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_summary.dart';

class ServiceDetailScreen extends ConsumerStatefulWidget {
  const ServiceDetailScreen({
    required this.choice,
    required this.travelDate,
    super.key,
  });

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;

  @override
  ConsumerState<ServiceDetailScreen> createState() =>
      _ServiceDetailScreenState();
}

class _ServiceDetailScreenState extends ConsumerState<ServiceDetailScreen> {
  late final ServiceDetailController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ServiceDetailController(
      ref.read(railwaySearchRepositoryProvider),
      choice: widget.choice,
      travelDate: widget.travelDate,
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Scheduled service details')),
    body: SafeArea(
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) => ListView(
          padding: const EdgeInsets.all(24),
          children: [
            ServiceDetailSummary(
              choice: widget.choice,
              travelDate: widget.travelDate,
              detail: _controller.state.serviceDetail,
            ),
            const SizedBox(height: 24),
            _section(
              context,
              heading: 'Service information',
              status: _controller.state.serviceStatus,
              message: _controller.state.serviceMessage,
              retryKey: const Key('retry-service-detail'),
              retry: _controller.retryService,
              loadingLabel: 'Loading service details',
              unavailableLabel: 'Service details are unavailable.',
              notFoundLabel: 'This service is no longer available.',
              invalidLabel:
                  'Service details are unavailable for this identifier.',
              accessLabel: 'Access to service details is unavailable.',
              child: _controller.state.serviceDetail == null
                  ? null
                  : Text(
                      'Service ${_controller.state.serviceDetail!.serviceCode} · '
                      'Route ${_controller.state.serviceDetail!.route.routeCode}',
                    ),
            ),
            const SizedBox(height: 20),
            _section(
              context,
              heading: 'Planned route stops',
              status: _controller.state.stopsStatus,
              message: _controller.state.stopsMessage,
              retryKey: const Key('retry-route-stops'),
              retry: _controller.retryStops,
              loadingLabel: 'Loading route stops',
              unavailableLabel: 'Route stops are unavailable.',
              notFoundLabel: 'This route is no longer available.',
              invalidLabel: 'Route stops are unavailable for this identifier.',
              accessLabel: 'Access to route stops is unavailable.',
              child: _controller.state.routeStops == null
                  ? null
                  : RouteStopsList(routeStops: _controller.state.routeStops!),
            ),
          ],
        ),
      ),
    ),
  );

  Widget _section(
    BuildContext context, {
    required String heading,
    required DetailSectionStatus status,
    required String? message,
    required Key retryKey,
    required Future<void> Function() retry,
    required String loadingLabel,
    required String unavailableLabel,
    required String notFoundLabel,
    required String invalidLabel,
    required String accessLabel,
    Widget? child,
  }) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(heading, style: Theme.of(context).textTheme.titleMedium),
      const SizedBox(height: 8),
      if (status == DetailSectionStatus.loading)
        Semantics(
          liveRegion: true,
          label: loadingLabel,
          child: const LinearProgressIndicator(),
        )
      else if (child != null &&
          (status == DetailSectionStatus.ready ||
              status == DetailSectionStatus.empty))
        child
      else ...[
        Text(switch (status) {
          DetailSectionStatus.accessDenied => accessLabel,
          DetailSectionStatus.notFound => notFoundLabel,
          DetailSectionStatus.invalid => invalidLabel,
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
    ],
  );
}
