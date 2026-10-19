import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_notifier.dart';
import 'package:smartrail_passenger/features/railway_search/application/service_detail_state.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

void main() {
  group('ServiceDetailController', () {
    test('loads service and route stops independently', () async {
      final repository = _Repository();
      final controller = _controller(repository);
      await Future<void>.delayed(Duration.zero);

      expect(repository.serviceCalls, 1);
      expect(repository.routeCalls, 1);
      expect(repository.serviceIds, ['service-id']);
      expect(repository.routeIds, ['route-id']);
      expect(controller.state.serviceDetail?.serviceCode, 'SR42');
      expect(controller.state.routeStops?.stops, hasLength(2));
      expect(controller.state.serviceStatus, DetailSectionStatus.ready);
      expect(controller.state.stopsStatus, DetailSectionStatus.ready);
    });

    test(
      'retains successful service when stops fail and retries stops only',
      () async {
        final repository = _Repository(failStops: true);
        final controller = _controller(repository);
        await Future<void>.delayed(Duration.zero);
        expect(controller.state.serviceDetail, isNotNull);
        expect(controller.state.stopsStatus, DetailSectionStatus.unavailable);

        repository.failStops = false;
        await controller.retryStops();
        expect(repository.serviceCalls, 1);
        expect(repository.routeCalls, 2);
        expect(controller.state.routeStops?.stops, hasLength(2));
      },
    );

    test('retains stops when service fails and retries service only', () async {
      final repository = _Repository(failService: true);
      final controller = _controller(repository);
      await Future<void>.delayed(Duration.zero);
      expect(controller.state.routeStops, isNotNull);
      expect(controller.state.serviceStatus, DetailSectionStatus.unavailable);

      repository.failService = false;
      await controller.retryService();
      expect(repository.serviceCalls, 2);
      expect(repository.routeCalls, 1);
      expect(controller.state.serviceDetail?.serviceCode, 'SR42');
    });

    test('does not duplicate a pending section read', () async {
      final pending = Completer<ServiceDetail>();
      final repository = _Repository(pendingService: pending);
      final controller = _controller(repository);
      await Future<void>.delayed(Duration.zero);
      final repeat = controller.retryService();
      expect(repository.serviceCalls, 1);
      pending.complete(_serviceDetail);
      await repeat;
    });

    test('ignores late completion after controller disposal', () async {
      final pending = Completer<ServiceDetail>();
      final repository = _Repository(pendingService: pending);
      final controller = _controller(repository);
      await Future<void>.delayed(Duration.zero);
      controller.dispose();
      pending.complete(_serviceDetail);
      await Future<void>.delayed(Duration.zero);
      expect(controller.state.serviceDetail, isNull);
    });

    test(
      'classifies malformed detail id as unavailable without retry',
      () async {
        final repository = _Repository(
          failService: true,
          serviceFailure: const ApiFailure(
            kind: ApiFailureKind.invalidRequest,
            operation: RailwayApiOperation.serviceDetail,
            statusCode: 400,
          ),
        );
        final controller = _controller(repository);
        await Future<void>.delayed(Duration.zero);
        expect(controller.state.serviceStatus, DetailSectionStatus.invalid);
        expect(controller.state.canRetryService, isFalse);
      },
    );
  });
}

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
ServiceDetailController _controller(_Repository repository) =>
    ServiceDetailController(
      repository,
      choice: _choice,
      travelDate: _travelDate,
    );

final _serviceDetail = ServiceDetail(
  serviceId: 'service-id',
  serviceCode: 'SR42',
  route: ServiceRoute(routeId: 'route-id', routeCode: 'R7', name: 'Main Line'),
  schedule: ServiceSchedule(revision: 3, firstDepartureTimeLocal: '22:55:00'),
);

class _Repository implements RailwaySearchRepository {
  _Repository({
    this.failService = false,
    this.failStops = false,
    this.serviceFailure,
    this.pendingService,
  });

  bool failService;
  bool failStops;
  final Object? serviceFailure;
  final Completer<ServiceDetail>? pendingService;
  int serviceCalls = 0;
  int routeCalls = 0;
  final serviceIds = <String>[];
  final routeIds = <String>[];

  @override
  Future<ServiceDetail> getService(String serviceId) {
    serviceCalls++;
    serviceIds.add(serviceId);
    if (pendingService != null) return pendingService!.future;
    if (failService) return Future.error(serviceFailure ?? Exception('failed'));
    return Future.value(_serviceDetail);
  }

  @override
  Future<RouteStops> getRouteStops(String routeId) async {
    routeCalls++;
    routeIds.add(routeId);
    if (failStops) throw Exception('failed');
    return RouteStops(
      routeId: routeId,
      routeCode: 'R7',
      name: 'Main Line',
      stops: [
        RouteStop(stopOrder: 2, stationId: 'b', stationName: 'Kandy'),
        RouteStop(stopOrder: 1, stationId: 'a', stationName: 'Colombo Fort'),
      ],
    );
  }

  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();
}
