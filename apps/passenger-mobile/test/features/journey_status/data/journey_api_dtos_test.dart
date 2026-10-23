import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_api_dtos.dart';

Object _fixture(String name) => jsonDecode(
  File('test/features/journey_status/data/fixtures/$name').readAsStringSync(),
);

void main() {
  group('journey API DTOs', () {
    test('accepts backend seeded GUID identifiers', () {
      final candidate =
          (_fixture('station_departures.json') as List).first
              as Map<String, dynamic>;
      candidate['journeyInstanceId'] = '45000000-0000-0000-0000-000000000019';
      candidate['serviceId'] = '30000000-0000-0000-0000-000000000001';
      final parsed = StationDeparturesDto.fromJson([candidate]);
      expect(
        parsed.items.single.journeyInstanceId,
        '45000000-0000-0000-0000-000000000019',
      );
    });

    test('parses departure candidates and preserves response order', () {
      final candidates = StationDeparturesDto.fromJson(
        _fixture('station_departures.json'),
      );
      expect(candidates.items, hasLength(2));
      expect(
        candidates.items.first.journeyInstanceId,
        'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
      );
      expect(
        candidates.items.first.scheduledDepartureAt,
        '2026-10-19T08:30:00+05:30',
      );
      expect(candidates.items.last.scheduledDepartureAt, isNull);
      expect(
        candidates.items.last.operationalDepartureAt,
        '2026-10-19T09:00:00+05:30',
      );
      expect(candidates.items.last.ambiguity, 'ambiguous');
      expect(
        StationDeparturesDto.fromJson(_fixture('empty_station_departures.json'))
            .items,
        isEmpty,
      );
    });

    test('accepts the current public station departure projection', () {
      final departure =
          Map<String, Object?>.from(
              ((_fixture('station_departures.json') as List).first as Map),
            )
            ..['scheduledArrivalAt'] = null
            ..['operationalArrivalAt'] = '2026-10-19T08:20:00+05:30'
            ..['serviceCode'] = 'DEV-SEED-REF-001'
            ..['displayName'] = 'Gampaha Express'
            ..['publicServiceNumber'] = 'T0008'
            ..['originName'] = 'Colombo Fort'
            ..['destinationName'] = 'Gampaha';
      final parsed = StationDepartureDto.fromJson(departure);
      expect(parsed.journeyInstanceId, departure['journeyInstanceId']);

      departure['operationalArrivalAt'] = 'not-a-timestamp';
      expect(
        () => StationDepartureDto.fromJson(departure),
        throwsFormatException,
      );
    });

    test('parses baseline and populated snapshots without UTC conversion', () {
      final baseline = JourneySnapshotDto.fromJson(
        _fixture('journey_snapshot_baseline.json'),
      );
      expect(baseline.publishedAt, isNull);
      expect(baseline.eta, isNull);
      expect(
        baseline.stops.single.plannedDepartureAt,
        '2026-10-19T08:30:00+05:30',
      );

      final operational = JourneySnapshotDto.fromJson(
        _fixture('journey_snapshot_operational.json'),
      );
      expect(operational.status, 'Delayed');
      expect(operational.eta!.value, '2026-10-19T11:45:00+05:30');
      expect(
        operational.stops.single.operationalArrivalAt,
        '2026-10-19T11:45:00+05:30',
      );
      expect(operational.publishedAt, '2026-10-19T09:20:00+05:30');
    });

    test('parses all public update change variants in returned order', () {
      final updates = JourneyUpdatesDto.fromJson(
        _fixture('journey_updates_populated.json'),
      );
      expect(updates.items.map((item) => item.revision), [4, 5, 6, 7]);
      expect(updates.items.map((item) => item.publicChange.changeType), [
        'status',
        'stopChange',
        'routeDiversion',
        'correction',
      ]);
      expect(
        updates.items.last.correctionOfUpdateId,
        'ffffffff-ffff-4fff-8fff-fffffffffff2',
      );
      expect(
        updates.items.last.publicChange.correctedChange!.changeType,
        'stopChange',
      );
      expect(
        JourneyUpdatesDto.fromJson(_fixture('journey_updates_empty.json'))
            .items,
        isEmpty,
      );
    });

    test('parses problem details and ignores safe extension fields', () {
      final problem = ProblemDetailsDto.fromJson({
        'type': 'about:blank',
        'title': 'Invalid request',
        'status': 400,
        'detail': 'Request input was invalid.',
        'instance': '/api/journeys/not-a-uuid',
        'errors': {
          'id': ['Must be a UUID.'],
        },
        'traceId': 'private extension ignored',
        'actor': {'subject': 'must not parse'},
      });
      expect(problem.status, 400);
      expect(problem.errors['id'], ['Must be a UUID.']);
      expect(problem.toString(), isNot(contains('private')));
    });

    test(
      'rejects invalid JSON, wrong roots, missing fields and malformed types',
      () {
        expect(() => decodeJourneyJson('{broken'), throwsFormatException);
        expect(
          () => StationDeparturesDto.fromJson({'items': []}),
          throwsFormatException,
        );
        expect(() => JourneySnapshotDto.fromJson({}), throwsFormatException);
        expect(
          () => JourneyUpdatesDto.fromJson({'not': 'an array'}),
          throwsFormatException,
        );
        expect(
          () => StationDepartureDto.fromJson({'journeyInstanceId': 4}),
          throwsFormatException,
        );
      },
    );

    test('rejects unknown properties on closed public schemas', () {
      final departure = Map<String, Object?>.from(
        (jsonDecode(
              File(
                'test/features/journey_status/data/fixtures/station_departures.json',
              ).readAsStringSync(),
            ) as List).first
            as Map,
      )..['actorSubject'] = 'private';
      expect(
        () => StationDepartureDto.fromJson(departure),
        throwsFormatException,
      );

      final snapshot = Map<String, Object?>.from(
        _fixture('journey_snapshot_baseline.json') as Map,
      )..['driverObservationId'] = 'private';
      expect(
        () => JourneySnapshotDto.fromJson(snapshot),
        throwsFormatException,
      );
    });

    test(
      'rejects null for optional fields that are non-nullable in OpenAPI',
      () {
        final source = Map<String, Object?>.from(
          ((_fixture('station_departures.json') as List).first as Map),
        )..['source'] = null;
        expect(
          () => StationDepartureDto.fromJson(source),
          throwsFormatException,
        );

        final stationName = Map<String, Object?>.from(
          ((_fixture('station_departures.json') as List).first as Map),
        )..['stationName'] = null;
        expect(
          () => StationDepartureDto.fromJson(stationName),
          throwsFormatException,
        );
      },
    );

    test(
      'rejects unknown enum values, malformed timestamps and corrections',
      () {
        final departure = Map<String, Object?>.from(
          ((_fixture('station_departures.json') as List).first as Map),
        )..['status'] = 'Boarding';
        expect(
          () => StationDepartureDto.fromJson(departure),
          throwsFormatException,
        );

        final badTimestamp = Map<String, Object?>.from(
          _fixture('journey_snapshot_baseline.json') as Map,
        );
        final stops = List<Map<String, Object?>>.from(
          (badTimestamp['stops'] as List).map(
            (item) => Map<String, Object?>.from(item as Map),
          ),
        );
        stops.first['plannedDepartureAt'] = '2026-10-19T08:30:00';
        badTimestamp['stops'] = stops;
        expect(
          () => JourneySnapshotDto.fromJson(badTimestamp),
          throwsFormatException,
        );

        final badTime = Map<String, Object?>.from(
          _fixture('journey_snapshot_baseline.json') as Map,
        )..['firstDepartureOccurrence'] = '25:61:00';
        expect(
          () => JourneySnapshotDto.fromJson(badTime),
          throwsFormatException,
        );

        for (final status in <String>[
          'Scheduled',
          'Running',
          'Delayed',
          'Interrupted',
          'Cancelled',
          'Completed',
          'Terminated',
        ]) {
          final knownStatus = Map<String, Object?>.from(
            ((_fixture('station_departures.json') as List).first as Map),
          )..['status'] = status;
          expect(StationDepartureDto.fromJson(knownStatus).status, status);
        }

        final invalidCorrection = Map<String, Object?>.from(
          ((_fixture('journey_updates_populated.json') as List).last as Map),
        )..remove('correctionOfUpdateId');
        expect(
          () => JourneyUpdateDto.fromJson(invalidCorrection),
          throwsFormatException,
        );
      },
    );
  });
}
