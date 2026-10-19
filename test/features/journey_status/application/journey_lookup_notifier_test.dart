import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

void main() {
  const journeyId1 = '11111111-1111-4111-8111-111111111111';
  const journeyId2 = '22222222-2222-4222-8222-222222222222';
  const serviceId = '33333333-3333-4333-8333-333333333333';
  const otherServiceId = '44444444-4444-4444-8444-444444444444';
  const stationId = '55555555-5555-4555-8555-555555555555';
  final travelDate = TravelDate.parse('2026-10-20');

  test('loads departures, filters exact service and station-local date, preserves order', () async {
    final first = candidate(
      journeyId1,
      serviceId: serviceId,
      date: '2026-10-20',
    );
    final wrongDate = candidate(
      journeyId2,
      serviceId: serviceId,
      date: '2026-10-21',
    );
    final wrongService = candidate(
      journeyId2,
      serviceId: otherServiceId,
      date: '2026-10-20',
    );
    final second = candidate(
      journeyId2,
      serviceId: serviceId,
      date: '2026-10-20',
    );
    final repository = FakeJourneyRepository([
      first,
      wrongDate,
      wrongService,
      second,
    ]);
    final container = ProviderContainer(
      overrides: [
        journeyStatusRepositoryProvider.overrideWithValue(repository),
      ],
    );
    addTearDown(container.dispose);
    final request = lookupRequest(serviceId, stationId, travelDate);

    await container
        .read(journeyLookupNotifierProvider(request).notifier)
        .load();

    expect(repository.departureStationIds, [stationId]);
    expect(repository.departureDates, ['2026-10-20']);
    expect(
      container.read(journeyLookupNotifierProvider(request)).status,
      JourneyLookupStatus.ready,
    );
    expect(
      container
          .read(journeyLookupNotifierProvider(request))
          .candidates
          .map((c) => c.journeyInstanceId),
      [journeyId1, journeyId2],
    );
  });

  test(
    'successful empty and filtered-empty results do not synthesize journeys',
    () async {
      final repository = FakeJourneyRepository([]);
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      addTearDown(container.dispose);
      final request = lookupRequest(serviceId, stationId, travelDate);

      await container
          .read(journeyLookupNotifierProvider(request).notifier)
          .load();

      final state = container.read(journeyLookupNotifierProvider(request));
      expect(state.status, JourneyLookupStatus.empty);
      expect(state.candidates, isEmpty);
      expect(state.selectedCandidate, isNull);
    },
  );

  test(
    'failure can retry and a later successful empty response clears it',
    () async {
      final repository = FakeJourneyRepository(
        [],
        failures: [ApiFailure.network(RailwayApiOperation.stationDepartures)],
      );
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      addTearDown(container.dispose);
      final request = lookupRequest(serviceId, stationId, travelDate);
      final notifier = container.read(
        journeyLookupNotifierProvider(request).notifier,
      );

      await notifier.load();
      expect(
        container.read(journeyLookupNotifierProvider(request)).canRetry,
        isTrue,
      );
      await notifier.retry();
      expect(repository.departureCalls, 2);
      expect(
        container.read(journeyLookupNotifierProvider(request)).status,
        JourneyLookupStatus.empty,
      );
    },
  );

  test(
    'completion after provider disposal does not update state or throw',
    () async {
      final pending = Completer<List<JourneyCandidate>>();
      final repository = FakeJourneyRepository([], pending: pending);
      final container = ProviderContainer(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
      );
      final request = lookupRequest(serviceId, stationId, travelDate);
      final notifier = container.read(
        journeyLookupNotifierProvider(request).notifier,
      );
      final load = notifier.load();
      container.dispose();
      pending.complete([
        candidate(journeyId1, serviceId: serviceId, date: '2026-10-20'),
      ]);
      await expectLater(load, completes);
    },
  );

  test('selection retains exact journey ID and cannot select an unlisted candidate', () async {
    final candidates = [
      candidate(journeyId1, serviceId: serviceId, date: '2026-10-20'),
      candidate(journeyId2, serviceId: serviceId, date: '2026-10-20'),
    ];
    final repository = FakeJourneyRepository(candidates);
    final container = ProviderContainer(
      overrides: [
        journeyStatusRepositoryProvider.overrideWithValue(repository),
      ],
    );
    addTearDown(container.dispose);
    final request = lookupRequest(serviceId, stationId, travelDate);
    final notifier = container.read(
      journeyLookupNotifierProvider(request).notifier,
    );
    await notifier.load();

    notifier.select(candidates.last);

    expect(
      container
          .read(journeyLookupNotifierProvider(request))
          .selectedCandidate
          ?.journeyInstanceId,
      journeyId2,
    );
    expect(
      () => notifier.select(
        candidate(
          '66666666-6666-4666-8666-666666666666',
          serviceId: serviceId,
          date: '2026-10-20',
        ),
      ),
      throwsArgumentError,
    );
  });
}

JourneyLookupRequest lookupRequest(
  String serviceId,
  String stationId,
  TravelDate date,
) => JourneyLookupRequest(
  choice: ScheduledServiceChoice(
    serviceId: serviceId,
    serviceCode: 'SR-1',
    routeId: '77777777-7777-4777-8777-777777777777',
    routeCode: 'R1',
    scheduleRevision: 1,
    origin: ScheduledCall(
      stationId: stationId,
      stationName: 'Origin',
      stopOrder: 1,
      scheduledAt: '08:00',
    ),
    destination: const ScheduledCall(
      stationId: '88888888-8888-4888-8888-888888888888',
      stationName: 'Destination',
      stopOrder: 2,
      scheduledAt: '09:00',
    ),
  ),
  travelDate: date,
);

JourneyCandidate candidate(
  String id, {
  required String serviceId,
  required String date,
  JourneyAmbiguity ambiguity = JourneyAmbiguity.none,
}) => JourneyCandidate(
  journeyInstanceId: id,
  serviceId: serviceId,
  operationDate: date,
  stationLocalDepartureDate: date,
  revision: 1,
  status: JourneyStatus.scheduled,
  stationId: '55555555-5555-4555-8555-555555555555',
  stopOrder: 1,
  scheduledDepartureAt: null,
  ambiguity: ambiguity,
  stationName: 'Origin',
);

class FakeJourneyRepository implements JourneyStatusRepository {
  FakeJourneyRepository(
    this.candidates, {
    this.failures = const [],
    this.pending,
  });
  final List<JourneyCandidate> candidates;
  final List<ApiFailure> failures;
  final Completer<List<JourneyCandidate>>? pending;
  final departureStationIds = <String>[];
  final departureDates = <String>[];
  int departureCalls = 0;

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) {
    departureCalls++;
    departureStationIds.add(stationId);
    departureDates.add(date);
    if (pending != null) return pending!.future;
    if (failures.isNotEmpty) return Future.error(failures.removeAt(0));
    return Future.value(candidates);
  }

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) => throw UnimplementedError();

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) => throw UnimplementedError();
}
