import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

const _serviceId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1';
const _originId = 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1';
const _destinationId = 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1';
const _routeId = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1';
const _journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';

void main() {
  testWidgets(
    'service details opens a returned dated journey and Back retains context',
    (tester) async {
      final journeyRepository = _JourneyRepository();
      appRouter.go(AppRoutes.search);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            railwaySearchRepositoryProvider.overrideWithValue(
              _RailwayRepository(),
            ),
            journeyStatusRepositoryProvider.overrideWithValue(
              journeyRepository,
            ),
          ],
          child: SmartRailApp(
            config: AppConfig.fromValue('https://api.example.test'),
          ),
        ),
      );
      await tester.pumpAndSettle();

      final choice = ScheduledServiceChoice(
        serviceId: _serviceId,
        serviceCode: 'SR42',
        routeId: _routeId,
        routeCode: 'R7',
        scheduleRevision: 3,
        origin: const ScheduledCall(
          stationId: _originId,
          stationName: 'Colombo Fort',
          stopOrder: 1,
          scheduledAt: '2026-10-19T08:00:00+05:30',
        ),
        destination: const ScheduledCall(
          stationId: _destinationId,
          stationName: 'Galle',
          stopOrder: 2,
          scheduledAt: '2026-10-19T10:00:00+05:30',
        ),
      );
      final detailsFuture = appRouter.push<void>(
        '/railway/services/$_serviceId',
        extra: (choice: choice, travelDate: TravelDate.parse('2026-10-19')),
      );
      await tester.pumpAndSettle();
      expect(find.text('Scheduled service details'), findsOneWidget);

      await tester.tap(find.byKey(const Key('open-journey-status')));
      await tester.pumpAndSettle();
      expect(journeyRepository.requestedStationId, _originId);
      expect(journeyRepository.requestedDate, '2026-10-19');
      expect(find.text('Journey status'), findsOneWidget);
      expect(find.text('Colombo Fort'), findsOneWidget);

      await tester.tap(find.text('Open journey status'));
      await tester.pump(const Duration(milliseconds: 400));
      await tester.pump();
      expect(find.text('Current status'), findsOneWidget);
      expect(find.text('Scheduled'), findsOneWidget);
      expect(find.text('No published updates.'), findsOneWidget);
      expect(journeyRepository.requestedJourneyIds, [_journeyId, _journeyId]);

      appRouter.pop();
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.text('Current status'), findsNothing);
      expect(find.text('Journey status'), findsOneWidget);
      appRouter.pop();
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.text('Scheduled service details'), findsOneWidget);
      expect(find.text('Service information'), findsOneWidget);
      appRouter.pop();
      await tester.pump(const Duration(milliseconds: 400));
      await detailsFuture;
      expect(find.text('Search railway services'), findsOneWidget);
    },
  );
}

class _JourneyRepository implements JourneyStatusRepository {
  String? requestedStationId;
  String? requestedDate;
  final requestedJourneyIds = <String>[];

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async {
    requestedStationId = stationId;
    requestedDate = date;
    return [
      JourneyCandidate(
        journeyInstanceId: _journeyId,
        serviceId: _serviceId,
        operationDate: date,
        stationLocalDepartureDate: date,
        revision: 1,
        status: JourneyStatus.scheduled,
        stationId: stationId,
        stopOrder: 1,
        scheduledDepartureAt: JourneyTimestamp.parse(
          '$date'
          'T08:00:00+05:30',
        ),
        ambiguity: JourneyAmbiguity.none,
        stationName: 'Colombo Fort',
      ),
    ];
  }

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) async {
    requestedJourneyIds.add(journeyInstanceId);
    return JourneyStatusSnapshot(
      journeyInstanceId: _journeyId,
      serviceId: _serviceId,
      scheduleId: _routeId,
      scheduleRevision: 3,
      operationDate: '2026-10-19',
      firstDepartureOccurrence: '08:00:00',
      revision: 1,
      status: JourneyStatus.scheduled,
      stops: const [],
      source: JourneyDataSource.scheduledBaseline,
      publishedAt: null,
    );
  }

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    requestedJourneyIds.add(journeyInstanceId);
    return const [];
  }
}

class _RailwayRepository implements RailwaySearchRepository {
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
