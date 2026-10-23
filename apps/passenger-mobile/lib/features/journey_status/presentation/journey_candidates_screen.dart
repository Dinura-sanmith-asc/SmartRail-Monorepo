import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';

class JourneyCandidatesScreen extends ConsumerWidget {
  const JourneyCandidatesScreen({
    required this.request,
    required this.onJourneySelected,
    this.actionLabel = 'Open journey status',
    super.key,
  });

  final JourneyLookupRequest request;
  final ValueChanged<JourneyCandidate> onJourneySelected;
  final String actionLabel;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(journeyLookupNotifierProvider(request));
    final notifier = ref.read(journeyLookupNotifierProvider(request).notifier);
    final candidates = state.candidates;
    final unique =
        candidates.length == 1 &&
        candidates.single.ambiguity == JourneyAmbiguity.none;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (unique)
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: FilledButton(
              key: const Key('open-journey-candidate'),
              onPressed: () {
                notifier.openUniqueUnambiguous();
                onJourneySelected(candidates.single);
              },
              child: Text(actionLabel),
            ),
          ),
        Expanded(
          child: FocusTraversalGroup(
            policy: ReadingOrderTraversalPolicy(),
            child: ListView.builder(
              itemCount: candidates.length,
              itemBuilder: (context, index) {
                final candidate = candidates[index];
                final label = _candidateLabel(candidate);
                final ambiguous =
                    candidate.ambiguity == JourneyAmbiguity.ambiguous;
                final selected = identical(state.selectedCandidate, candidate);
                void selectCandidate() {
                  notifier.select(candidate);
                  onJourneySelected(candidate);
                }

                return Semantics(
                  button: true,
                  selected: selected,
                  onTap: selectCandidate,
                  container: true,
                  excludeSemantics: true,
                  label:
                      'Journey option ${index + 1} of ${candidates.length}. $label'
                      '${ambiguous ? '. Journey identity is ambiguous.' : ''}',
                  child: ListTile(
                    key: ValueKey('journey-option-${index + 1}'),
                    selected: selected,
                    title: Text(
                      candidate.stationName ?? 'Journey option ${index + 1}',
                    ),
                    subtitle: Text(
                      '$label${ambiguous ? ' · Ambiguous journey identity' : ''}',
                    ),
                    trailing: const Icon(Icons.chevron_right),
                    onTap: selectCandidate,
                  ),
                );
              },
            ),
          ),
        ),
      ],
    );
  }

  String _candidateLabel(JourneyCandidate candidate) {
    final parts = <String>[
      candidate.status.apiValue,
      'Station departure date ${candidate.stationLocalDepartureDate}',
      'Operation date ${candidate.operationDate}',
      if (candidate.scheduledDepartureAt != null)
        'Scheduled departure ${candidate.scheduledDepartureAt!.rawValue}',
      if (candidate.operationalDepartureAt != null)
        'Operational departure ${candidate.operationalDepartureAt!.rawValue}',
      if (candidate.platform != null) 'Platform ${candidate.platform}',
    ];
    return parts.join(' · ');
  }
}
