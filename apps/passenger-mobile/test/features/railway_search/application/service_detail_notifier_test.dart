import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

void main() {
  group('ServiceDetailNotifier', () {
    test('loads the selected service and route IDs independently', () async {
      final repository = _Repository();
      final container = _container(repository);
      addTearDown(container.dispose);
      final subscription = container.listen(
        serviceDetailNotifierProvider(_request),
        (previous, next) {},
      );
      addTearDown(subscription.close);
      await Future<void>.delayed(Duration.zero);

      final state = container.read(serviceDetailNotifierProvider(_request));
      expect(repository.serviceIds, ['service-id']);
      expect(repository.routeIds, ['route-id']);
      expect(state.serviceDetail?.serviceCode, 'SR42');
      expect(state.routeStops?.stops, hasLength(2));
      expect(state.serviceMetadata?.lastRetrievedAt, _retrievedAt);
      expect(state.stopsMetadata?.lastRetrievedAt, _retrievedAt);
      expect(state.serviceStatus, DetailSectionStatus.ready);
      expect(state.stopsStatus, DetailSectionStatus.ready);
    });

    test(
      'retains successful service after stops fail and retries stops only',
      () async {
        final repository = _Repository(failStops: true);
        final container = _container(repository);
        addTearDown(container.dispose);
        final subscription = _listen(container);
        addTearDown(subscription.close);
        await Future<void>.delayed(Duration.zero);
        expect(_read(container).serviceDetail, isNotNull);
        expect(_read(container).stopsStatus, DetailSectionStatus.unavailable);

        repository.failStops = false;
        await container
            .read(serviceDetailNotifierProvider(_request).notifier)
            .retryStops();
        expect(repository.serviceIds.length, 1);
        expect(repository.routeIds.length, 2);
        expect(_read(container).routeStops?.stops, hasLength(2));
      },
    );

    test(
      'retains ordered stops after service fails and retries service only',
      () async {
        final repository = _Repository(failService: true);
        final container = _container(repository);
        addTearDown(container.dispose);
        final subscription = _listen(container);
        addTearDown(subscription.close);
        await Future<void>.delayed(Duration.zero);
        expect(_read(container).routeStops, isNotNull);
        expect(_read(container).serviceStatus, DetailSectionStatus.unavailable);

        repository.failService = false;
        await container
            .read(serviceDetailNotifierProvider(_request).notifier)
            .retryService();
        expect(repository.serviceIds.length, 2);
        expect(repository.routeIds.length, 1);
        expect(_read(container).serviceDetail?.serviceCode, 'SR42');
      },
    );

    test('marks retained data not refreshed after a failed refresh', () async {
      final repository = _Repository();
      final container = _container(repository);
      addTearDown(container.dispose);
      final subscription = _listen(container);
      addTearDown(subscription.close);
      await Future<void>.delayed(Duration.zero);
      final notifier = container.read(
        serviceDetailNotifierProvider(_request).notifier,
      );
      repository.failService = true;
      await notifier.retryService();

      final state = _read(container);
      expect(state.serviceDetail?.serviceCode, 'SR42');
      expect(state.serviceMetadata?.lastRetrievedAt, _retrievedAt);
      expect(state.serviceMetadata?.isNotRefreshed, isTrue);
      expect(state.stopsMetadata?.isNotRefreshed, isFalse);
    });

    test('ignores duplicate calls while the initial read is pending', () async {
      final pending = Completer<ServiceDetail>();
      final repository = _Repository(pendingService: pending);
      final container = _container(repository);
      addTearDown(container.dispose);
      final subscription = _listen(container);
      addTearDown(subscription.close);
      await Future<void>.delayed(Duration.zero);
      await container
          .read(serviceDetailNotifierProvider(_request).notifier)
          .retryService();
      expect(repository.serviceIds, ['service-id']);
      pending.complete(_serviceDetail);
    });

    test('detail 400 and 403 failures are not retryable', () async {
      for (final failure in [
        const ApiFailure(
          kind: ApiFailureKind.invalidRequest,
          operation: RailwayApiOperation.serviceDetail,
          statusCode: 400,
        ),
        const ApiFailure(
          kind: ApiFailureKind.accessDenied,
          operation: RailwayApiOperation.serviceDetail,
          statusCode: 403,
        ),
      ]) {
        final repository = _Repository(
          failService: true,
          serviceFailure: failure,
        );
        final container = _container(repository);
        final subscription = _listen(container);
        await Future<void>.delayed(Duration.zero);
        expect(_read(container).canRetryService, isFalse);
        final calls = repository.serviceIds.length;
        await container
            .read(serviceDetailNotifierProvider(_request).notifier)
            .retryService();
        expect(repository.serviceIds.length, calls);
        subscription.close();
        container.dispose();
      }
    });

    test(
      'auto-disposes after route listener leaves and ignores late results',
      () async {
        final observer = _DetailObserver();
        final pendingService = Completer<ServiceDetail>();
        final pendingStops = Completer<RouteStops>();
        final repository = _Repository(
          pendingService: pendingService,
          pendingStops: pendingStops,
        );
        final provider = serviceDetailNotifierProvider(_request);
        final container = ProviderContainer(
          observers: [observer],
          overrides: [
            railwaySearchRepositoryProvider.overrideWithValue(repository),
          ],
        );
        final subscription = container.listen(provider, (previous, next) {});
        await Future<void>.delayed(Duration.zero);
        subscription.close();
        await Future<void>.delayed(Duration.zero);
        expect(observer.disposedProviders, contains(provider));

        pendingService.complete(_serviceDetail);
        pendingStops.complete(_stops);
        await Future<void>.delayed(Duration.zero);
        container.dispose();
      },
    );
  });
}

final DateTime _retrievedAt = DateTime.utc(2026, 10, 19, 8, 0);
final _request = (choice: _choice, travelDate: _travelDate);
final _choice = ScheduledServiceChoice(
  serviceId: 'service-id',
  serviceCode: 'SR42',
  routeId: 'route-id',
  routeCode: 'R7',
  scheduleRevision: 3,
  origin: const ScheduledCall(
    stationId: 'origin-id',
    stationName: 'Colombo Fort',
    stopOrder: 1,
    scheduledAt: '2026-10-19T23:30:00+05:30',
  ),
  destination: const ScheduledCall(
    stationId: 'destination-id',
    stationName: 'Badulla',
    stopOrder: 8,
    scheduledAt: '2026-10-20T08:15:00+05:30',
  ),
);
final _travelDate = TravelDate(year: 2026, month: 10, day: 19);
final _serviceDetail = ServiceDetail(
  serviceId: 'service-id',
  serviceCode: 'SR42',
  route: const ServiceRoute(
    routeId: 'route-id',
    routeCode: 'R7',
    name: 'Main Line',
  ),
  schedule: ServiceSchedule(revision: 3, firstDepartureTimeLocal: '22:55:00'),
);
final _stops = RouteStops(
  routeId: 'route-id',
  routeCode: 'R7',
  name: 'Main Line',
  stops: [
    RouteStop(stopOrder: 2, stationId: 'b', stationName: 'Kandy'),
    RouteStop(stopOrder: 1, stationId: 'a', stationName: 'Colombo Fort'),
  ],
);

ProviderContainer _container(_Repository repository) => ProviderContainer(
  overrides: [
    railwaySearchRepositoryProvider.overrideWithValue(repository),
    serviceDetailClockProvider.overrideWithValue(() => _retrievedAt),
  ],
);

ProviderSubscription<ServiceDetailState> _listen(ProviderContainer container) =>
    container.listen(
      serviceDetailNotifierProvider(_request),
      (previous, next) {},
    );

ServiceDetailState _read(ProviderContainer container) =>
    container.read(serviceDetailNotifierProvider(_request));

class _Repository implements RailwaySearchRepository {
  _Repository({
    this.failService = false,
    this.failStops = false,
    this.serviceFailure,
    this.pendingService,
    this.pendingStops,
  });

  bool failService;
  bool failStops;
  final Object? serviceFailure;
  final Completer<ServiceDetail>? pendingService;
  final Completer<RouteStops>? pendingStops;
  final serviceIds = <String>[];
  final routeIds = <String>[];

  @override
  Future<ServiceDetail> getService(String serviceId) {
    serviceIds.add(serviceId);
    if (pendingService != null) return pendingService!.future;
    if (failService) return Future.error(serviceFailure ?? Exception('failed'));
    return Future.value(_serviceDetail);
  }

  @override
  Future<RouteStops> getRouteStops(String routeId) {
    routeIds.add(routeId);
    if (pendingStops != null) return pendingStops!.future;
    if (failStops) return Future.error(Exception('failed'));
    return Future.value(_stops);
  }

  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();
}

final class _DetailObserver extends ProviderObserver {
  final disposedProviders = <Object>[];

  @override
  void didDisposeProvider(ProviderObserverContext context) {
    disposedProviders.add(context.provider);
  }
}
