import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/application/seen_journey_update_tracker.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';

const journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';
const otherJourneyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2';

JourneyUpdate makeUpdate(String id, String journey) => JourneyUpdate(
  updateId: id,
  journeyInstanceId: journey,
  revision: 2,
  kind: JourneyUpdateKind.status,
  status: JourneyStatus.delayed,
  source: JourneyOperationalSource.controllerManual,
  publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
  publicChange: const JourneyStatusChange(JourneyStatus.delayed),
);

void main() {
  test('first successful history read establishes baseline without alert', () {
    final tracker = SeenJourneyUpdateTracker();
    final first = makeUpdate('ffffffff-ffff-4fff-8fff-fffffffffff1', journeyId);
    expect(tracker.observe(journeyId, [first]), isEmpty);
    expect(tracker.observe(journeyId, [first]), isEmpty);
  });

  test(
    'returns only newly observed IDs and keeps journey identities separate',
    () {
      final tracker = SeenJourneyUpdateTracker();
      final baseline = makeUpdate(
        'ffffffff-ffff-4fff-8fff-fffffffffff1',
        journeyId,
      );
      tracker.observe(journeyId, [baseline]);
      final fresh = makeUpdate(
        'ffffffff-ffff-4fff-8fff-fffffffffff2',
        journeyId,
      );
      expect(tracker.observe(journeyId, [baseline, fresh]), [fresh]);
      expect(tracker.observe(journeyId, [baseline, fresh]), isEmpty);
      final other = makeUpdate(
        'ffffffff-ffff-4fff-8fff-fffffffffff3',
        otherJourneyId,
      );
      expect(tracker.observe(otherJourneyId, [other]), isEmpty);
      final otherFresh = makeUpdate(
        'ffffffff-ffff-4fff-8fff-fffffffffff4',
        otherJourneyId,
      );
      expect(tracker.observe(otherJourneyId, [other, otherFresh]), [
        otherFresh,
      ]);
    },
  );

  test('session reset clears the in-memory baseline', () {
    final tracker = SeenJourneyUpdateTracker();
    final update = makeUpdate(
      'ffffffff-ffff-4fff-8fff-fffffffffff1',
      journeyId,
    );
    tracker.observe(journeyId, [update]);
    tracker.reset();
    expect(tracker.observe(journeyId, [update]), isEmpty);
  });
}
