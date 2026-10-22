import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';

class JourneyUpdateAlert {
  const JourneyUpdateAlert({
    required this.updateId,
    required this.revision,
    required this.update,
    required this.summary,
  });

  final String updateId;
  final int revision;
  final JourneyUpdate update;
  final String summary;
}

/// Classifies only documented public updates; no private or inferred reason is used.
class JourneyUpdateAlerts {
  static JourneyUpdateAlert? classify(JourneyUpdate update) {
    final summary = _summary(
      update.publicChange,
      correctionOf: update.correctionOfUpdateId,
    );
    if (summary == null) return null;
    return JourneyUpdateAlert(
      updateId: update.updateId,
      revision: update.revision,
      update: update,
      summary: summary,
    );
  }

  static String? _summary(
    JourneyPublicChange change, {
    String? correctionOf,
  }) => switch (change) {
    JourneyStatusChange(:final status) => switch (status) {
      JourneyStatus.delayed => 'Journey delayed',
      JourneyStatus.cancelled => 'Journey cancelled',
      JourneyStatus.scheduled ||
      JourneyStatus.running ||
      JourneyStatus.interrupted ||
      JourneyStatus.completed ||
      JourneyStatus.terminated => null,
    },
    JourneyStopChange(
      :final platform,
      :final operationalArrivalAt,
      :final operationalDepartureAt,
    ) =>
      platform != null ||
              operationalArrivalAt != null ||
              operationalDepartureAt != null
          ? 'Stop details changed'
                '${platform == null ? '' : ' · platform $platform'}'
                '${operationalArrivalAt == null ? '' : ' · operational arrival ${operationalArrivalAt.rawValue}'}'
                '${operationalDepartureAt == null ? '' : ' · operational departure ${operationalDepartureAt.rawValue}'}'
          : null,
    JourneyRouteDiversion(:final divertedStops) =>
      divertedStops.isNotEmpty
          ? 'Route changed · ${divertedStops.length} published stop changes'
          : null,
    JourneyCorrectionChange(:final correctedChange) =>
      _summary(correctedChange) == null
          ? null
          : 'Correction${correctionOf == null ? '' : ' to update $correctionOf'} · ${_summary(correctedChange)}',
  };
}
