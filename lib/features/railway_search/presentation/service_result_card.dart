import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/scheduled_timestamp.dart';

class ServiceResultCard extends StatelessWidget {
  const ServiceResultCard({
    required this.choice,
    required this.travelDate,
    required this.onTap,
    super.key,
  });

  final ScheduledServiceChoice choice;
  final TravelDate travelDate;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final origin = ScheduledTimestamp.parse(choice.origin.scheduledAt);
    final destination = ScheduledTimestamp.parse(
      choice.destination.scheduledAt,
    );
    final searchDate = travelDate.toIsoDate();
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Semantics(
          button: true,
          label:
              'Service ${choice.serviceCode}, route ${choice.routeCode}. '
              'Planned departure from ${choice.origin.stationName} on '
              '${origin.localDate} at ${origin.localTime} ${origin.offset}. '
              'Planned arrival at ${choice.destination.stationName} on '
              '${destination.localDate} at ${destination.localTime} ${destination.offset}'
              '${destination.isLaterCalendarDayThan(searchDate) ? ', next day' : ''}.',
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        choice.serviceCode,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ),
                    Text('Route ${choice.routeCode}'),
                  ],
                ),
                const SizedBox(height: 8),
                const Text('Planned schedule'),
                const SizedBox(height: 8),
                _CallSummary(
                  label: 'From',
                  stationName: choice.origin.stationName,
                  timestamp: origin,
                ),
                const SizedBox(height: 8),
                _CallSummary(
                  label: 'To',
                  stationName: choice.destination.stationName,
                  timestamp: destination,
                  nextDay: destination.isLaterCalendarDayThan(searchDate),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _CallSummary extends StatelessWidget {
  const _CallSummary({
    required this.label,
    required this.stationName,
    required this.timestamp,
    this.nextDay = false,
  });

  final String label;
  final String stationName;
  final ScheduledTimestamp timestamp;
  final bool nextDay;

  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      SizedBox(width: 48, child: Text('$label:')),
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(stationName),
            Text(timestamp.localDate),
            Text('${timestamp.localTime} ${timestamp.offset}'),
            if (nextDay) const Text('next day'),
          ],
        ),
      ),
    ],
  );
}
