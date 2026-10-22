import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_candidates_screen.dart';
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

  testWidgets(
    'shows distinct ambiguous options with accessible ordinals; only tapped option opens',
    (tester) async {
      final candidates = [
        candidate(
          '11111111-1111-4111-8111-111111111111',
          JourneyAmbiguity.ambiguous,
        ),
        candidate(
          '22222222-2222-4222-8222-222222222222',
          JourneyAmbiguity.ambiguous,
        ),
      ];
      JourneyCandidate? selected;
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            journeyLookupNotifierProvider(request).overrideWith(
              () => TestJourneyLookupNotifier(request, candidates),
            ),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: SizedBox(
                height: 500,
                child: JourneyCandidatesScreen(
                  request: request,
                  onJourneySelected: (item) => selected = item,
                ),
              ),
            ),
          ),
        ),
      );
      expect(
        find.text('Ambiguous journey identity'),
        findsNothing,
      ); // cue is included in each row's visible description.
      expect(
        find.textContaining('Ambiguous journey identity'),
        findsNWidgets(2),
      );
      expect(find.text('Open journey status'), findsNothing);
      expect(find.byKey(const ValueKey('journey-option-1')), findsOneWidget);
      expect(find.byKey(const ValueKey('journey-option-2')), findsOneWidget);
      final semantics = tester.ensureSemantics();
      expect(
        tester
            .getSemantics(find.byKey(const ValueKey('journey-option-1')))
            .label,
        contains('Journey option 1 of 2'),
      );
      await tester.tap(find.byKey(const ValueKey('journey-option-2')));
      expect(selected?.journeyInstanceId, candidates[1].journeyInstanceId);
      semantics.dispose();
    },
  );

  testWidgets('unique unambiguous candidate has an explicit open action', (
    tester,
  ) async {
    final item = candidate(
      '11111111-1111-4111-8111-111111111111',
      JourneyAmbiguity.none,
    );
    JourneyCandidate? selected;
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          journeyLookupNotifierProvider(request)
              .overrideWith(() => TestJourneyLookupNotifier(request, [item])),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: SizedBox(
              height: 500,
              child: JourneyCandidatesScreen(
                request: request,
                onJourneySelected: (value) => selected = value,
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.byKey(const Key('open-journey-candidate')));
    expect(selected?.journeyInstanceId, item.journeyInstanceId);
  });
}

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

class TestJourneyLookupNotifier extends JourneyLookupNotifier {
  TestJourneyLookupNotifier(super.request, this.values);

  final List<JourneyCandidate> values;

  @override
  JourneyLookupState build() =>
      JourneyLookupState(status: JourneyLookupStatus.ready, candidates: values);
}
