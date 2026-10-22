import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

void main() {
  group('Railway domain models', () {
    test(
      'TravelDate validates a calendar date and serializes without a timezone',
      () {
        final date = TravelDate.parse('2024-02-29');
        expect(date.toIsoDate(), '2024-02-29');
        expect(date, TravelDate(year: 2024, month: 2, day: 29));
        expect(() => TravelDate.parse('2026-02-29'), throwsArgumentError);
        expect(
          () => TravelDate.parse('2026-10-18T00:00:00Z'),
          throwsFormatException,
        );
      },
    );

    test(
      'SearchCriteria is submittable only with different stations and a date',
      () {
        expect(const SearchCriteria().isSubmittable, isFalse);
        expect(
          SearchCriteria(
            originStationId: 'station',
            destinationStationId: 'station',
            travelDate: TravelDate(year: 2026, month: 10, day: 18),
          ).isSubmittable,
          isFalse,
        );
        expect(
          SearchCriteria(
            originStationId: 'origin',
            destinationStationId: 'destination',
            travelDate: TravelDate(year: 2026, month: 10, day: 18),
          ).isSubmittable,
          isTrue,
        );
      },
    );

    test(
      'domain constraints reject empty names and invalid ordered revisions',
      () {
        expect(() => Station(id: 'id', name: ''), throwsArgumentError);
        expect(
          () =>
              RouteStop(stopOrder: 0, stationId: 'id', stationName: 'Central'),
          throwsArgumentError,
        );
        expect(
          () =>
              ServiceSchedule(revision: 0, firstDepartureTimeLocal: '00:00:00'),
          throwsArgumentError,
        );
      },
    );

    test('domain collections are immutable', () {
      final response = ServiceSearchResponse(
        originStationId: 'origin',
        destinationStationId: 'destination',
        travelDate: TravelDate(year: 2026, month: 10, day: 18),
        services: [],
      );
      expect(
        () => response.services.add(_serviceChoice()),
        throwsUnsupportedError,
      );
    });
  });
}

ScheduledServiceChoice _serviceChoice() => ScheduledServiceChoice(
  serviceId: 'service',
  serviceCode: 'S1',
  routeId: 'route',
  routeCode: 'R1',
  scheduleRevision: 1,
  origin: const ScheduledCall(
    stationId: 'origin',
    stationName: 'Central',
    stopOrder: 1,
    scheduledAt: '2026-10-18T08:00:00+05:30',
  ),
  destination: const ScheduledCall(
    stationId: 'destination',
    stationName: 'Harbour',
    stopOrder: 2,
    scheduledAt: '2026-10-18T10:00:00+05:30',
  ),
);
