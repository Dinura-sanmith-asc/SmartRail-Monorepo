import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

void main() {
  const serviceId = '33333333-3333-4333-8333-333333333333';
  const originId = '55555555-5555-4555-8555-555555555555';
  final date = TravelDate.parse('2026-10-20');
  test('one unambiguous candidate is eligible to open using dated journey identity', () async {
    final item = candidate(
      '11111111-1111-4111-8111-111111111111',
      JourneyAmbiguity.none,
    );
    final container = containerWith([item]);
    addTearDown(container.dispose);
    final request = lookupRequest(serviceId, originId, date);
    final notifier = container.read(
      journeyLookupNotifierProvider(request).notifier,
    );
    await notifier.load();

    notifier.openUniqueUnambiguous();

    expect(
      container
          .read(journeyLookupNotifierProvider(request))
          .selectedCandidate
          ?.journeyInstanceId,
      item.journeyInstanceId,
    );
    expect(
      container
          .read(journeyLookupNotifierProvider(request))
          .selectedCandidate
          ?.serviceId,
      serviceId,
    );
  });

  test('ambiguous candidates with identical labels remain separate and require tap', () async {
    final first = candidate(
      '11111111-1111-4111-8111-111111111111',
      JourneyAmbiguity.ambiguous,
    );
    final second = candidate(
      '22222222-2222-4222-8222-222222222222',
      JourneyAmbiguity.ambiguous,
    );
    final container = containerWith([first, second]);
    addTearDown(container.dispose);
    final request = lookupRequest(serviceId, originId, date);
    final notifier = container.read(
      journeyLookupNotifierProvider(request).notifier,
    );
    await notifier.load();

    expect(
      container.read(journeyLookupNotifierProvider(request)).selectedCandidate,
      isNull,
    );
    expect(
      container.read(journeyLookupNotifierProvider(request)).candidates,
      hasLength(2),
    );
    notifier.select(second);
    expect(
      container
          .read(journeyLookupNotifierProvider(request))
          .selectedCandidate
          ?.journeyInstanceId,
      second.journeyInstanceId,
    );
  });

  test(
    'multiple candidates never choose the first row automatically',
    () async {
      final items = [
        candidate(
          '11111111-1111-4111-8111-111111111111',
          JourneyAmbiguity.none,
        ),
        candidate(
          '22222222-2222-4222-8222-222222222222',
          JourneyAmbiguity.none,
        ),
      ];
      final container = containerWith(items);
      addTearDown(container.dispose);
      final request = lookupRequest(serviceId, originId, date);
      await container
          .read(journeyLookupNotifierProvider(request).notifier)
          .load();
      expect(
        container
            .read(journeyLookupNotifierProvider(request))
            .selectedCandidate,
        isNull,
      );
    },
  );
}

ProviderContainer containerWith(List<JourneyCandidate> values) =>
    ProviderContainer(
      overrides: [
        journeyStatusRepositoryProvider.overrideWithValue(
          FakeRepository(values),
        ),
      ],
    );

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

JourneyCandidate candidate(String id, JourneyAmbiguity ambiguity) =>
    JourneyCandidate(
      journeyInstanceId: id,
      serviceId: '33333333-3333-4333-8333-333333333333',
      operationDate: '2026-10-20',
      stationLocalDepartureDate: '2026-10-20',
      revision: 1,
      status: JourneyStatus.scheduled,
      stationId: '55555555-5555-4555-8555-555555555555',
      stopOrder: 1,
      scheduledDepartureAt: null,
      ambiguity: ambiguity,
      stationName: 'Origin',
    );

class FakeRepository implements JourneyStatusRepository {
  FakeRepository(this.values);
  final List<JourneyCandidate> values;
  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => values;
  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) => throw UnimplementedError();
  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) => throw UnimplementedError();
}
