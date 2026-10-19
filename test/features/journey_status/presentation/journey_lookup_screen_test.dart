import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_lookup_screen.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

void main() {
  const serviceId = '33333333-3333-4333-8333-333333333333';
  const stationId = '55555555-5555-4555-8555-555555555555';
  final request = JourneyLookupRequest(
    choice: ScheduledServiceChoice(
      serviceId: serviceId,
      serviceCode: 'SR1',
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
    travelDate: TravelDate.parse('2026-10-20'),
  );

  testWidgets('shows empty feedback and keeps Back available', (tester) async {
    final navigator = GlobalKey<NavigatorState>();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(
            FakeRepository(const []),
          ),
        ],
        child: MaterialApp(
          navigatorKey: navigator,
          home: JourneyLookupScreen(
            request: request,
            onJourneySelected: (_) {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(
      find.textContaining('Dated journey information is not available'),
      findsOneWidget,
    );
    expect(find.byTooltip('Back'), findsOneWidget);
  });

  testWidgets('shows loading and safe retry after network failure', (
    tester,
  ) async {
    final pending = Completer<List<JourneyCandidate>>();
    final repository = FakeRepository(const [], pending: pending);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          journeyStatusRepositoryProvider.overrideWithValue(repository),
        ],
        child: MaterialApp(
          home: JourneyLookupScreen(
            request: request,
            onJourneySelected: (_) {},
          ),
        ),
      ),
    );
    await tester.pump();
    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    pending.completeError(
      ApiFailure.network(RailwayApiOperation.stationDepartures),
    );
    await tester.pumpAndSettle();
    expect(find.textContaining('temporarily unavailable'), findsOneWidget);
    await tester.tap(find.byKey(const Key('retry-journey-lookup')));
    await tester.pumpAndSettle();
    expect(repository.calls, 2);
    expect(
      find.textContaining('Dated journey information is not available'),
      findsOneWidget,
    );
  });
}

class FakeRepository implements JourneyStatusRepository {
  FakeRepository(this.candidates, {this.pending});
  final List<JourneyCandidate> candidates;
  final Completer<List<JourneyCandidate>>? pending;
  int calls = 0;
  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async {
    calls++;
    if (calls == 1 && pending != null) return pending!.future;
    return candidates;
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
