import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_candidates_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_recovery_section.dart';

class JourneyLookupScreen extends ConsumerStatefulWidget {
  const JourneyLookupScreen({
    required this.request,
    required this.onJourneySelected,
    super.key,
  });

  final JourneyLookupRequest request;
  final ValueChanged<JourneyCandidate> onJourneySelected;

  @override
  ConsumerState<JourneyLookupScreen> createState() =>
      _JourneyLookupScreenState();
}

class _JourneyLookupScreenState extends ConsumerState<JourneyLookupScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) {
        ref.read(journeyLookupNotifierProvider(widget.request).notifier).load();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(journeyLookupNotifierProvider(widget.request));
    return Scaffold(
      appBar: AppBar(
        title: const Text('Journey status'),
        leading: Semantics(
          button: true,
          label: 'Back',
          onTap: () => Navigator.of(context).maybePop(),
          child: ExcludeSemantics(
            child: IconButton(
              tooltip: 'Back',
              icon: const Icon(Icons.arrow_back),
              onPressed: () => Navigator.of(context).maybePop(),
            ),
          ),
        ),
      ),
      body: SafeArea(child: _body(state)),
    );
  }

  Widget _body(JourneyLookupState state) => switch (state.status) {
    JourneyLookupStatus.initial || JourneyLookupStatus.loading => Center(
      child: Semantics(
        liveRegion: true,
        label: 'Loading dated journey information',
        child: CircularProgressIndicator(),
      ),
    ),
    JourneyLookupStatus.empty => const Center(
      child: Padding(
        padding: EdgeInsets.all(24),
        child: Text(
          'Dated journey information is not available for this service and date.',
          textAlign: TextAlign.center,
          semanticsLabel: 'No matching dated journey is available.',
        ),
      ),
    ),
    JourneyLookupStatus.ready => JourneyCandidatesScreen(
      request: widget.request,
      onJourneySelected: widget.onJourneySelected,
    ),
    JourneyLookupStatus.unavailable ||
    JourneyLookupStatus.invalid ||
    JourneyLookupStatus.accessDenied ||
    JourneyLookupStatus.notFound => _failure(state),
  };

  Widget _failure(JourneyLookupState state) {
    final failure = state.failure;
    if (failure == null) {
      return const Center(
        child: Text('Dated journey information is unavailable.'),
      );
    }
    return Padding(
      padding: const EdgeInsets.all(24),
      child: JourneyRecoverySection(
        sectionLabel: 'Dated journey information',
        failure: failure,
        onBack: () => Navigator.of(context).maybePop(),
        retryKey: const Key('retry-journey-lookup'),
        onRetry: () => ref
            .read(journeyLookupNotifierProvider(widget.request).notifier)
            .retry(),
      ),
    );
  }
}
