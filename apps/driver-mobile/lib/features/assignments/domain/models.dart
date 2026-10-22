class DriverIdentity {
  const DriverIdentity({required this.subject, required this.roles});
  final String subject;
  final List<String> roles;
  bool get isDriver => roles.contains('Driver');
  factory DriverIdentity.fromJson(Map<String, dynamic> j) => DriverIdentity(
    subject: j['subject'] as String,
    roles: (j['roles'] as List).cast<String>(),
  );
}

class Assignment {
  const Assignment({
    required this.id,
    required this.driverSubject,
    required this.journey,
    required this.updates,
  });
  final String id, driverSubject;
  final JourneySnapshot journey;
  final List<OfficialUpdate> updates;
  factory Assignment.fromJson(Map<String, dynamic> j) => Assignment(
    id: j['assignmentId'],
    driverSubject: j['driverSubject'],
    journey: JourneySnapshot.fromJson(
      (j['journey'] as Map)['snapshot'] as Map<String, dynamic>,
    ),
    updates:
        ((j['journey']['updates'] as List?) ?? const [])
            .map((e) => OfficialUpdate.fromJson(e as Map<String, dynamic>))
            .toList()
          ..sort((a, b) => b.revision.compareTo(a.revision)),
  );
}

class JourneySnapshot {
  const JourneySnapshot({
    required this.id,
    required this.serviceId,
    required this.scheduleId,
    required this.scheduleRevision,
    required this.operationDate,
    required this.firstDepartureOccurrence,
    required this.revision,
    required this.status,
    required this.source,
    required this.stops,
    required this.publishedAt,
    required this.lastUpdatedAt,
    required this.eta,
  });
  final String id,
      serviceId,
      scheduleId,
      operationDate,
      firstDepartureOccurrence,
      status,
      source;
  final int scheduleRevision, revision;
  final DateTime? publishedAt, lastUpdatedAt;
  final Estimate? eta;
  final List<JourneyStop> stops;
  factory JourneySnapshot.fromJson(Map<String, dynamic> j) => JourneySnapshot(
    id: j['journeyInstanceId'],
    serviceId: j['serviceId'],
    scheduleId: j['scheduleId'],
    scheduleRevision: j['scheduleRevision'],
    operationDate: j['operationDate'],
    firstDepartureOccurrence: j['firstDepartureOccurrence'],
    revision: j['revision'],
    status: j['status'],
    source: j['source'],
    publishedAt: _date(j['publishedAt']),
    lastUpdatedAt: _date(j['lastUpdatedAt']),
    eta: j['eta'] == null
        ? null
        : Estimate.fromJson(j['eta'] as Map<String, dynamic>),
    stops:
        ((j['stops'] as List?) ?? const [])
            .map((e) => JourneyStop.fromJson(e as Map<String, dynamic>))
            .toList()
          ..sort((a, b) => a.order.compareTo(b.order)),
  );
}

class JourneyStop {
  const JourneyStop({
    required this.id,
    required this.order,
    required this.stationId,
    required this.ambiguity,
    this.stationName,
    this.plannedArrivalAt,
    this.plannedDepartureAt,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.platform,
    this.timingSource,
    this.timingUpdatedAt,
  });
  final String id, stationId;
  final int order;
  final String ambiguity;
  final String? stationName, platform;
  final DateTime? plannedArrivalAt,
      plannedDepartureAt,
      operationalArrivalAt,
      operationalDepartureAt;
  final String? timingSource;
  final DateTime? timingUpdatedAt;
  factory JourneyStop.fromJson(Map<String, dynamic> j) => JourneyStop(
    id: j['journeyStopId'],
    order: j['stopOrder'],
    stationId: j['stationId'],
    ambiguity: j['ambiguity'] ?? 'none',
    stationName: j['stationName'],
    platform: j['platform'],
    plannedArrivalAt: _date(j['plannedArrivalAt']),
    plannedDepartureAt: _date(j['plannedDepartureAt']),
    operationalArrivalAt: _date(j['operationalArrivalAt']),
    operationalDepartureAt: _date(j['operationalDepartureAt']),
    timingSource: j['timingSource'],
    timingUpdatedAt: _date(j['timingUpdatedAt']),
  );
}

class Estimate {
  const Estimate({
    required this.value,
    required this.source,
    required this.updatedAt,
    required this.freshness,
  });
  final DateTime value, updatedAt;
  final String source, freshness;
  factory Estimate.fromJson(Map<String, dynamic> j) => Estimate(
    value: DateTime.parse(j['value']),
    source: j['source'],
    updatedAt: DateTime.parse(j['updatedAt']),
    freshness: j['freshness'],
  );
  Map<String, dynamic> toJson() => {
    'value': value.toIso8601String(),
    'source': source,
    'updatedAt': updatedAt.toIso8601String(),
    'freshness': freshness,
  };
}

class DivertedStop {
  const DivertedStop({
    required this.journeyStopId,
    required this.stationId,
    required this.stopOrder,
    this.scheduledArrivalAt,
    this.scheduledDepartureAt,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.platform,
  });
  final String? journeyStopId, platform;
  final String stationId;
  final int stopOrder;
  final DateTime? scheduledArrivalAt,
      scheduledDepartureAt,
      operationalArrivalAt,
      operationalDepartureAt;
  factory DivertedStop.fromJson(Map<String, dynamic> j) => DivertedStop(
    journeyStopId: j['journeyStopId'],
    stationId: j['stationId'],
    stopOrder: j['stopOrder'],
    platform: j['platform'],
    scheduledArrivalAt: _date(j['scheduledArrivalAt']),
    scheduledDepartureAt: _date(j['scheduledDepartureAt']),
    operationalArrivalAt: _date(j['operationalArrivalAt']),
    operationalDepartureAt: _date(j['operationalDepartureAt']),
  );
  Map<String, dynamic> toJson() => {
    'journeyStopId': journeyStopId,
    'stationId': stationId,
    'stopOrder': stopOrder,
    'platform': platform,
    'scheduledArrivalAt': scheduledArrivalAt?.toIso8601String(),
    'scheduledDepartureAt': scheduledDepartureAt?.toIso8601String(),
    'operationalArrivalAt': operationalArrivalAt?.toIso8601String(),
    'operationalDepartureAt': operationalDepartureAt?.toIso8601String(),
  };
}

class PublicChange {
  const PublicChange({
    required this.type,
    this.status,
    this.journeyStopId,
    this.platform,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.divertedStops = const [],
    this.correctedChange,
  });
  final String type;
  final String? status, journeyStopId, platform;
  final DateTime? operationalArrivalAt, operationalDepartureAt;
  final List<DivertedStop> divertedStops;
  final PublicChange? correctedChange;
  factory PublicChange.fromJson(Map<String, dynamic> j) => PublicChange(
    type: j['changeType'],
    status: j['status'],
    journeyStopId: j['journeyStopId'],
    platform: j['platform'],
    operationalArrivalAt: _date(j['operationalArrivalAt']),
    operationalDepartureAt: _date(j['operationalDepartureAt']),
    divertedStops: ((j['divertedStops'] as List?) ?? const [])
        .map((e) => DivertedStop.fromJson(e as Map<String, dynamic>))
        .toList(),
    correctedChange: j['correctedChange'] == null
        ? null
        : PublicChange.fromJson(j['correctedChange'] as Map<String, dynamic>),
  );
  Map<String, dynamic> toJson() => {
    'changeType': type,
    'status': status,
    'journeyStopId': journeyStopId,
    'platform': platform,
    'operationalArrivalAt': operationalArrivalAt?.toIso8601String(),
    'operationalDepartureAt': operationalDepartureAt?.toIso8601String(),
    'divertedStops': divertedStops.map((s) => s.toJson()).toList(),
    'correctedChange': correctedChange?.toJson(),
  };
}

class OfficialUpdate {
  const OfficialUpdate({
    required this.updateId,
    required this.journeyInstanceId,
    required this.revision,
    required this.kind,
    required this.status,
    required this.source,
    required this.publishedAt,
    required this.publicChange,
    this.correctionOfUpdateId,
  });
  final String updateId, journeyInstanceId;
  final int revision;
  final String kind, status, source;
  final DateTime publishedAt;
  final String? correctionOfUpdateId;
  final PublicChange publicChange;
  String get summary {
    final change = publicChange;
    switch (change.type) {
      case 'status':
        return 'Official status: ${change.status}';
      case 'stopChange':
        final details = <String>[];
        if (change.platform != null) {
          details.add('Platform ${change.platform}');
        }
        if (change.operationalArrivalAt != null) {
          details.add('Arrival updated');
        }
        if (change.operationalDepartureAt != null) {
          details.add('Departure updated');
        }
        return details.isEmpty
            ? 'Stop information updated'
            : details.join(' · ');
      case 'routeDiversion':
        final stops = change.divertedStops.length;
        return 'Route diversion · $stops calling points';
      case 'correction':
        return 'Official update corrected';
      default:
        return 'Official journey update';
    }
  }

  factory OfficialUpdate.fromJson(Map<String, dynamic> j) => OfficialUpdate(
    updateId: j['updateId'],
    journeyInstanceId: j['journeyInstanceId'],
    revision: j['revision'],
    kind: j['kind'],
    status: j['status'],
    source: j['source'],
    publishedAt: DateTime.parse(j['publishedAt']),
    correctionOfUpdateId: j['correctionOfUpdateId'],
    publicChange: PublicChange.fromJson(
      j['publicChange'] as Map<String, dynamic>,
    ),
  );
}

class DriverObservation {
  const DriverObservation({
    required this.id,
    required this.journeyInstanceId,
    required this.assignmentId,
    required this.driverSubject,
    required this.text,
    required this.reportedAtUtc,
    required this.journeyRevision,
  });
  final String id, journeyInstanceId, assignmentId, driverSubject, text;
  final DateTime reportedAtUtc;
  final int journeyRevision;
  factory DriverObservation.fromJson(Map<String, dynamic> j) =>
      DriverObservation(
        id: j['observationId'],
        journeyInstanceId: j['journeyInstanceId'],
        assignmentId: j['assignmentId'],
        driverSubject: j['driverSubject'],
        text: j['text'],
        reportedAtUtc: DateTime.parse(j['reportedAtUtc']),
        journeyRevision: j['journeyRevision'],
      );
}

DateTime? _date(dynamic value) =>
    value == null ? null : DateTime.parse(value as String);
