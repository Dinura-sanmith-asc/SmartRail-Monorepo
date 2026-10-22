import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';

class JourneyUpdatesList extends StatelessWidget {
  const JourneyUpdatesList({required this.updates, super.key});

  final List<JourneyUpdate> updates;

  @override
  Widget build(BuildContext context) {
    if (updates.isEmpty) return const Text('No published updates.');
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final update in updates)
          Card(
            child: ListTile(
              title: Text(_summary(update)),
              subtitle: Text(
                'Revision ${update.revision} · ${update.source.apiValue} · '
                '${update.publishedAt.rawValue} · Update ${update.updateId}'
                '${update.correctionOfUpdateId == null ? '' : ' · Corrects ${update.correctionOfUpdateId}'}',
              ),
            ),
          ),
      ],
    );
  }

  String _summary(JourneyUpdate update) => switch (update.publicChange) {
    JourneyStatusChange(:final status) => 'Status: ${status.apiValue}',
    JourneyStopChange(
      :final platform,
      :final operationalArrivalAt,
      :final operationalDepartureAt,
    ) =>
      'Stop update${platform == null ? '' : ' · Platform $platform'}'
          '${operationalArrivalAt == null ? '' : ' · Arrival ${operationalArrivalAt.rawValue}'}'
          '${operationalDepartureAt == null ? '' : ' · Departure ${operationalDepartureAt.rawValue}'}',
    JourneyRouteDiversion(:final divertedStops) =>
      'Route changed · '
          '${divertedStops.map(_divertedStopLabel).join(' · ')}',
    JourneyCorrectionChange(:final correctedChange) =>
      'Correction · ${_changeLabel(correctedChange)}',
  };

  String _changeLabel(JourneyPublicChange change) => switch (change) {
    JourneyStatusChange(:final status) => 'status ${status.apiValue}',
    JourneyStopChange(
      :final platform,
      :final operationalArrivalAt,
      :final operationalDepartureAt,
    ) =>
      'stop details${platform == null ? '' : ', platform $platform'}'
          '${operationalArrivalAt == null ? '' : ', arrival ${operationalArrivalAt.rawValue}'}'
          '${operationalDepartureAt == null ? '' : ', departure ${operationalDepartureAt.rawValue}'}',
    JourneyRouteDiversion(:final divertedStops) =>
      'route change: ${divertedStops.map(_divertedStopLabel).join(' · ')}',
    JourneyCorrectionChange(:final correctedChange) =>
      'nested correction of ${_changeLabel(correctedChange)}',
  };

  String _divertedStopLabel(JourneyDivertedStop stop) =>
      'stop ${stop.stopOrder} station ${stop.stationId}'
      '${stop.scheduledArrivalAt == null ? '' : ', planned arrival ${stop.scheduledArrivalAt!.rawValue}'}'
      '${stop.scheduledDepartureAt == null ? '' : ', planned departure ${stop.scheduledDepartureAt!.rawValue}'}'
      '${stop.operationalArrivalAt == null ? '' : ', operational arrival ${stop.operationalArrivalAt!.rawValue}'}'
      '${stop.operationalDepartureAt == null ? '' : ', operational departure ${stop.operationalDepartureAt!.rawValue}'}'
      '${stop.platform == null ? '' : ', platform ${stop.platform}'}';
}
