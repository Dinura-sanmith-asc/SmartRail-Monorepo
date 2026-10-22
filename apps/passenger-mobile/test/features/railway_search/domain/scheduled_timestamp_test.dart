import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/railway_search/domain/scheduled_timestamp.dart';

void main() {
  group('ScheduledTimestamp', () {
    test(
      'preserves supplied offset, local date and wall-clock time literally',
      () {
        final timestamp = ScheduledTimestamp.parse('2026-10-18T23:30:15+05:30');
        expect(timestamp.rawValue, '2026-10-18T23:30:15+05:30');
        expect(timestamp.localDate, '2026-10-18');
        expect(timestamp.localTime, '23:30:15');
        expect(timestamp.offset, '+05:30');
      },
    );

    test(
      'preserves UTC and fractional second representations without conversion',
      () {
        final timestamp = ScheduledTimestamp.parse('2026-10-19T02:15:00.250Z');
        expect(timestamp.rawValue, '2026-10-19T02:15:00.250Z');
        expect(timestamp.localDate, '2026-10-19');
        expect(timestamp.localTime, '02:15:00.250');
        expect(timestamp.offset, 'Z');
      },
    );

    test('identifies a later schedule date without computing elapsed time', () {
      final sameDay = ScheduledTimestamp.parse('2026-10-18T22:00:00+05:30');
      final nextDay = ScheduledTimestamp.parse('2026-10-19T01:00:00+05:30');
      expect(sameDay.isLaterCalendarDayThan('2026-10-18'), isFalse);
      expect(nextDay.isLaterCalendarDayThan('2026-10-18'), isTrue);
    });

    test(
      'rejects timestamps that lack a valid offset-bearing schedule value',
      () {
        expect(
          () => ScheduledTimestamp.parse('2026-10-18T23:30:00'),
          throwsFormatException,
        );
        expect(
          () => ScheduledTimestamp.parse('2026-02-30T23:30:00Z'),
          throwsFormatException,
        );
      },
    );
  });
}
