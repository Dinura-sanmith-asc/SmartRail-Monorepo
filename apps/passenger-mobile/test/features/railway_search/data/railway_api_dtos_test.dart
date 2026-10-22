import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_api_dtos.dart';

const _stationId = '11111111-1111-4111-8111-111111111111';
const _destinationId = '22222222-2222-4222-8222-222222222222';
const _serviceId = '33333333-3333-4333-8333-333333333333';
const _routeId = '44444444-4444-4444-8444-444444444444';

void main() {
  group('Railway API DTOs', () {
    test(
      'parses station lists, optional detail arrays and empty collections',
      () {
        final stations = StationListDto.fromJson({
          'items': [
            {'stationId': _stationId, 'name': 'Central'},
            {
              'stationId': _destinationId,
              'name': 'Harbour',
              'locationDetails': ['Near the harbour'],
              'serviceDetails': ['Accessible platform'],
            },
          ],
        });
        expect(stations.items, hasLength(2));
        expect(stations.items.first.locationDetails, isNull);
        expect(stations.items[1].locationDetails, ['Near the harbour']);
        expect(stations.items[1].serviceDetails, ['Accessible platform']);
        expect(StationListDto.fromJson({'items': []}).items, isEmpty);
      },
    );

    test('parses route stops without changing their returned order', () {
      final route = RouteStopsDto.fromJson({
        'routeId': _routeId,
        'routeCode': 'R1',
        'name': 'Coastal line',
        'stops': [
          {'stopOrder': 1, 'stationId': _stationId, 'stationName': 'Central'},
          {
            'stopOrder': 2,
            'stationId': _destinationId,
            'stationName': 'Harbour',
          },
        ],
      });
      expect(route.stops.map((stop) => stop.stopOrder), [1, 2]);
      expect(route.stops.last.stationName, 'Harbour');
      expect(
        RouteStopsDto.fromJson({
          'routeId': _routeId,
          'routeCode': 'R1',
          'name': 'Coastal line',
          'stops': [],
        }).stops,
        isEmpty,
      );
    });

    test('parses service choices and offset-bearing scheduled calls', () {
      final response = ServiceSearchResponseDto.fromJson({
        'originStationId': _stationId,
        'destinationStationId': _destinationId,
        'travelDate': '2026-10-18',
        'services': [
          {
            'serviceId': _serviceId,
            'serviceCode': 'SR-1',
            'routeId': _routeId,
            'routeCode': 'R1',
            'scheduleRevision': 2,
            'origin': {
              'stationId': _stationId,
              'stationName': 'Central',
              'stopOrder': 1,
              'scheduledAt': '2026-10-18T23:30:00+05:30',
            },
            'destination': {
              'stationId': _destinationId,
              'stationName': 'Harbour',
              'stopOrder': 2,
              'scheduledAt': '2026-10-19T02:15:00+05:30',
            },
          },
        ],
      });
      expect(response.travelDate, '2026-10-18');
      expect(
        response.services.single.origin.scheduledAt,
        '2026-10-18T23:30:00+05:30',
      );
      expect(
        response.services.single.destination.scheduledAt,
        '2026-10-19T02:15:00+05:30',
      );
      expect(
        ServiceSearchResponseDto.fromJson({
          'originStationId': _stationId,
          'destinationStationId': _destinationId,
          'travelDate': '2026-10-18',
          'services': [],
        }).services,
        isEmpty,
      );
    });

    test(
      'parses service details with documented nested route and schedule fields',
      () {
        final detail = ServiceDetailDto.fromJson({
          'serviceId': _serviceId,
          'serviceCode': 'SR-1',
          'route': {
            'routeId': _routeId,
            'routeCode': 'R1',
            'name': 'Coastal line',
          },
          'schedule': {'revision': 2, 'firstDepartureTimeLocal': '05:45:00'},
        });
        expect(detail.route.name, 'Coastal line');
        expect(detail.schedule.firstDepartureTimeLocal, '05:45:00');
      },
    );

    test('rejects invalid JSON and missing required fields', () {
      expect(() => decodeJsonObject('{not json'), throwsFormatException);
      expect(() => StationListDto.fromJson({}), throwsFormatException);
      expect(
        () => RouteStopsDto.fromJson({
          'routeId': _routeId,
          'routeCode': 'R1',
          'name': 'Line',
        }),
        throwsFormatException,
      );
    });

    test('rejects malformed UUIDs, dates, types and undocumented fields', () {
      expect(
        () =>
            StationDto.fromJson({'stationId': 'not-a-uuid', 'name': 'Central'}),
        throwsFormatException,
      );
      expect(
        () => StationDto.fromJson({'stationId': _stationId, 'name': ''}),
        throwsFormatException,
      );
      expect(
        () => StationDto.fromJson({
          'stationId': _stationId,
          'name': 'Central',
          'locationDetails': 'nearby',
        }),
        throwsFormatException,
      );
      expect(
        () => ServiceSearchResponseDto.fromJson({
          'originStationId': _stationId,
          'destinationStationId': _destinationId,
          'travelDate': '18-10-2026',
          'services': [],
        }),
        throwsFormatException,
      );
      expect(
        () => StationListDto.fromJson({'items': 'empty'}),
        throwsFormatException,
      );
    });
  });
}
