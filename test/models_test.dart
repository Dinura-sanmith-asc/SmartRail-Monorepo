import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_driver_mobile/features/assignments/domain/models.dart';
import 'package:smartrail_driver_mobile/features/authentication/application/app_controller.dart';

void main() {
  test('identity takes backend role labels as authority', () {
    expect(
      DriverIdentity.fromJson({
        'subject': 'opaque-sub',
        'roles': ['Driver'],
      }).isDriver,
      isTrue,
    );
    expect(
      DriverIdentity.fromJson({
        'subject': 'opaque-sub',
        'roles': ['Controller'],
      }).isDriver,
      isFalse,
    );
    expect(
      DriverIdentity.fromJson({'subject': 'opaque-sub', 'roles': []}).isDriver,
      isFalse,
    );
  });

  test('restored application session requires an access token and current Driver identity', () {
    const driver = DriverIdentity(subject: 'driver-sub', roles: ['Driver']);
    const controller = DriverIdentity(
      subject: 'controller-sub',
      roles: ['Controller'],
    );
    expect(
      AppState(token: 'opaque-access-token', identity: driver).signedIn,
      isTrue,
    );
    expect(
      AppState(token: 'opaque-access-token', identity: controller).signedIn,
      isFalse,
    );
    expect(AppState(identity: driver).signedIn, isFalse);
  });

  test('assignment maps Feature 005 projection and preserves ordered stops and official update evidence', () {
    final assignment = Assignment.fromJson({
      'assignmentId': 'assignment-1',
      'driverSubject': 'driver-sub',
      'journey': {
        'snapshot': {
          'journeyInstanceId': 'journey-1',
          'serviceId': 'service-1',
          'scheduleId': 'schedule-1',
          'scheduleRevision': 2,
          'operationDate': '2026-10-20',
          'firstDepartureOccurrence': '23:30:00',
          'revision': 3,
          'status': 'Delayed',
          'source': 'ControllerManual',
          'publishedAt': '2026-10-20T18:00:00Z',
          'lastUpdatedAt': '2026-10-20T18:01:00Z',
          'stops': [
            {
              'journeyStopId': 'stop-2',
              'stopOrder': 2,
              'stationId': 'station-2',
              'stationName': 'Colombo Fort',
              'plannedArrivalAt': '2026-10-20T20:00:00Z',
              'plannedDepartureAt': null,
              'operationalArrivalAt': null,
              'operationalDepartureAt': null,
              'platform': null,
              'timingSource': null,
              'timingUpdatedAt': null,
              'ambiguity': 'none',
            },
            {
              'journeyStopId': 'stop-1',
              'stopOrder': 1,
              'stationId': 'station-1',
              'stationName': 'Kandy',
              'plannedArrivalAt': null,
              'plannedDepartureAt': '2026-10-20T18:00:00Z',
              'operationalArrivalAt': null,
              'operationalDepartureAt': null,
              'platform': '2',
              'timingSource': null,
              'timingUpdatedAt': null,
              'ambiguity': 'none',
            },
          ],
        },
        'updates': [
          {
            'updateId': 'update-1',
            'journeyInstanceId': 'journey-1',
            'revision': 3,
            'kind': 'status',
            'status': 'Delayed',
            'source': 'ControllerManual',
            'publishedAt': '2026-10-20T18:01:00Z',
            'correctionOfUpdateId': null,
            'publicChange': {'changeType': 'status', 'status': 'Delayed'},
          },
        ],
      },
    });
    expect(assignment.journey.stops.map((s) => s.order), [1, 2]);
    expect(assignment.journey.stops.first.stationName, 'Kandy');
    expect(assignment.journey.status, 'Delayed');
    expect(assignment.journey.revision, 3);
    expect(assignment.updates.single.publicChange.type, 'status');
    expect(assignment.updates.single.summary, 'Official status: Delayed');
    expect(assignment.updates.single.publishedAt.isUtc, isTrue);
  });

  test('journey timestamps accept nullable operational times without inventing values', () {
    final stop = JourneyStop.fromJson({
      'journeyStopId': 's',
      'stopOrder': 1,
      'stationId': 'st',
      'plannedArrivalAt': null,
      'plannedDepartureAt': null,
      'operationalArrivalAt': null,
      'operationalDepartureAt': null,
    });
    expect(stop.plannedArrivalAt, isNull);
    expect(stop.operationalDepartureAt, isNull);
  });

  test(
    'observation maps author and UTC receipt time from the backend response',
    () {
      final result = DriverObservation.fromJson({
        'observationId': 'obs-1',
        'journeyInstanceId': 'journey-1',
        'assignmentId': 'assignment-1',
        'driverSubject': 'verified-subject',
        'text': 'Observed at station',
        'reportedAtUtc': '2026-10-20T18:15:00Z',
        'journeyRevision': 3,
      });
      expect(result.driverSubject, 'verified-subject');
      expect(result.reportedAtUtc.isUtc, isTrue);
      expect(result.journeyRevision, 3);
    },
  );
}
