import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';

class JourneyUpdateAlertBanner extends StatelessWidget {
  const JourneyUpdateAlertBanner({required this.alert, super.key});

  final JourneyUpdateAlert alert;

  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    label:
        'New journey update. ${alert.summary}. Status ${alert.update.status.apiValue}. '
        'Source ${alert.update.source.apiValue}. Published ${alert.update.publishedAt.rawValue}. '
        'Revision ${alert.revision}. Update ID ${alert.updateId}.',
    child: Card(
      child: ListTile(
        leading: const Icon(Icons.info_outline),
        title: Text(alert.summary),
        subtitle: Text(
          'Status ${alert.update.status.apiValue} · ${alert.update.source.apiValue} · '
          '${alert.update.publishedAt.rawValue} · Revision ${alert.revision} · Update ${alert.updateId}',
        ),
      ),
    ),
  );
}
