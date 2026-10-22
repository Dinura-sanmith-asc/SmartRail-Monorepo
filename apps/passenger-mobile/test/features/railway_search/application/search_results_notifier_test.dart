import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/search_results_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

void main() {
  final criteria = SearchCriteria(
    originStationId: 'origin',
    destinationStationId: 'destination',
    travelDate: TravelDate(year: 2026, month: 10, day: 19),
  );

  test(
    'submits criteria once and transitions through loading to populated',
    () async {
      final repository = _FakeRepository();
      final container = _container(repository);
      addTearDown(container.dispose);
      final notifier = container.read(searchResultsNotifierProvider.notifier);

      final pending = notifier.submit(criteria);
      expect(
        container.read(searchResultsNotifierProvider).status,
        SearchResultsStatus.loading,
      );
      final duplicate = notifier.submit(criteria);
      repository.pending.single.complete(_response([_choice()]));
      await Future.wait([pending, duplicate]);

      final state = container.read(searchResultsNotifierProvider);
      expect(repository.searchCalls, 1);
      expect(repository.lastCriteria, same(criteria));
      expect(state.status, SearchResultsStatus.populated);
      expect(state.response!.services.single.serviceCode, 'SR-14');
    },
  );

  test('keeps a successful empty response distinct from failures', () async {
    final repository = _FakeRepository()..nextResponse = _response([]);
    final container = _container(repository);
    addTearDown(container.dispose);

    await container
        .read(searchResultsNotifierProvider.notifier)
        .submit(criteria);

    expect(
      container.read(searchResultsNotifierProvider).status,
      SearchResultsStatus.empty,
    );
    expect(container.read(searchResultsNotifierProvider).failure, isNull);
  });

  test(
    'classifies search 400 for correction and permits resubmission',
    () async {
      final repository = _FakeRepository()
        ..nextError = const ApiFailure(
          kind: ApiFailureKind.validation,
          operation: RailwayApiOperation.serviceSearch,
          statusCode: 400,
        );
      final container = _container(repository);
      addTearDown(container.dispose);
      final notifier = container.read(searchResultsNotifierProvider.notifier);

      await notifier.submit(criteria);
      expect(
        container.read(searchResultsNotifierProvider).status,
        SearchResultsStatus.validation,
      );
      expect(container.read(searchResultsNotifierProvider).canRetry, isFalse);
      expect(repository.searchCalls, 1);
    },
  );

  test(
    '403 has no retry and 503 retry repeats the retained criteria',
    () async {
      final repository = _FakeRepository()
        ..nextError = const ApiFailure(
          kind: ApiFailureKind.accessDenied,
          operation: RailwayApiOperation.serviceSearch,
          statusCode: 403,
        );
      final container = _container(repository);
      addTearDown(container.dispose);
      final notifier = container.read(searchResultsNotifierProvider.notifier);

      await notifier.submit(criteria);
      expect(
        container.read(searchResultsNotifierProvider).status,
        SearchResultsStatus.accessDenied,
      );
      expect(container.read(searchResultsNotifierProvider).canRetry, isFalse);

      repository.nextError = const ApiFailure(
        kind: ApiFailureKind.unavailable,
        operation: RailwayApiOperation.serviceSearch,
        statusCode: 503,
      );
      await notifier.submit(criteria);
      expect(container.read(searchResultsNotifierProvider).canRetry, isTrue);
      repository.nextError = const ApiFailure(
        kind: ApiFailureKind.unavailable,
        operation: RailwayApiOperation.serviceSearch,
        statusCode: 503,
      );
      await notifier.retry();
      expect(repository.searchCalls, 3);
      expect(repository.lastCriteria, same(criteria));
      expect(
        container.read(searchResultsNotifierProvider).status,
        SearchResultsStatus.unavailable,
      );
    },
  );

  test(
    'clear invalidates pending response and removes previous results',
    () async {
      final repository = _FakeRepository()
        ..nextResponse = _response([_choice()]);
      final container = _container(repository);
      addTearDown(container.dispose);
      final notifier = container.read(searchResultsNotifierProvider.notifier);
      await notifier.submit(criteria);
      expect(container.read(searchResultsNotifierProvider).response, isNotNull);

      final pending = notifier.submit(criteria);
      notifier.clear();
      repository.pending.last.complete(
        _response([_choice(serviceCode: 'LATE')]),
      );
      await pending;

      expect(
        container.read(searchResultsNotifierProvider).status,
        SearchResultsStatus.notSearched,
      );
      expect(container.read(searchResultsNotifierProvider).response, isNull);
    },
  );

  test('out-of-order responses cannot replace the latest search', () async {
    final repository = _FakeRepository();
    final container = _container(repository);
    addTearDown(container.dispose);
    final notifier = container.read(searchResultsNotifierProvider.notifier);
    final first = notifier.submit(criteria);
    final firstCompleter = repository.pending.last;
    final newerCriteria = SearchCriteria(
      originStationId: 'new-origin',
      destinationStationId: 'destination',
      travelDate: criteria.travelDate,
    );
    notifier.clear();
    final second = notifier.submit(newerCriteria);
    final secondCompleter = repository.pending.last;
    secondCompleter.complete(_response([_choice(serviceCode: 'NEW')]));
    await second;
    firstCompleter.complete(_response([_choice(serviceCode: 'OLD')]));
    await first;

    expect(
      container
          .read(searchResultsNotifierProvider)
          .response!
          .services
          .single
          .serviceCode,
      'NEW',
    );
    expect(
      container.read(searchResultsNotifierProvider).criteria,
      same(newerCriteria),
    );
  });
}

ProviderContainer _container(_FakeRepository repository) {
  final container = ProviderContainer(
    overrides: [railwaySearchRepositoryProvider.overrideWithValue(repository)],
  );
  container.listen(searchResultsNotifierProvider, (previous, next) {});
  return container;
}

ServiceSearchResponse _response(List<ScheduledServiceChoice> services) =>
    ServiceSearchResponse(
      originStationId: 'origin',
      destinationStationId: 'destination',
      travelDate: TravelDate(year: 2026, month: 10, day: 19),
      services: services,
    );

ScheduledServiceChoice _choice({String serviceCode = 'SR-14'}) =>
    ScheduledServiceChoice(
      serviceId: 'service-1',
      serviceCode: serviceCode,
      routeId: 'route-1',
      routeCode: 'R1',
      scheduleRevision: 2,
      origin: const ScheduledCall(
        stationId: 'origin',
        stationName: 'Central',
        stopOrder: 1,
        scheduledAt: '2026-10-19T23:55:00+05:30',
      ),
      destination: const ScheduledCall(
        stationId: 'destination',
        stationName: 'Harbour',
        stopOrder: 4,
        scheduledAt: '2026-10-20T00:30:00+05:30',
      ),
    );

class _FakeRepository implements RailwaySearchRepository {
  final List<Completer<ServiceSearchResponse>> pending = [];
  ServiceSearchResponse? nextResponse;
  Object? nextError;
  SearchCriteria? lastCriteria;
  int searchCalls = 0;

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) {
    searchCalls++;
    lastCriteria = criteria;
    if (nextError case final error?) {
      nextError = null;
      return Future.error(error);
    }
    if (nextResponse case final response?) {
      nextResponse = null;
      return Future.value(response);
    }
    final completer = Completer<ServiceSearchResponse>();
    pending.add(completer);
    return completer.future;
  }

  @override
  Future<List<Station>> listStations() async => const [];
  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();
  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
