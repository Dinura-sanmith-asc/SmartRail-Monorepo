import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_controller.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

final _origin = Station(id: 'origin', name: 'Central');
final _destination = Station(id: 'destination', name: 'Harbour');

void main() {
  test(
    'starts loading stations with unset criteria and accepts published choices',
    () async {
      final repository = _FakeRepository(stations: [_origin, _destination]);
      final container = _container(repository);
      addTearDown(container.dispose);

      expect(
        container.read(searchControllerProvider).stationStatus,
        StationLoadStatus.loading,
      );
      await _settle(container);

      final state = container.read(searchControllerProvider);
      expect(state.stationStatus, StationLoadStatus.ready);
      expect(state.stations.map((station) => station.name), [
        'Central',
        'Harbour',
      ]);
      expect(state.criteria.originStationId, isNull);
      expect(state.criteria.destinationStationId, isNull);
      expect(state.criteria.travelDate, isNull);
    },
  );

  test('represents an empty published station collection separately', () async {
    final container = _container(_FakeRepository());
    addTearDown(container.dispose);
    await _settle(container);
    expect(
      container.read(searchControllerProvider).stationStatus,
      StationLoadStatus.empty,
    );
    expect(container.read(searchControllerProvider).stations, isEmpty);
  });

  test(
    'represents failure and retries station retrieval without losing criteria',
    () async {
      final repository = _FakeRepository(
        stations: [_origin, _destination],
        failFirstStationRead: true,
      );
      final container = _container(repository);
      addTearDown(container.dispose);
      await _settle(container);
      final controller = container.read(searchControllerProvider.notifier);
      expect(
        container.read(searchControllerProvider).stationStatus,
        StationLoadStatus.unavailable,
      );

      await controller.retryStations();

      final state = container.read(searchControllerProvider);
      expect(state.stationStatus, StationLoadStatus.ready);
      expect(repository.stationCalls, 2);
    },
  );

  test('retains selected values when station retrieval is retried', () async {
    final repository = _FakeRepository(stations: [_origin, _destination]);
    final container = _container(repository);
    addTearDown(container.dispose);
    await _settle(container);
    final controller = container.read(searchControllerProvider.notifier);
    controller.selectOrigin(_origin.id);
    controller.selectDestination(_destination.id);
    controller.selectTravelDate(DateTime(2026, 10, 18));

    await controller.retryStations();
    final state = container.read(searchControllerProvider);
    expect(state.stationStatus, StationLoadStatus.ready);
    expect(state.criteria.originStationId, _origin.id);
    expect(state.criteria.destinationStationId, _destination.id);
    expect(
      state.criteria.travelDate,
      TravelDate(year: 2026, month: 10, day: 18),
    );
    expect(repository.stationCalls, 2);
  });

  test(
    'supports changing and clearing each criterion without submitting a search',
    () async {
      final repository = _FakeRepository(stations: [_origin, _destination]);
      final container = _container(repository);
      addTearDown(container.dispose);
      await _settle(container);
      final controller = container.read(searchControllerProvider.notifier);

      controller.selectOrigin(_origin.id);
      controller.selectDestination(_destination.id);
      controller.selectTravelDate(DateTime(2026, 10, 18));
      expect(
        container.read(searchControllerProvider).criteria.isSubmittable,
        isTrue,
      );
      controller.selectDestination(null);
      expect(
        container.read(searchControllerProvider).criteria.destinationStationId,
        isNull,
      );
      expect(
        container
            .read(searchControllerProvider)
            .criteria
            .travelDate!
            .toIsoDate(),
        '2026-10-18',
      );
      expect(repository.searchCalls, 0);
    },
  );

  test('keeps valid selections when local validation fails', () async {
    final repository = _FakeRepository(stations: [_origin, _destination]);
    final container = _container(repository);
    addTearDown(container.dispose);
    await _settle(container);
    final controller = container.read(searchControllerProvider.notifier);
    controller.selectOrigin(_origin.id);
    controller.selectTravelDate(DateTime(2026, 10, 18));

    expect(controller.validateCriteria(), isFalse);
    final criteria = container.read(searchControllerProvider).criteria;
    expect(criteria.originStationId, _origin.id);
    expect(criteria.destinationStationId, isNull);
    expect(criteria.travelDate!.toIsoDate(), '2026-10-18');
    expect(repository.searchCalls, 0);
  });

  test('each invalid criteria case blocks validation with exactly zero repository searches', () async {
    final cases = <String, List<String>>{
      'missing origin': ['destination', 'date'],
      'missing destination': ['origin', 'date'],
      'missing travel date': ['origin', 'destination'],
      'identical origin and destination': ['origin', 'same-destination'],
    };

    for (final entry in cases.entries) {
      final repository = _FakeRepository(stations: [_origin, _destination]);
      final container = _container(repository);
      await _settle(container);
      final controller = container.read(searchControllerProvider.notifier);
      for (final criterion in entry.value) {
        switch (criterion) {
          case 'origin':
            controller.selectOrigin(_origin.id);
            break;
          case 'destination':
            controller.selectDestination(_destination.id);
            break;
          case 'same-destination':
            controller.selectDestination(_origin.id);
            break;
          case 'date':
            controller.selectTravelDate(DateTime(2026, 10, 18));
            break;
        }
      }

      expect(controller.validateCriteria(), isFalse, reason: entry.key);
      expect(
        repository.searchCalls,
        0,
        reason: '${entry.key} must never call repository.search',
      );
      container.dispose();
    }
  });
}

ProviderContainer _container(_FakeRepository repository) {
  final container = ProviderContainer(
    overrides: [railwaySearchRepositoryProvider.overrideWithValue(repository)],
  );
  container.listen(searchControllerProvider, (previous, next) {});
  return container;
}

Future<void> _settle(ProviderContainer container) async {
  await Future<void>.delayed(Duration.zero);
  await container.pump();
  await Future<void>.delayed(Duration.zero);
}

class _FakeRepository implements RailwaySearchRepository {
  _FakeRepository({
    this.stations = const [],
    this.failFirstStationRead = false,
  });

  final List<Station> stations;
  final bool failFirstStationRead;
  int stationCalls = 0;
  int searchCalls = 0;

  @override
  Future<List<Station>> listStations() async {
    stationCalls++;
    if (failFirstStationRead && stationCalls == 1) {
      throw Exception('private backend diagnostic');
    }
    return stations;
  }

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async {
    searchCalls++;
    throw UnimplementedError('Phase 3 must not submit service searches.');
  }

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
