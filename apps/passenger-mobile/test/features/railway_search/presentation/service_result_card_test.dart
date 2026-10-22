import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_result_card.dart';

void main() {
  testWidgets('shows supplied planned date and local time with later-day cue', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ServiceResultCard(
            choice: _choice(),
            travelDate: TravelDate(year: 2026, month: 10, day: 19),
            onTap: () {},
          ),
        ),
      ),
    );

    expect(find.text('Planned schedule'), findsOneWidget);
    expect(find.text('Central'), findsOneWidget);
    expect(find.text('2026-10-19'), findsOneWidget);
    expect(find.text('23:55:00 +05:30'), findsOneWidget);
    expect(find.text('Harbour'), findsOneWidget);
    expect(find.text('2026-10-20'), findsOneWidget);
    expect(find.text('00:30:00 +05:30'), findsOneWidget);
    expect(find.text('next day'), findsOneWidget);
    expect(find.text('SR-14'), findsOneWidget);
  });

  testWidgets('does not add a next-day cue for same-day scheduled calls', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: ServiceResultCard(
            choice: _choice(destinationAt: '2026-10-19T23:59:00+05:30'),
            travelDate: TravelDate(year: 2026, month: 10, day: 19),
            onTap: () {},
          ),
        ),
      ),
    );

    expect(find.text('next day'), findsNothing);
    expect(find.text('23:59:00 +05:30'), findsOneWidget);
  });
}

ScheduledServiceChoice _choice({
  String destinationAt = '2026-10-20T00:30:00+05:30',
}) => ScheduledServiceChoice(
  serviceId: 'service-1',
  serviceCode: 'SR-14',
  routeId: 'route-1',
  routeCode: 'R1',
  scheduleRevision: 2,
  origin: const ScheduledCall(
    stationId: 'origin',
    stationName: 'Central',
    stopOrder: 1,
    scheduledAt: '2026-10-19T23:55:00+05:30',
  ),
  destination: ScheduledCall(
    stationId: 'destination',
    stationName: 'Harbour',
    stopOrder: 4,
    scheduledAt: destinationAt,
  ),
);
