import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/scheduled_timestamp.dart';

class ServiceDetailSummary extends StatelessWidget {
  const ServiceDetailSummary({
    required this.choice,
    required this.travelDate,
    this.detail,
    super.key,
  });

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;
  final ServiceDetail? detail;

  @override
  Widget build(BuildContext context) {
    final origin = ScheduledTimestamp.parse(choice.origin.scheduledAt);
    final destination = ScheduledTimestamp.parse(
      choice.destination.scheduledAt,
    );
    final travelDateText = travelDate.toIsoDate();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Service ${detail?.serviceCode ?? choice.serviceCode}',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        Text('Route ${detail?.route.routeCode ?? choice.routeCode}'),
        if (detail != null) Text(detail!.route.name),
        const SizedBox(height: 12),
        Text('Travel date: $travelDateText'),
        const Text('Planned schedule'),
        _callRow('Origin', choice.origin.stationName, origin),
        _callRow(
          'Destination',
          choice.destination.stationName,
          destination,
          nextDay: destination.isLaterCalendarDayThan(travelDateText),
        ),
        if (detail != null) ...[
          const SizedBox(height: 8),
          Text(
            'Planned first-call departure: ${detail!.schedule.firstDepartureTimeLocal}',
          ),
        ],
      ],
    );
  }

  Widget _callRow(
    String label,
    String stationName,
    ScheduledTimestamp timestamp, {
    bool nextDay = false,
  }) {
    final offset = timestamp.offset;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('$label · $stationName'),
          Text('${timestamp.localDate} ${timestamp.localTime} ($offset)'),
          if (nextDay) const Text('Next day'),
        ],
      ),
    );
  }
}
