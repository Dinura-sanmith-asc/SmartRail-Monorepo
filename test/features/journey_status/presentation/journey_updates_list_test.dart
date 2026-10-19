import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_updates_list.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';

JourneyUpdate item(
  int revision,
  JourneyPublicChange change,
  JourneyUpdateKind kind,
) => JourneyUpdate(
  updateId: 'ffffffff-ffff-4fff-8fff-fffffffffff$revision',
  journeyInstanceId: journeyId,
  revision: revision,
  kind: kind,
  status: JourneyStatus.delayed,
  source: JourneyOperationalSource.controllerManual,
  publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
  correctionOfUpdateId: kind == JourneyUpdateKind.correction
      ? 'ffffffff-ffff-4fff-8fff-fffffffffff2'
      : null,
  publicChange: change,
);

void main() {
  testWidgets('empty history has a distinct successful empty message', (
    tester,
  ) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: JourneyUpdatesList(updates: [])),
      ),
    );
    expect(find.text('No published updates.'), findsOneWidget);
  });

  testWidgets(
    'renders returned order, supported public changes and correction reference',
    (tester) async {
      final updates = [
        item(
          4,
          const JourneyStatusChange(JourneyStatus.delayed),
          JourneyUpdateKind.status,
        ),
        item(
          5,
          JourneyStopChange(
            journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
            platform: '4',
          ),
          JourneyUpdateKind.stopChange,
        ),
        item(
          6,
          JourneyRouteDiversion([
            JourneyDivertedStop(
              journeyStopId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee2',
              stationId: 'cccccccc-cccc-4ccc-8ccc-ccccccccccc2',
              stopOrder: 3,
            ),
          ]),
          JourneyUpdateKind.routeDiversion,
        ),
        item(
          7,
          JourneyCorrectionChange(JourneyStatusChange(JourneyStatus.cancelled)),
          JourneyUpdateKind.correction,
        ),
      ];
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: JourneyUpdatesList(updates: updates)),
        ),
      );
      final text = tester
          .widgetList<Text>(find.byType(Text))
          .map((t) => t.data ?? '')
          .toList();
      expect(text.join('|'), contains('Revision 4'));
      expect(text.join('|'), contains('Revision 7'));
      expect(
        text.indexWhere((s) => s.contains('Status: Delayed')),
        lessThan(text.indexWhere((s) => s.contains('Stop update'))),
      );
      expect(
        find.textContaining('Corrects ffffffff-ffff-4fff-8fff-fffffffffff2'),
        findsOneWidget,
      );
      expect(find.textContaining('actor'), findsNothing);
      expect(find.textContaining('driver'), findsNothing);
    },
  );
}
