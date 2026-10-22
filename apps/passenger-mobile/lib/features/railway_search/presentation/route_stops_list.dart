import 'package:flutter/material.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

class RouteStopsList extends StatelessWidget {
  const RouteStopsList({required this.routeStops, super.key});

  final RouteStops routeStops;

  @override
  Widget build(BuildContext context) {
    if (routeStops.stops.isEmpty) {
      return const Text('No published stop details are available.');
    }
    final orderedStops = [...routeStops.stops]
      ..sort((a, b) => a.stopOrder.compareTo(b.stopOrder));
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('${routeStops.name} · ${routeStops.routeCode}'),
        for (final stop in orderedStops)
          ListTile(
            contentPadding: EdgeInsets.zero,
            leading: CircleAvatar(child: Text('${stop.stopOrder}')),
            title: Text(stop.stationName),
          ),
      ],
    );
  }
}
