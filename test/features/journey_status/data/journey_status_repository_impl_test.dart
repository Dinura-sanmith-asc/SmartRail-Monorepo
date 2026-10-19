import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_api_dtos.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_api.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_repository_impl.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';

Object _fixture(String name) => jsonDecode(
  File('test/features/journey_status/data/fixtures/$name').readAsStringSync(),
);

class _FakeJourneyApi implements JourneyStatusApiClient {
  List<StationDepartureDto> departures = [];
  JourneySnapshotDto? snapshot;
  List<JourneyUpdateDto> updates = [];
  Object? failure;
  String? stationId;
  String? date;
  String? journeyId;
  String? lastOperation;

  @override
  Future<List<StationDepartureDto>> getStationDepartures({
    required String stationId,
    required String date,
  }) async {
    lastOperation = 'departures';
    this.stationId = stationId;
    this.date = date;
    if (failure case final error?) throw error;
    return departures;
  }

  @override
  Future<JourneySnapshotDto> getJourneySnapshot({
    required String journeyInstanceId,
  }) async {
    lastOperation = 'snapshot';
    journeyId = journeyInstanceId;
    if (failure case final error?) throw error;
    return snapshot!;
  }

  @override
  Future<List<JourneyUpdateDto>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    lastOperation = 'updates';
    journeyId = journeyInstanceId;
    if (failure case final error?) throw error;
    return updates;
  }
}

void main() {
  late _FakeJourneyApi fake;
  late JourneyStatusRepositoryImpl repository;

  setUp(() {
    fake = _FakeJourneyApi();
    repository = JourneyStatusRepositoryImpl(fake);
  });

  test(
    'maps departure identity/date values and preserves candidates',
    () async {
      fake.departures = StationDeparturesDto.fromJson(
        _fixture('station_departures.json'),
      ).items;

      final candidates = await repository.getStationDepartures(
        stationId: 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1',
        date: '2026-10-19',
      );

      expect(fake.lastOperation, 'departures');
      expect(fake.stationId, 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1');
      expect(fake.date, '2026-10-19');
      expect(candidates.map((item) => item.journeyInstanceId), [
        'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
        'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2',
      ]);
      expect(
        candidates.first.serviceId,
        'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1',
      );
      expect(candidates.last.operationDate, '2026-10-18');
      expect(candidates.last.stationLocalDepartureDate, '2026-10-19');
      expect(candidates.last.scheduledDepartureAt, isNull);
      expect(
        candidates.last.operationalDepartureAt!.rawValue,
        '2026-10-19T09:00:00+05:30',
      );
    },
  );

  test('maps baseline and ETA snapshots without deriving values', () async {
    fake.snapshot = JourneySnapshotDto.fromJson(
      _fixture('journey_snapshot_baseline.json'),
    );
    final snapshot = await repository.getJourneySnapshot(
      journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
    );
    expect(fake.journeyId, snapshot.journeyInstanceId);
    expect(fake.journeyId, isNot(snapshot.serviceId));
    expect(snapshot.source, JourneyDataSource.scheduledBaseline);
    expect(snapshot.publishedAt, isNull);
    expect(snapshot.eta, isNull);
    expect(
      snapshot.stops.single.plannedDepartureAt!.rawValue,
      '2026-10-19T08:30:00+05:30',
    );
    expect(snapshot.stops.single.operationalDepartureAt, isNull);

    fake.snapshot = JourneySnapshotDto.fromJson(
      _fixture('journey_snapshot_operational.json'),
    );
    final operational = await repository.getJourneySnapshot(
      journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2',
    );
    expect(operational.status, JourneyStatus.delayed);
    expect(operational.eta!.value.rawValue, '2026-10-19T11:45:00+05:30');
    expect(operational.eta!.freshness, JourneyEstimateFreshness.current);
  });

  test('maps each public change type and keeps API update order', () async {
    fake.updates = JourneyUpdatesDto.fromJson(
      _fixture('journey_updates_populated.json'),
    ).items;
    final updates = await repository.getJourneyUpdates(
      journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2',
    );

    expect(updates.map((item) => item.revision), [4, 5, 6, 7]);
    expect(updates.map((item) => item.kind), [
      JourneyUpdateKind.status,
      JourneyUpdateKind.stopChange,
      JourneyUpdateKind.routeDiversion,
      JourneyUpdateKind.correction,
    ]);
    expect(updates[0].publicChange, isA<JourneyStatusChange>());
    expect(updates[1].publicChange, isA<JourneyStopChange>());
    expect(updates[2].publicChange, isA<JourneyRouteDiversion>());
    expect(updates[3].publicChange, isA<JourneyCorrectionChange>());
    expect(
      updates.last.correctionOfUpdateId,
      'ffffffff-ffff-4fff-8fff-fffffffffff2',
    );
    expect(
      (updates[2].publicChange as JourneyRouteDiversion)
          .divertedStops
          .single
          .stopOrder,
      2,
    );
  });

  test(
    'maps empty history as successful empty and preserves API failures',
    () async {
      fake.updates = JourneyUpdatesDto.fromJson(
        _fixture('journey_updates_empty.json'),
      ).items;
      expect(
        await repository.getJourneyUpdates(
          journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
        ),
        isEmpty,
      );

      final failure = ApiFailure.protocol(RailwayApiOperation.journeyUpdates);
      fake.failure = failure;
      await expectLater(
        repository.getJourneyUpdates(
          journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
        ),
        throwsA(same(failure)),
      );
    },
  );
}
