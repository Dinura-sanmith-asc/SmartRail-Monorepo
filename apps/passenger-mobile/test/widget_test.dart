import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';

void main() {
  testWidgets('all foundation routes build and use the normal back stack', (
    tester,
  ) async {
    final journeyRepository = _FakeJourneyRepository();
    appRouter.go(AppRoutes.search);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          railwaySearchRepositoryProvider.overrideWithValue(_FakeRepository()),
          journeyStatusRepositoryProvider.overrideWithValue(journeyRepository),
        ],
        child: SmartRailApp(
          config: AppConfig.fromValue('https://api.smartrail.example'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Search railway services'), findsOneWidget);
    expect(find.text('No published stations are available.'), findsOneWidget);

    final resultsPop = appRouter.push<void>(AppRoutes.results);
    await tester.pumpAndSettle();
    expect(find.text('Search not submitted'), findsOneWidget);

    final detailsPop = appRouter.push<void>(
      '/railway/services/phase-one-placeholder-id',
      extra: (
        choice: ScheduledServiceChoice(
          serviceId: 'phase-one-placeholder-id',
          serviceCode: 'SR42',
          routeId: 'route-id',
          routeCode: 'R1',
          scheduleRevision: 1,
          origin: const ScheduledCall(
            stationId: 'origin-id',
            stationName: 'Origin',
            stopOrder: 1,
            scheduledAt: '2026-10-19T08:00:00+05:30',
          ),
          destination: const ScheduledCall(
            stationId: 'destination-id',
            stationName: 'Destination',
            stopOrder: 2,
            scheduledAt: '2026-10-19T12:00:00+05:30',
          ),
        ),
        travelDate: TravelDate(year: 2026, month: 10, day: 19),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Scheduled service details'), findsOneWidget);
    expect(find.text('Service information'), findsOneWidget);

    await tester.tap(find.byKey(const Key('open-journey-status')));
    await tester.pumpAndSettle();
    expect(journeyRepository.requestedStationId, 'origin-id');
    expect(journeyRepository.requestedDate, '2026-10-19');
    expect(find.text('Journey status'), findsOneWidget);
    expect(
      find.text(
        'Dated journey information is not available for this service and date.',
      ),
      findsOneWidget,
    );
    appRouter.pop();
    await tester.pumpAndSettle();
    expect(find.text('Scheduled service details'), findsOneWidget);
    expect(find.text('Service information'), findsOneWidget);

    appRouter.pop();
    await tester.pumpAndSettle();
    await detailsPop;
    expect(find.text('Search not submitted'), findsOneWidget);

    appRouter.pop();
    await tester.pumpAndSettle();
    await resultsPop;
    expect(find.text('Search railway services'), findsOneWidget);
  });
}

class _FakeRepository implements RailwaySearchRepository {
  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}

class _FakeJourneyRepository implements JourneyStatusRepository {
  String? requestedStationId;
  String? requestedDate;

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async {
    requestedStationId = stationId;
    requestedDate = date;
    return const [];
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
