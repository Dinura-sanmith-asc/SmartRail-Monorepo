import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_screen.dart';

void main() {
  testWidgets(
    'Back restores retained results and late details do not reopen route',
    (tester) async {
      final repository = _Repository();
      final router = GoRouter(
        initialLocation: '/results',
        routes: [
          GoRoute(
            path: '/results',
            builder: (context, state) => Scaffold(
              body: Column(
                children: [
                  const Text(
                    'Retained criteria: Colombo Fort → Badulla · 2026-10-19',
                  ),
                  const Text('Retained result: SR42'),
                  ElevatedButton(
                    onPressed: () => context.pushNamed(
                      'detail',
                      pathParameters: {'serviceId': 'service-id'},
                      extra: (choice: _choice, travelDate: _travelDate),
                    ),
                    child: const Text('Open service'),
                  ),
                ],
              ),
            ),
          ),
          GoRoute(
            path: '/services/:serviceId',
            name: 'detail',
            builder: (context, state) {
              final selection =
                  state.extra!
                      as ({
                        ScheduledServiceChoice choice,
                        TravelDate travelDate,
                      });
              return ServiceDetailScreen(
                choice: selection.choice,
                travelDate: selection.travelDate,
              );
            },
          ),
        ],
      );
      addTearDown(router.dispose);
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            railwaySearchRepositoryProvider.overrideWithValue(repository),
          ],
          child: MaterialApp.router(routerConfig: router),
        ),
      );
      await tester.tap(find.text('Open service'));
      await tester.pump();
      expect(find.byType(LinearProgressIndicator), findsNWidgets(2));
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(
        find.text('Retained criteria: Colombo Fort → Badulla · 2026-10-19'),
        findsOneWidget,
      );
      expect(find.text('Retained result: SR42'), findsOneWidget);

      repository.serviceRequest.complete(_serviceDetail);
      repository.stopsRequest.complete(_stops);
      await tester.pumpAndSettle();
      expect(find.text('Service SR42'), findsNothing);
      expect(router.routeInformationProvider.value.uri.path, '/results');
    },
  );
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

final _serviceDetail = ServiceDetail(
  serviceId: 'service-id',
  serviceCode: 'SR42',
  route: ServiceRoute(routeId: 'route-id', routeCode: 'R7', name: 'Main Line'),
  schedule: ServiceSchedule(revision: 3, firstDepartureTimeLocal: '22:55:00'),
);

final _stops = RouteStops(
  routeId: 'route-id',
  routeCode: 'R7',
  name: 'Main Line',
  stops: [],
);

class _Repository implements RailwaySearchRepository {
  final Completer<ServiceDetail> serviceRequest = Completer<ServiceDetail>();
  final Completer<RouteStops> stopsRequest = Completer<RouteStops>();

  @override
  Future<ServiceDetail> getService(String serviceId) => serviceRequest.future;

  @override
  Future<RouteStops> getRouteStops(String routeId) => stopsRequest.future;

  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();
}
