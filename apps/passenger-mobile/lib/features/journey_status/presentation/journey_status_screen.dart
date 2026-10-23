import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_update_alert.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_recovery_section.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_updates_list.dart';

class JourneyStatusScreen extends ConsumerStatefulWidget {
  const JourneyStatusScreen({
    required this.journeyInstanceId,
    this.journeyLabel,
    super.key,
  });

  final String journeyInstanceId;
  final String? journeyLabel;

  @override
  ConsumerState<JourneyStatusScreen> createState() =>
      _JourneyStatusScreenState();
}

class _JourneyStatusScreenState extends ConsumerState<JourneyStatusScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        unawaited(
          ref
              .read(
                journeyStatusNotifierProvider(widget.journeyInstanceId)
                    .notifier,
              )
              .load(),
        );
      }
    });
  }

  @override
  void didUpdateWidget(covariant JourneyStatusScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.journeyInstanceId != widget.journeyInstanceId) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted) {
          unawaited(
            ref
                .read(
                  journeyStatusNotifierProvider(widget.journeyInstanceId)
                      .notifier,
                )
                .load(),
          );
        }
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = journeyStatusNotifierProvider(widget.journeyInstanceId);
    final state = ref.watch(provider);
    final notifier = ref.read(provider.notifier);
    return Scaffold(
      appBar: AppBar(title: const Text('Journey status')),
      body: RefreshIndicator(
        onRefresh: notifier.refresh,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            FilledButton.icon(
              onPressed: () => context.push(
                '/catering/journeys/${widget.journeyInstanceId}',
                extra: widget.journeyLabel,
              ),
              icon: const Icon(Icons.restaurant),
              label: const Text('Order food for this journey'),
            ),
            Semantics(
              button: true,
              label: 'Refresh journey information',
              child: Align(
                alignment: Alignment.centerRight,
                child: IconButton(
                  tooltip: 'Refresh journey information',
                  onPressed: state.isLoading ? null : notifier.refresh,
                  icon: const Icon(Icons.refresh),
                ),
              ),
            ),
            if (state.newAlerts.isNotEmpty)
              for (final alert in state.newAlerts)
                JourneyUpdateAlertBanner(alert: alert),
            _snapshotSection(state, notifier),
            const SizedBox(height: 20),
            _updatesSection(state, notifier),
            if (state.hasRevisionMismatch)
              const Padding(
                padding: EdgeInsets.only(top: 12),
                child: Text(
                  'The status and update history show different revisions. Refresh to check for newer information.',
                  key: Key('journey-revision-mismatch'),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _snapshotSection(
    JourneyStatusState state,
    JourneyStatusNotifier notifier,
  ) {
    final snapshot = state.snapshot;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Current status', style: Theme.of(context).textTheme.titleLarge),
        const SizedBox(height: 8),
        if (state.snapshotStatus == JourneyReadStatus.loading &&
            snapshot == null)
          Semantics(
            container: true,
            label: 'Loading journey status',
            liveRegion: true,
            child: ExcludeSemantics(child: LinearProgressIndicator()),
          ),
        if (snapshot != null) ...[
          Semantics(
            liveRegion: true,
            child: Text(
              snapshot.status.apiValue,
              key: const Key('journey-status-value'),
              style: Theme.of(context).textTheme.headlineSmall,
            ),
          ),
          Text('Source: ${snapshot.source.apiValue}'),
          Text('Revision ${snapshot.revision}'),
          Text(
            snapshot.publishedAt == null
                ? 'Scheduled baseline · no operational publication time'
                : 'Published ${snapshot.publishedAt!.rawValue}',
          ),
          if (snapshot.lastUpdatedAt != null)
            Text('Last updated ${snapshot.lastUpdatedAt!.rawValue}'),
          if (snapshot.eta == null)
            const Text('Estimated arrival unavailable')
          else
            Text(
              'Estimated arrival ${snapshot.eta!.value.rawValue} · ${snapshot.eta!.freshness.apiValue}',
            ),
          if (state.snapshotMetadata != null) ...[
            Text(
              'Retrieved ${state.snapshotMetadata!.lastRetrievedAt.toIso8601String()}',
            ),
            if (state.snapshotMetadata!.notRefreshed)
              const Text('Not refreshed · showing previously retrieved status'),
          ],
          const SizedBox(height: 8),
          const Text('Journey stops'),
          for (final stop in snapshot.stops)
            ListTile(
              contentPadding: EdgeInsets.zero,
              title: Text(
                '${stop.stopOrder}. ${stop.stationName ?? 'Station'}',
              ),
              subtitle: Text(_stopTimes(stop)),
            ),
        ],
        if (state.snapshotFailure != null && snapshot == null)
          _failure(
            state.snapshotFailure!,
            'Journey status',
            notifier.retrySnapshot,
          ),
        if (state.snapshotFailure != null && snapshot != null)
          _retryLoaded(
            'Status could not be refreshed.',
            state.snapshotStatus,
            notifier.retrySnapshot,
          ),
        if (state.isSnapshotPending && snapshot != null)
          const LinearProgressIndicator(),
      ],
    );
  }

  Widget _updatesSection(
    JourneyStatusState state,
    JourneyStatusNotifier notifier,
  ) {
    final updates = state.updates;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Published updates',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: 8),
        if (state.updatesStatus == JourneyReadStatus.loading && updates == null)
          Semantics(
            label: 'Loading published updates',
            liveRegion: true,
            child: LinearProgressIndicator(),
          ),
        if (updates != null) JourneyUpdatesList(updates: updates),
        if (updates != null && state.updatesMetadata != null) ...[
          Text(
            'Retrieved ${state.updatesMetadata!.lastRetrievedAt.toIso8601String()}',
          ),
          if (state.updatesMetadata!.notRefreshed)
            const Text('Not refreshed · showing previously retrieved updates'),
        ],
        if (state.updatesFailure != null && updates == null)
          _failure(
            state.updatesFailure!,
            'Published updates',
            notifier.retryUpdates,
          ),
        if (state.updatesFailure != null && updates != null)
          _retryLoaded(
            'Updates could not be refreshed.',
            state.updatesStatus,
            notifier.retryUpdates,
          ),
        if (state.isUpdatesPending && updates != null)
          const LinearProgressIndicator(),
      ],
    );
  }

  Widget _failure(
    ApiFailure failure,
    String sectionLabel,
    Future<void> Function() retry,
  ) => JourneyRecoverySection(
    sectionLabel: sectionLabel,
    failure: failure,
    onBack: () => Navigator.of(context).maybePop(),
    onRetry: () => unawaited(retry()),
  );

  Widget _retryLoaded(
    String message,
    JourneyReadStatus status,
    Future<void> Function() retry,
  ) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(message),
      if (status == JourneyReadStatus.unavailable)
        TextButton(onPressed: retry, child: const Text('Retry')),
    ],
  );

  String _stopTimes(JourneyStopStatus stop) {
    final values = <String>[];
    if (stop.plannedArrivalAt != null) {
      values.add('Planned arrival ${stop.plannedArrivalAt!.rawValue}');
    }
    if (stop.plannedDepartureAt != null) {
      values.add('Planned departure ${stop.plannedDepartureAt!.rawValue}');
    }
    if (stop.operationalArrivalAt != null) {
      values.add('Operational arrival ${stop.operationalArrivalAt!.rawValue}');
    }
    if (stop.operationalDepartureAt != null) {
      values.add(
        'Operational departure ${stop.operationalDepartureAt!.rawValue}',
      );
    }
    if (stop.platform != null) values.add('Platform ${stop.platform}');
    if (stop.timingSource != null) {
      values.add('Timing source ${stop.timingSource}');
    }
    if (stop.timingUpdatedAt != null) {
      values.add('Timing updated ${stop.timingUpdatedAt!.rawValue}');
    }
    return values.isEmpty ? 'Schedule details unavailable' : values.join(' · ');
  }
}
