import 'dart:convert';

import '../domain/journey_models.dart';
import '../domain/journey_timestamp.dart';

Map<String, Object?> _map(Object? value, String where) {
  if (value is! Map<String, dynamic>) {
    throw FormatException('$where must be an object.');
  }
  return value;
}

List<Object?> _array(Object? value, String where) {
  if (value is! List) throw FormatException('$where must be an array.');
  return value.cast<Object?>();
}

String _string(Map<String, Object?> value, String key) {
  final field = value[key];
  if (field is! String) throw FormatException('$key must be a string.');
  return field;
}

String? _nullableString(Map<String, Object?> value, String key) {
  if (!value.containsKey(key)) return null;
  final field = value[key];
  if (field == null) return null;
  if (field is! String) throw FormatException('$key must be a string or null.');
  return field;
}

String? _optionalString(Map<String, Object?> value, String key) {
  if (!value.containsKey(key)) return null;
  final field = value[key];
  if (field is! String) {
    throw FormatException('$key must be a string when supplied.');
  }
  return field;
}

int _integer(Map<String, Object?> value, String key) {
  final field = value[key];
  if (field is! int) throw FormatException('$key must be an integer.');
  return field;
}

String? _date(
  Map<String, Object?> value,
  String key, {
  required bool nullable,
}) {
  if (!value.containsKey(key)) {
    if (nullable) return null;
    throw FormatException('Missing required field $key.');
  }
  final input = value[key];
  if (input == null && nullable) return null;
  if (input is! String) throw FormatException('$key must be a date string.');
  final match = RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(input);
  if (!match) throw FormatException('$key must be YYYY-MM-DD.');
  final parts = input.split('-').map(int.parse).toList();
  final date = DateTime.utc(parts[0], parts[1], parts[2]);
  if (date.year != parts[0] || date.month != parts[1] || date.day != parts[2]) {
    throw FormatException('$key is not a calendar date.');
  }
  return input;
}

String? _timestamp(
  Map<String, Object?> value,
  String key, {
  required bool nullable,
  bool optional = false,
}) {
  if (!value.containsKey(key)) {
    if (optional) return null;
    if (nullable) throw FormatException('Missing required field $key.');
    throw FormatException('Missing required field $key.');
  }
  final raw = value[key];
  if (raw == null && nullable) return null;
  if (raw is! String) throw FormatException('$key must be a date-time string.');
  JourneyTimestamp.parse(raw);
  return raw;
}

String _uuid(Map<String, Object?> value, String key) {
  final raw = _string(value, key);
  if (!RegExp(r'^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$')
      .hasMatch(raw)) {
    throw FormatException('$key must be a UUID.');
  }
  return raw;
}

int _positive(Map<String, Object?> value, String key) {
  final result = _integer(value, key);
  if (result < 1) throw FormatException('$key must be positive.');
  return result;
}

void _closed(Map<String, Object?> value, Set<String> known, String schema) {
  final unknown = value.keys.where((key) => !known.contains(key)).toList();
  if (unknown.isNotEmpty) {
    throw FormatException('$schema contains an unsupported field.');
  }
}

T _enumValue<T>(
  String value,
  Iterable<T> values,
  String Function(T item) apiValue,
  String field,
) {
  for (final item in values) {
    if (apiValue(item) == value) return item;
  }
  throw FormatException('Unknown $field value.');
}

Object? decodeJourneyJson(String source) {
  try {
    return jsonDecode(source);
  } on FormatException {
    throw const FormatException('Invalid JSON response.');
  }
}

class StationDeparturesDto {
  StationDeparturesDto(List<StationDepartureDto> items)
    : items = List.unmodifiable(items);
  final List<StationDepartureDto> items;

  factory StationDeparturesDto.fromJson(Object? json) => StationDeparturesDto(
    _array(json, 'Station departures')
        .map(
          (item) =>
              StationDepartureDto.fromJson(_map(item, 'Station departure')),
        )
        .toList(),
  );
}

class StationDepartureDto {
  const StationDepartureDto({
    required this.journeyInstanceId,
    required this.serviceId,
    required this.operationDate,
    required this.stationLocalDepartureDate,
    required this.revision,
    required this.status,
    required this.stationId,
    required this.stationName,
    required this.stopOrder,
    required this.scheduledDepartureAt,
    required this.operationalDepartureAt,
    required this.platform,
    required this.source,
    required this.updatedAt,
    required this.ambiguity,
  });

  final String journeyInstanceId;
  final String serviceId;
  final String operationDate;
  final String stationLocalDepartureDate;
  final int revision;
  final String status;
  final String stationId;
  final String? stationName;
  final int stopOrder;
  final String? scheduledDepartureAt;
  final String? operationalDepartureAt;
  final String? platform;
  final String? source;
  final String? updatedAt;
  final String ambiguity;

  factory StationDepartureDto.fromJson(Map<String, Object?> json) {
    _closed(json, {
      'journeyInstanceId',
      'serviceId',
      'operationDate',
      'stationLocalDepartureDate',
      'revision',
      'status',
      'stationId',
      'stationName',
      'stopOrder',
      'scheduledDepartureAt',
      'operationalDepartureAt',
      'platform',
      'source',
      'updatedAt',
      'ambiguity',
    }, 'StationDeparture');
    final status = JourneyStatus.parse(_string(json, 'status'));
    final ambiguity = JourneyAmbiguity.parse(_string(json, 'ambiguity'));
    final source = _optionalString(json, 'source');
    if (source != null) JourneyDataSource.parse(source);
    final updatedAt = _timestamp(
      json,
      'updatedAt',
      nullable: false,
      optional: true,
    );
    final scheduled = _timestamp(json, 'scheduledDepartureAt', nullable: true);
    final operational = json.containsKey('operationalDepartureAt')
        ? _timestamp(json, 'operationalDepartureAt', nullable: true)
        : null;
    if (json['platform'] != null && json['platform'] is! String) {
      throw const FormatException('platform must be a string or null.');
    }
    return StationDepartureDto(
      journeyInstanceId: _uuid(json, 'journeyInstanceId'),
      serviceId: _uuid(json, 'serviceId'),
      operationDate: _date(json, 'operationDate', nullable: false)!,
      stationLocalDepartureDate: _date(
        json,
        'stationLocalDepartureDate',
        nullable: false,
      )!,
      revision: _positive(json, 'revision'),
      status: status.apiValue,
      stationId: _uuid(json, 'stationId'),
      stationName: _optionalString(json, 'stationName'),
      stopOrder: _positive(json, 'stopOrder'),
      scheduledDepartureAt: scheduled,
      operationalDepartureAt: operational,
      platform: _nullableString(json, 'platform'),
      source: source,
      updatedAt: updatedAt,
      ambiguity: ambiguity.apiValue,
    );
  }
}

class JourneySnapshotDto {
  JourneySnapshotDto({
    required this.journeyInstanceId,
    required this.serviceId,
    required this.scheduleId,
    required this.scheduleRevision,
    required this.operationDate,
    required this.firstDepartureOccurrence,
    required this.revision,
    required this.status,
    required List<JourneyStopDto> stops,
    required this.source,
    required this.publishedAt,
    required this.lastUpdatedAt,
    required this.eta,
  }) : stops = List.unmodifiable(stops);

  final String journeyInstanceId;
  final String serviceId;
  final String scheduleId;
  final int scheduleRevision;
  final String operationDate;
  final String firstDepartureOccurrence;
  final int revision;
  final String status;
  final List<JourneyStopDto> stops;
  final String source;
  final String? publishedAt;
  final String? lastUpdatedAt;
  final JourneyEstimateDto? eta;

  factory JourneySnapshotDto.fromJson(Object? value) {
    final json = _map(value, 'JourneySnapshot');
    _closed(json, {
      'journeyInstanceId',
      'serviceId',
      'scheduleId',
      'scheduleRevision',
      'operationDate',
      'firstDepartureOccurrence',
      'revision',
      'status',
      'stops',
      'source',
      'publishedAt',
      'lastUpdatedAt',
      'eta',
    }, 'JourneySnapshot');
    final status = JourneyStatus.parse(_string(json, 'status'));
    final source = JourneyDataSource.parse(_string(json, 'source'));
    final publishedAt = _timestamp(json, 'publishedAt', nullable: true);
    final lastUpdatedAt = json.containsKey('lastUpdatedAt')
        ? _timestamp(json, 'lastUpdatedAt', nullable: true)
        : null;
    final etaRaw = json['eta'];
    final eta = etaRaw == null
        ? null
        : JourneyEstimateDto.fromJson(_map(etaRaw, 'Estimate'));
    final occurrence = _string(json, 'firstDepartureOccurrence');
    if (!RegExp(
      r'^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d+)?(?:Z|[+-](?:0\d|1\d|2[0-3]):[0-5]\d)?$',
    ).hasMatch(occurrence)) {
      throw const FormatException('firstDepartureOccurrence must be a time.');
    }
    return JourneySnapshotDto(
      journeyInstanceId: _uuid(json, 'journeyInstanceId'),
      serviceId: _uuid(json, 'serviceId'),
      scheduleId: _uuid(json, 'scheduleId'),
      scheduleRevision: _positive(json, 'scheduleRevision'),
      operationDate: _date(json, 'operationDate', nullable: false)!,
      firstDepartureOccurrence: occurrence,
      revision: _positive(json, 'revision'),
      status: status.apiValue,
      stops: _array(json['stops'], 'stops')
          .map((item) => JourneyStopDto.fromJson(_map(item, 'JourneyStop')))
          .toList(),
      source: source.apiValue,
      publishedAt: publishedAt,
      lastUpdatedAt: lastUpdatedAt,
      eta: eta,
    );
  }
}

class JourneyStopDto {
  const JourneyStopDto({
    required this.journeyStopId,
    required this.stopOrder,
    required this.stationId,
    required this.stationName,
    required this.plannedArrivalAt,
    required this.plannedDepartureAt,
    required this.operationalArrivalAt,
    required this.operationalDepartureAt,
    required this.platform,
    required this.timingSource,
    required this.timingUpdatedAt,
    required this.ambiguity,
  });
  final String journeyStopId;
  final int stopOrder;
  final String stationId;
  final String? stationName;
  final String? plannedArrivalAt;
  final String? plannedDepartureAt;
  final String? operationalArrivalAt;
  final String? operationalDepartureAt;
  final String? platform;
  final String? timingSource;
  final String? timingUpdatedAt;
  final String ambiguity;

  factory JourneyStopDto.fromJson(Map<String, Object?> json) {
    _closed(json, {
      'journeyStopId',
      'stopOrder',
      'stationId',
      'stationName',
      'plannedArrivalAt',
      'plannedDepartureAt',
      'operationalArrivalAt',
      'operationalDepartureAt',
      'platform',
      'timingSource',
      'timingUpdatedAt',
      'ambiguity',
    }, 'JourneyStop');
    final ambiguity = JourneyAmbiguity.parse(_string(json, 'ambiguity'));
    final timingSource = _nullableString(json, 'timingSource');
    return JourneyStopDto(
      journeyStopId: _uuid(json, 'journeyStopId'),
      stopOrder: _positive(json, 'stopOrder'),
      stationId: _uuid(json, 'stationId'),
      stationName: _optionalString(json, 'stationName'),
      plannedArrivalAt: json.containsKey('plannedArrivalAt')
          ? _timestamp(json, 'plannedArrivalAt', nullable: true)
          : null,
      plannedDepartureAt: json.containsKey('plannedDepartureAt')
          ? _timestamp(json, 'plannedDepartureAt', nullable: true)
          : null,
      operationalArrivalAt: json.containsKey('operationalArrivalAt')
          ? _timestamp(json, 'operationalArrivalAt', nullable: true)
          : null,
      operationalDepartureAt: json.containsKey('operationalDepartureAt')
          ? _timestamp(json, 'operationalDepartureAt', nullable: true)
          : null,
      platform: _nullableString(json, 'platform'),
      timingSource: timingSource,
      timingUpdatedAt: json.containsKey('timingUpdatedAt')
          ? _timestamp(json, 'timingUpdatedAt', nullable: true)
          : null,
      ambiguity: ambiguity.apiValue,
    );
  }
}

class JourneyEstimateDto {
  const JourneyEstimateDto({
    required this.value,
    required this.source,
    required this.updatedAt,
    required this.freshness,
  });
  final String value;
  final String source;
  final String updatedAt;
  final String freshness;

  factory JourneyEstimateDto.fromJson(Map<String, Object?> json) {
    _closed(json, {'value', 'source', 'updatedAt', 'freshness'}, 'Estimate');
    final source = JourneyDataSource.parse(_string(json, 'source'));
    final freshness = _enumValue(
      _string(json, 'freshness'),
      JourneyEstimateFreshness.values,
      (item) => item.apiValue,
      'ETA freshness',
    );
    return JourneyEstimateDto(
      value: _timestamp(json, 'value', nullable: false)!,
      source: source.apiValue,
      updatedAt: _timestamp(json, 'updatedAt', nullable: false)!,
      freshness: freshness.apiValue,
    );
  }
}

class JourneyUpdatesDto {
  JourneyUpdatesDto(List<JourneyUpdateDto> items)
    : items = List.unmodifiable(items);
  final List<JourneyUpdateDto> items;
  factory JourneyUpdatesDto.fromJson(Object? value) => JourneyUpdatesDto(
    _array(value, 'Journey updates')
        .map((item) => JourneyUpdateDto.fromJson(_map(item, 'JourneyUpdate')))
        .toList(),
  );
}

class JourneyUpdateDto {
  const JourneyUpdateDto({
    required this.updateId,
    required this.journeyInstanceId,
    required this.revision,
    required this.kind,
    required this.status,
    required this.source,
    required this.publishedAt,
    required this.correctionOfUpdateId,
    required this.publicChange,
  });
  final String updateId;
  final String journeyInstanceId;
  final int revision;
  final String kind;
  final String status;
  final String source;
  final String publishedAt;
  final String? correctionOfUpdateId;
  final PublicChangeDto publicChange;

  factory JourneyUpdateDto.fromJson(Map<String, Object?> json) {
    _closed(json, {
      'updateId',
      'journeyInstanceId',
      'revision',
      'kind',
      'status',
      'source',
      'publishedAt',
      'correctionOfUpdateId',
      'publicChange',
    }, 'JourneyUpdate');
    final kind = JourneyUpdateKind.parse(_string(json, 'kind'));
    final status = JourneyStatus.parse(_string(json, 'status'));
    final source = JourneyOperationalSource.parse(_string(json, 'source'));
    final publicChange = PublicChangeDto.fromJson(
      _map(json['publicChange'], 'publicChange'),
    );
    if (kind.apiValue != publicChange.changeType) {
      throw const FormatException('kind must match publicChange.changeType.');
    }
    final correctionId = _nullableString(json, 'correctionOfUpdateId');
    if (kind == JourneyUpdateKind.correction && correctionId == null) {
      throw const FormatException('Correction requires correctionOfUpdateId.');
    }
    if (correctionId != null) {
      final probe = <String, Object?>{'id': correctionId};
      _uuid(probe, 'id');
      if (kind != JourneyUpdateKind.correction) {
        throw const FormatException(
          'Only correction updates may reference a correction.',
        );
      }
    }
    return JourneyUpdateDto(
      updateId: _uuid(json, 'updateId'),
      journeyInstanceId: _uuid(json, 'journeyInstanceId'),
      revision: _positive(json, 'revision'),
      kind: kind.apiValue,
      status: status.apiValue,
      source: source.apiValue,
      publishedAt: _timestamp(json, 'publishedAt', nullable: false)!,
      correctionOfUpdateId: correctionId,
      publicChange: publicChange,
    );
  }
}

class PublicChangeDto {
  const PublicChangeDto({
    required this.changeType,
    this.status,
    this.journeyStopId,
    this.platform,
    this.operationalArrivalAt,
    this.operationalDepartureAt,
    this.divertedStops = const [],
    this.correctedChange,
  });
  final String changeType;
  final String? status;
  final String? journeyStopId;
  final String? platform;
  final String? operationalArrivalAt;
  final String? operationalDepartureAt;
  final List<PublicDivertedStopDto> divertedStops;
  final PublicChangeDto? correctedChange;

  factory PublicChangeDto.fromJson(Map<String, Object?> json) {
    final type = _string(json, 'changeType');
    switch (type) {
      case 'status':
        _closed(json, {'changeType', 'status'}, 'StatusPublicChange');
        return PublicChangeDto(
          changeType: type,
          status: JourneyStatus.parse(_string(json, 'status')).apiValue,
        );
      case 'stopChange':
        _closed(json, {
          'changeType',
          'journeyStopId',
          'platform',
          'operationalArrivalAt',
          'operationalDepartureAt',
        }, 'StopPublicChange');
        final platform = _nullableString(json, 'platform');
        final arrival = json.containsKey('operationalArrivalAt')
            ? _timestamp(json, 'operationalArrivalAt', nullable: true)
            : null;
        final departure = json.containsKey('operationalDepartureAt')
            ? _timestamp(json, 'operationalDepartureAt', nullable: true)
            : null;
        if (!json.containsKey('platform') &&
            !json.containsKey('operationalArrivalAt') &&
            !json.containsKey('operationalDepartureAt')) {
          throw const FormatException(
            'stopChange requires at least one changed field.',
          );
        }
        return PublicChangeDto(
          changeType: type,
          journeyStopId: _uuid(json, 'journeyStopId'),
          platform: platform,
          operationalArrivalAt: arrival,
          operationalDepartureAt: departure,
        );
      case 'routeDiversion':
        _closed(json, {
          'changeType',
          'divertedStops',
        }, 'RouteDiversionPublicChange');
        return PublicChangeDto(
          changeType: type,
          divertedStops: List.unmodifiable(
            _array(json['divertedStops'], 'divertedStops').map(
              (item) => PublicDivertedStopDto.fromJson(
                _map(item, 'PublicDivertedStop'),
              ),
            ),
          ),
        );
      case 'correction':
        _closed(json, {
          'changeType',
          'correctedChange',
        }, 'CorrectionPublicChange');
        final corrected = PublicChangeDto.fromJson(
          _map(json['correctedChange'], 'correctedChange'),
        );
        if (corrected.changeType == 'correction') {
          throw const FormatException(
            'A correction cannot correct another correction.',
          );
        }
        return PublicChangeDto(changeType: type, correctedChange: corrected);
      default:
        throw FormatException('Unknown public change type.');
    }
  }
}

class PublicDivertedStopDto {
  const PublicDivertedStopDto({
    required this.journeyStopId,
    required this.stationId,
    required this.stopOrder,
    required this.scheduledArrivalAt,
    required this.scheduledDepartureAt,
    required this.operationalArrivalAt,
    required this.operationalDepartureAt,
    required this.platform,
  });
  final String journeyStopId;
  final String stationId;
  final int stopOrder;
  final String? scheduledArrivalAt;
  final String? scheduledDepartureAt;
  final String? operationalArrivalAt;
  final String? operationalDepartureAt;
  final String? platform;

  factory PublicDivertedStopDto.fromJson(Map<String, Object?> json) {
    _closed(json, {
      'journeyStopId',
      'stationId',
      'stopOrder',
      'scheduledArrivalAt',
      'scheduledDepartureAt',
      'operationalArrivalAt',
      'operationalDepartureAt',
      'platform',
    }, 'PublicDivertedStop');
    return PublicDivertedStopDto(
      journeyStopId: _uuid(json, 'journeyStopId'),
      stationId: _uuid(json, 'stationId'),
      stopOrder: _positive(json, 'stopOrder'),
      scheduledArrivalAt: json.containsKey('scheduledArrivalAt')
          ? _timestamp(json, 'scheduledArrivalAt', nullable: true)
          : null,
      scheduledDepartureAt: json.containsKey('scheduledDepartureAt')
          ? _timestamp(json, 'scheduledDepartureAt', nullable: true)
          : null,
      operationalArrivalAt: json.containsKey('operationalArrivalAt')
          ? _timestamp(json, 'operationalArrivalAt', nullable: true)
          : null,
      operationalDepartureAt: json.containsKey('operationalDepartureAt')
          ? _timestamp(json, 'operationalDepartureAt', nullable: true)
          : null,
      platform: _nullableString(json, 'platform'),
    );
  }
}

class ProblemDetailsDto {
  ProblemDetailsDto({
    required this.type,
    required this.title,
    required this.status,
    required this.detail,
    required this.instance,
    required Map<String, List<String>> errors,
  }) : errors = Map.unmodifiable(
         errors.map(
           (key, value) => MapEntry(key, List<String>.unmodifiable(value)),
         ),
       );
  final String? type;
  final String? title;
  final int? status;
  final String? detail;
  final String? instance;
  final Map<String, List<String>> errors;

  factory ProblemDetailsDto.fromJson(Object? value) {
    final json = _map(value, 'Problem Details');
    String? optionalString(String key) {
      if (!json.containsKey(key) || json[key] == null) return null;
      if (json[key] is! String) throw FormatException('$key must be a string.');
      return json[key] as String;
    }

    int? status;
    if (json.containsKey('status') && json['status'] != null) {
      if (json['status'] is! int) {
        throw const FormatException('status must be an integer.');
      }
      status = json['status'] as int;
    }
    final errors = <String, List<String>>{};
    final rawErrors = json['errors'];
    if (rawErrors != null) {
      final values = _map(rawErrors, 'errors');
      for (final entry in values.entries) {
        final list = _array(entry.value, 'errors.${entry.key}');
        if (list.any((item) => item is! String)) {
          throw const FormatException(
            'Problem Details errors must contain strings.',
          );
        }
        errors[entry.key] = list.cast<String>();
      }
    }
    return ProblemDetailsDto(
      type: optionalString('type'),
      title: optionalString('title'),
      status: status,
      detail: optionalString('detail'),
      instance: optionalString('instance'),
      errors: errors,
    );
  }
}
