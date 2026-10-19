import 'journey_timestamp.dart';

enum JourneyStatus {
  scheduled('Scheduled'),
  running('Running'),
  delayed('Delayed'),
  interrupted('Interrupted'),
  cancelled('Cancelled'),
  completed('Completed'),
  terminated('Terminated');

  const JourneyStatus(this.apiValue);
  final String apiValue;

  static JourneyStatus parse(String value) => JourneyStatus.values.firstWhere(
    (status) => status.apiValue == value,
    orElse: () => throw FormatException('Unknown journey status: $value'),
  );
}

enum JourneyAmbiguity {
  none('none'),
  ambiguous('ambiguous');

  const JourneyAmbiguity(this.apiValue);
  final String apiValue;

  static JourneyAmbiguity parse(String value) =>
      JourneyAmbiguity.values.firstWhere(
        (ambiguity) => ambiguity.apiValue == value,
        orElse: () => throw FormatException('Unknown ambiguity: $value'),
      );
}

enum JourneyDataSource {
  scheduledBaseline('ScheduledBaseline'),
  controllerManual('ControllerManual'),
  approvedRailwayFeed('ApprovedRailwayFeed');

  const JourneyDataSource(this.apiValue);
  final String apiValue;

  static JourneyDataSource parse(String value) =>
      JourneyDataSource.values.firstWhere(
        (source) => source.apiValue == value,
        orElse: () => throw FormatException('Unknown journey source: $value'),
      );
}

enum JourneyOperationalSource {
  controllerManual('ControllerManual'),
  approvedRailwayFeed('ApprovedRailwayFeed');

  const JourneyOperationalSource(this.apiValue);
  final String apiValue;

  static JourneyOperationalSource parse(String value) =>
      JourneyOperationalSource.values.firstWhere(
        (source) => source.apiValue == value,
        orElse: () => throw FormatException('Unknown update source: $value'),
      );
}

enum JourneyEstimateFreshness {
  current('current'),
  stale('stale'),
  lastKnown('lastKnown');

  const JourneyEstimateFreshness(this.apiValue);
  final String apiValue;

  static JourneyEstimateFreshness parse(String value) =>
      JourneyEstimateFreshness.values.firstWhere(
        (freshness) => freshness.apiValue == value,
        orElse: () => throw FormatException('Unknown ETA freshness: $value'),
      );
}

enum JourneyUpdateKind {
  status('status'),
  stopChange('stopChange'),
  routeDiversion('routeDiversion'),
  correction('correction');

  const JourneyUpdateKind(this.apiValue);
  final String apiValue;

  static JourneyUpdateKind parse(String value) =>
      JourneyUpdateKind.values.firstWhere(
        (kind) => kind.apiValue == value,
        orElse: () => throw FormatException('Unknown update kind: $value'),
      );
}

sealed class JourneyPublicChange {
  const JourneyPublicChange();
  String get changeType;
}

class JourneyStatusChange extends JourneyPublicChange {
  const JourneyStatusChange(this.status);
  final JourneyStatus status;
  @override
  String get changeType => 'status';
}

class JourneyStopChange extends JourneyPublicChange {
  JourneyStopChange({
    required String journeyStopId,
    this.platform,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
  }) : journeyStopId = _uuid(journeyStopId, 'journeyStopId') {
    if (platform == null &&
        operationalArrivalAt == null &&
        operationalDepartureAt == null) {
      throw ArgumentError('A stop change must contain a changed field.');
    }
  }
  final String journeyStopId;
  final String? platform;
  final JourneyTimestamp? operationalArrivalAt;
  final JourneyTimestamp? operationalDepartureAt;
  @override
  String get changeType => 'stopChange';
}

class JourneyDivertedStop {
  JourneyDivertedStop({
    required String journeyStopId,
    required String stationId,
    required int stopOrder,
    this.scheduledArrivalAt,
    this.scheduledDepartureAt,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.platform,
  }) : journeyStopId = _uuid(journeyStopId, 'journeyStopId'),
       stationId = _uuid(stationId, 'stationId'),
       stopOrder = _positive(stopOrder, 'stopOrder');
  final String journeyStopId;
  final String stationId;
  final int stopOrder;
  final JourneyTimestamp? scheduledArrivalAt;
  final JourneyTimestamp? scheduledDepartureAt;
  final JourneyTimestamp? operationalArrivalAt;
  final JourneyTimestamp? operationalDepartureAt;
  final String? platform;
}

class JourneyRouteDiversion extends JourneyPublicChange {
  JourneyRouteDiversion(List<JourneyDivertedStop> divertedStops)
    : divertedStops = List.unmodifiable(divertedStops);
  final List<JourneyDivertedStop> divertedStops;
  @override
  String get changeType => 'routeDiversion';
}

class JourneyCorrectionChange extends JourneyPublicChange {
  const JourneyCorrectionChange(this.correctedChange);
  final JourneyPublicChange correctedChange;
  @override
  String get changeType => 'correction';
}

String _uuid(String value, String field) {
  if (!RegExp(
    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89ABab][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$',
  ).hasMatch(value)) {
    throw FormatException('$field must be a UUID.');
  }
  return value;
}

String _date(String value, String field) {
  final match = RegExp(r'^(\d{4})-(\d{2})-(\d{2})$').firstMatch(value);
  if (match == null) throw FormatException('$field must be YYYY-MM-DD.');
  final year = int.parse(match[1]!);
  final month = int.parse(match[2]!);
  final day = int.parse(match[3]!);
  final parsed = DateTime.utc(year, month, day);
  if (parsed.year != year || parsed.month != month || parsed.day != day) {
    throw FormatException('$field is not a valid calendar date.');
  }
  return value;
}

int _positive(int value, String field) {
  if (value < 1) throw ArgumentError.value(value, field, 'Must be positive.');
  return value;
}

String _time(String value, String field) {
  if (!RegExp(
    r'^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d+)?(?:Z|[+-](?:0\d|1\d|2[0-3]):[0-5]\d)?$',
  ).hasMatch(value)) {
    throw FormatException('$field must be an RFC 3339 time.');
  }
  return value;
}

class JourneyCandidate {
  JourneyCandidate({
    required String journeyInstanceId,
    required String serviceId,
    required String operationDate,
    required String stationLocalDepartureDate,
    required int revision,
    required this.status,
    required String stationId,
    required int stopOrder,
    required this.scheduledDepartureAt,
    required this.ambiguity,
    this.stationName,
    this.operationalDepartureAt,
    this.platform,
    this.source,
    this.updatedAt,
  }) : journeyInstanceId = _uuid(journeyInstanceId, 'journeyInstanceId'),
       serviceId = _uuid(serviceId, 'serviceId'),
       operationDate = _date(operationDate, 'operationDate'),
       stationLocalDepartureDate = _date(
         stationLocalDepartureDate,
         'stationLocalDepartureDate',
       ),
       revision = _positive(revision, 'revision'),
       stationId = _uuid(stationId, 'stationId'),
       stopOrder = _positive(stopOrder, 'stopOrder');

  final String journeyInstanceId;
  final String serviceId;
  final String operationDate;
  final String stationLocalDepartureDate;
  final int revision;
  final JourneyStatus status;
  final String stationId;
  final int stopOrder;
  final JourneyTimestamp? scheduledDepartureAt;
  final JourneyAmbiguity ambiguity;
  final String? stationName;
  final JourneyTimestamp? operationalDepartureAt;
  final String? platform;
  final JourneyDataSource? source;
  final JourneyTimestamp? updatedAt;
}

class JourneyStopStatus {
  JourneyStopStatus({
    required String journeyStopId,
    required int stopOrder,
    required String stationId,
    required this.ambiguity,
    this.stationName,
    this.plannedArrivalAt,
    this.plannedDepartureAt,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.platform,
    this.timingSource,
    this.timingUpdatedAt,
  }) : journeyStopId = _uuid(journeyStopId, 'journeyStopId'),
       stopOrder = _positive(stopOrder, 'stopOrder'),
       stationId = _uuid(stationId, 'stationId');

  final String journeyStopId;
  final int stopOrder;
  final String stationId;
  final JourneyAmbiguity ambiguity;
  final String? stationName;
  final JourneyTimestamp? plannedArrivalAt;
  final JourneyTimestamp? plannedDepartureAt;
  final JourneyTimestamp? operationalArrivalAt;
  final JourneyTimestamp? operationalDepartureAt;
  final String? platform;
  final String? timingSource;
  final JourneyTimestamp? timingUpdatedAt;
}

class JourneyEstimate {
  const JourneyEstimate({
    required this.value,
    required this.source,
    required this.updatedAt,
    required this.freshness,
  });

  final JourneyTimestamp value;
  final JourneyDataSource source;
  final JourneyTimestamp updatedAt;
  final JourneyEstimateFreshness freshness;
}

class JourneyStatusSnapshot {
  JourneyStatusSnapshot({
    required String journeyInstanceId,
    required String serviceId,
    required String scheduleId,
    required int scheduleRevision,
    required String operationDate,
    required String firstDepartureOccurrence,
    required int revision,
    required this.status,
    required List<JourneyStopStatus> stops,
    required this.source,
    required this.publishedAt,
    this.lastUpdatedAt,
    this.eta,
  }) : journeyInstanceId = _uuid(journeyInstanceId, 'journeyInstanceId'),
       serviceId = _uuid(serviceId, 'serviceId'),
       scheduleId = _uuid(scheduleId, 'scheduleId'),
       scheduleRevision = _positive(scheduleRevision, 'scheduleRevision'),
       operationDate = _date(operationDate, 'operationDate'),
       firstDepartureOccurrence = _time(
         firstDepartureOccurrence,
         'firstDepartureOccurrence',
       ),
       revision = _positive(revision, 'revision'),
       stops = List.unmodifiable(stops);

  final String journeyInstanceId;
  final String serviceId;
  final String scheduleId;
  final int scheduleRevision;
  final String operationDate;
  final String firstDepartureOccurrence;
  final int revision;
  final JourneyStatus status;
  final List<JourneyStopStatus> stops;
  final JourneyDataSource source;
  final JourneyTimestamp? publishedAt;
  final JourneyTimestamp? lastUpdatedAt;
  final JourneyEstimate? eta;
}

class JourneyUpdate {
  JourneyUpdate({
    required String updateId,
    required String journeyInstanceId,
    required int revision,
    required this.kind,
    required this.status,
    required this.source,
    required this.publishedAt,
    required this.publicChange,
    String? correctionOfUpdateId,
  }) : updateId = _uuid(updateId, 'updateId'),
       journeyInstanceId = _uuid(journeyInstanceId, 'journeyInstanceId'),
       revision = _positive(revision, 'revision'),
       correctionOfUpdateId = correctionOfUpdateId == null
           ? null
           : _uuid(correctionOfUpdateId, 'correctionOfUpdateId') {
    if (kind == JourneyUpdateKind.correction &&
        this.correctionOfUpdateId == null) {
      throw ArgumentError('Correction updates require a referenced update ID.');
    }
    if (kind.apiValue != publicChange.changeType) {
      throw ArgumentError('Update kind must match its public change type.');
    }
  }

  final String updateId;
  final String journeyInstanceId;
  final int revision;
  final JourneyUpdateKind kind;
  final JourneyStatus status;
  final JourneyOperationalSource source;
  final JourneyTimestamp publishedAt;
  final String? correctionOfUpdateId;
  final JourneyPublicChange publicChange;
}
