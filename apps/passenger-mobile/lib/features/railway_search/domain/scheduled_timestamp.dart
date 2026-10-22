/// Literal offset-bearing timetable text. It is never converted to a device-zone instant.
class ScheduledTimestamp {
  const ScheduledTimestamp._({
    required this.rawValue,
    required this.localDate,
    required this.localTime,
    required this.offset,
  });

  final String rawValue;
  final String localDate;
  final String localTime;
  final String offset;

  factory ScheduledTimestamp.parse(String value) {
    final match = RegExp(
      r'^(\d{4})-(\d{2})-(\d{2})T((?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d+)?)(Z|[+-](?:0\d|1\d|2[0-3]):[0-5]\d)$',
    ).firstMatch(value);
    if (match == null) {
      throw const FormatException(
        'Expected an offset-bearing schedule date-time.',
      );
    }
    final year = int.parse(match[1]!);
    final month = int.parse(match[2]!);
    final day = int.parse(match[3]!);
    final date = DateTime.utc(year, month, day);
    if (date.year != year || date.month != month || date.day != day) {
      throw const FormatException(
        'Schedule timestamp contains an invalid calendar date.',
      );
    }
    return ScheduledTimestamp._(
      rawValue: value,
      localDate: '${match[1]}-${match[2]}-${match[3]}',
      localTime: match[4]!,
      offset: match[5]!,
    );
  }

  bool isLaterCalendarDayThan(String calendarDate) =>
      localDate.compareTo(calendarDate) > 0;
}
