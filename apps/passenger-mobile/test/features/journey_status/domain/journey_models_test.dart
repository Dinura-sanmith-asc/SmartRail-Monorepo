import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';

const _journeyId = '45000000-0000-0000-0000-000000000019';
const _serviceId = '30000000-0000-0000-0000-000000000001';
const _scheduleId = 'cccccccc-cccc-4ccc-8ccc-cccccccccccc';
const _stationA = 'dddddddd-dddd-4ddd-8ddd-dddddddddddd';
const _stationB = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
const _stopA = 'ffffffff-ffff-4fff-8fff-ffffffffffff';
const _stopB = '11111111-1111-4111-8111-111111111111';

void main() {
  group('journey domain identity and source values', () {
    test('keeps journey, service, schedule and date identities distinct', () {
      final candidate = JourneyCandidate(
        journeyInstanceId: _journeyId,
        serviceId: _serviceId,
        operationDate: '2026-10-20',
        stationLocalDepartureDate: '2026-10-21',
        revision: 3,
        status: JourneyStatus.running,
        stationId: _stationA,
        stopOrder: 2,
        scheduledDepartureAt: JourneyTimestamp.parse(
          '2026-10-21T00:15:00+05:30',
        ),
        ambiguity: JourneyAmbiguity.ambiguous,
      );

      expect(candidate.journeyInstanceId, _journeyId);
      expect(candidate.serviceId, _serviceId);
      expect(candidate.journeyInstanceId, isNot(candidate.serviceId));
      expect(candidate.operationDate, '2026-10-20');
      expect(candidate.stationLocalDepartureDate, '2026-10-21');
      expect(
        candidate.scheduledDepartureAt!.rawValue,
        '2026-10-21T00:15:00+05:30',
      );
    });

    test('represents all seven official status values distinctly', () {
      expect(JourneyStatus.values, hasLength(7));
      expect(
        JourneyStatus.values.map((status) => status.apiValue).toList(),
        <String>[
          'Scheduled',
          'Running',
          'Delayed',
          'Interrupted',
          'Cancelled',
          'Completed',
          'Terminated',
        ],
      );
      expect(JourneyStatus.values.toSet(), hasLength(7));
    });

    test('keeps ambiguity values explicit', () {
      expect(JourneyAmbiguity.none.apiValue, 'none');
      expect(JourneyAmbiguity.ambiguous.apiValue, 'ambiguous');
    });

    test('retains supplied date, time and UTC offset without conversion', () {
      final timestamp = JourneyTimestamp.parse('2026-10-21T00:15:00+05:30');

      expect(timestamp.rawValue, '2026-10-21T00:15:00+05:30');
      expect(timestamp.localDate, '2026-10-21');
      expect(timestamp.localTime, '00:15:00');
      expect(timestamp.offset, '+05:30');
    });
  });

  group('journey model invariants', () {
    test('requires positive revisions and stop order', () {
      expect(
        () => JourneyCandidate(
          journeyInstanceId: _journeyId,
          serviceId: _serviceId,
          operationDate: '2026-10-20',
          stationLocalDepartureDate: '2026-10-20',
          revision: 0,
          status: JourneyStatus.scheduled,
          stationId: _stationA,
          stopOrder: 1,
          scheduledDepartureAt: null,
          ambiguity: JourneyAmbiguity.none,
        ),
        throwsArgumentError,
      );
      expect(
        () => JourneyStopStatus(
          journeyStopId: _stopA,
          stopOrder: 0,
          stationId: _stationA,
          ambiguity: JourneyAmbiguity.none,
        ),
        throwsArgumentError,
      );
    });

    test('snapshot retains ordered stops in an immutable collection', () {
      final sourceStops = <JourneyStopStatus>[
        JourneyStopStatus(
          journeyStopId: _stopA,
          stopOrder: 1,
          stationId: _stationA,
          ambiguity: JourneyAmbiguity.none,
        ),
        JourneyStopStatus(
          journeyStopId: _stopB,
          stopOrder: 2,
          stationId: _stationB,
          ambiguity: JourneyAmbiguity.ambiguous,
        ),
      ];
      final snapshot = JourneyStatusSnapshot(
        journeyInstanceId: _journeyId,
        serviceId: _serviceId,
        scheduleId: _scheduleId,
        scheduleRevision: 4,
        operationDate: '2026-10-20',
        firstDepartureOccurrence: '23:55:00',
        revision: 2,
        status: JourneyStatus.scheduled,
        stops: sourceStops,
        source: JourneyDataSource.scheduledBaseline,
        publishedAt: null,
      );

      expect(snapshot.stops.map((stop) => stop.journeyStopId), <String>[
        _stopA,
        _stopB,
      ]);
      expect(
        () => snapshot.stops.add(sourceStops.first),
        throwsUnsupportedError,
      );
      expect(snapshot.publishedAt, isNull);
      expect(snapshot.eta, isNull);
    });
  });
}
