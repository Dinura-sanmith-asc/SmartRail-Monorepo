import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_update_alert.dart';

void main() {
  testWidgets(
    'notice exposes exact update identity/revision and is announced without color',
    (tester) async {
      final update = JourneyUpdate(
        updateId: 'ffffffff-ffff-4fff-8fff-fffffffffff2',
        journeyInstanceId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1',
        revision: 8,
        kind: JourneyUpdateKind.status,
        status: JourneyStatus.cancelled,
        source: JourneyOperationalSource.approvedRailwayFeed,
        publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
        publicChange: const JourneyStatusChange(JourneyStatus.cancelled),
      );
      final alert = JourneyUpdateAlerts.classify(update)!;
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: JourneyUpdateAlertBanner(alert: alert)),
        ),
      );
      expect(find.text('Journey cancelled'), findsOneWidget);
      expect(find.textContaining(update.updateId), findsOneWidget);
      expect(find.textContaining('Revision 8'), findsOneWidget);
      final semantics = tester.getSemantics(
        find.byType(JourneyUpdateAlertBanner),
      );
      expect(semantics.label, contains(update.updateId));
      expect(semantics.getSemanticsData().flagsCollection.isLiveRegion, isTrue);
    },
  );
}
