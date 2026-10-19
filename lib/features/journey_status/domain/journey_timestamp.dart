/// An offset-bearing API timestamp kept in its original railway-local form.
///
/// This value deliberately does not expose a device-local [DateTime].
class JourneyTimestamp {
  JourneyTimestamp._({
    required this.rawValue,
    required this.localDate,
    required this.localTime,
    required this.offset,
  });

  final String rawValue;
  final String localDate;
  final String localTime;
  final String offset;

  static final _pattern = RegExp(
    r'^(\d{4})-(\d{2})-(\d{2})T((?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d+)?)(Z|[+-](?:0\d|1\d|2[0-3]):[0-5]\d)$',
  );

  factory JourneyTimestamp.parse(String value) {
    final match = _pattern.firstMatch(value);
    if (match == null) {
      throw const FormatException(
        'Expected an offset-bearing journey date-time.',
      );
    }
    final year = int.parse(match[1]!);
    final month = int.parse(match[2]!);
    final day = int.parse(match[3]!);
    final calendarDate = DateTime.utc(year, month, day);
    if (calendarDate.year != year ||
        calendarDate.month != month ||
        calendarDate.day != day) {
      throw const FormatException('Journey timestamp has an invalid date.');
    }
    return JourneyTimestamp._(
      rawValue: value,
      localDate: '${match[1]}-${match[2]}-${match[3]}',
      localTime: match[4]!,
      offset: match[5]!,
    );
  }
}
