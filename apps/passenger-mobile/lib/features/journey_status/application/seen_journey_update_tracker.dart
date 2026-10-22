import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';

/// Tracks successfully observed public update IDs for the lifetime of the app.
/// The first successful response for each journey is a silent baseline.
class SeenJourneyUpdateTracker {
  final Map<String, Set<String>> _seenByJourney = {};

  List<JourneyUpdate> observe(
    String journeyInstanceId,
    List<JourneyUpdate> updates,
  ) {
    final seen = _seenByJourney[journeyInstanceId];
    if (seen == null) {
      _seenByJourney[journeyInstanceId] = updates
          .map((item) => item.updateId)
          .toSet();
      return const [];
    }
    final newItems = <JourneyUpdate>[];
    for (final update in updates) {
      if (update.journeyInstanceId != journeyInstanceId) continue;
      if (seen.add(update.updateId)) newItems.add(update);
    }
    return List.unmodifiable(newItems);
  }

  void reset() => _seenByJourney.clear();
}
