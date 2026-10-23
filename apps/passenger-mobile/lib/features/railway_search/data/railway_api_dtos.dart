import 'dart:convert';

/// Parses a JSON object, rejecting non-object top-level responses.
Map<String, Object?> decodeJsonObject(String source) {
  final Object? decoded;
  try {
    decoded = jsonDecode(source);
  } on FormatException {
    rethrow;
  }
  if (decoded is! Map<String, dynamic>) {
    throw const FormatException('Expected a JSON object.');
  }
  return decoded.cast<String, Object?>();
}

Map<String, Object?> _object(
  Object? value,
  String at,
  Set<String> required, [
  Set<String> optional = const {},
]) {
  if (value is! Map<String, dynamic>) {
    throw FormatException('$at must be an object.');
  }
  final map = value.cast<String, Object?>();
  final missing = required.difference(map.keys.toSet());
  final unknown = map.keys.toSet().difference(required.union(optional));
  if (missing.isNotEmpty) {
    throw FormatException('$at is missing ${missing.join(', ')}.');
  }
  if (unknown.isNotEmpty) {
    throw FormatException('$at contains undocumented fields.');
  }
  return map;
}

String _string(Object? value, String at, {bool nonEmpty = false}) {
  if (value is! String || (nonEmpty && value.isEmpty)) {
    throw FormatException(
      '$at must be ${nonEmpty ? 'a non-empty ' : 'a '}string.',
    );
  }
  return value;
}

String? _nullableString(Map<String, Object?> json, String key, String at) {
  final value = json[key];
  return value == null ? null : _string(value, at);
}

int _integer(Object? value, String at, {int minimum = 0}) {
  if (value is! int || value < minimum) {
    throw FormatException('$at must be an integer >= $minimum.');
  }
  return value;
}

List<Object?> _array(Object? value, String at) {
  if (value is! List) {
    throw FormatException('$at must be an array.');
  }
  return value.cast<Object?>();
}

String _uuid(Object? value, String at) {
  final result = _string(value, at);
  if (!RegExp(r'^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$')
      .hasMatch(result)) {
    throw FormatException('$at must be a UUID.');
  }
  return result;
}

String _date(Object? value, String at) {
  final result = _string(value, at);
  final match = RegExp(r'^(\d{4})-(\d{2})-(\d{2})$').firstMatch(result);
  if (match == null) throw FormatException('$at must be a full ISO date.');
  final year = int.parse(match[1]!);
  final month = int.parse(match[2]!);
  final day = int.parse(match[3]!);
  final check = DateTime.utc(year, month, day);
  if (check.year != year || check.month != month || check.day != day) {
    throw FormatException('$at must be a valid calendar date.');
  }
  return result;
}

String _dateTime(Object? value, String at) {
  final result = _string(value, at);
  final match = RegExp(
    r'^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2}:\d{2}(?:\.\d+)?)(Z|[+-]\d{2}:\d{2})$',
  ).firstMatch(result);
  if (match == null) {
    throw FormatException('$at must be an offset-bearing date-time.');
  }
  _date(match[1], at);
  final time = match[2]!;
  final parts = time.split(':');
  final hour = int.parse(parts[0]);
  final minute = int.parse(parts[1]);
  final second = double.parse(parts[2]);
  if (hour > 23 || minute > 59 || second >= 60) {
    throw FormatException('$at has an invalid time.');
  }
  final offset = match[3]!;
  if (offset != 'Z') {
    final offsetHour = int.parse(offset.substring(1, 3));
    final offsetMinute = int.parse(offset.substring(4, 6));
    if (offsetHour > 23 || offsetMinute > 59) {
      throw FormatException('$at has an invalid offset.');
    }
  }
  return result;
}

String _localTime(Object? value, String at) {
  final result = _string(value, at);
  if (!RegExp(r'^([01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d+)?$')
      .hasMatch(result)) {
    throw FormatException('$at must be a local time.');
  }
  return result;
}

List<String>? _optionalStringArray(Map<String, Object?> json, String key) {
  if (!json.containsKey(key)) return null;
  return List.unmodifiable(
    _array(json[key], key).asMap().entries.map(
      (entry) => _string(entry.value, '$key[${entry.key}]'),
    ),
  );
}

class StationListDto {
  StationListDto(List<StationDto> items) : items = List.unmodifiable(items);
  final List<StationDto> items;
  factory StationListDto.fromJson(Object? json) {
    final map = _object(json, 'StationList', {'items'});
    return StationListDto(
      _array(map['items'], 'items')
          .asMap()
          .entries
          .map((e) => StationDto.fromJson(e.value, at: 'items[${e.key}]'))
          .toList(),
    );
  }
}

class StationDto {
  const StationDto({
    required this.stationId,
    required this.name,
    this.locationDetails,
    this.serviceDetails,
  });
  final String stationId;
  final String name;
  final List<String>? locationDetails;
  final List<String>? serviceDetails;
  factory StationDto.fromJson(Object? json, {String at = 'Station'}) {
    final map = _object(
      json,
      at,
      {'stationId', 'name'},
      {'locationDetails', 'serviceDetails'},
    );
    return StationDto(
      stationId: _uuid(map['stationId'], '$at.stationId'),
      name: _string(map['name'], '$at.name', nonEmpty: true),
      locationDetails: _optionalStringArray(map, 'locationDetails'),
      serviceDetails: _optionalStringArray(map, 'serviceDetails'),
    );
  }
}

class RouteStopsDto {
  RouteStopsDto({
    required this.routeId,
    required this.routeCode,
    required this.name,
    required List<RouteStopDto> stops,
  }) : stops = List.unmodifiable(stops);
  final String routeId;
  final String routeCode;
  final String name;
  final List<RouteStopDto> stops;
  factory RouteStopsDto.fromJson(Object? json) {
    final map = _object(json, 'RouteStops', {
      'routeId',
      'routeCode',
      'name',
      'stops',
    });
    return RouteStopsDto(
      routeId: _uuid(map['routeId'], 'routeId'),
      routeCode: _string(map['routeCode'], 'routeCode'),
      name: _string(map['name'], 'name'),
      stops: _array(map['stops'], 'stops')
          .asMap()
          .entries
          .map((e) => RouteStopDto.fromJson(e.value, at: 'stops[${e.key}]'))
          .toList(),
    );
  }
}

class RouteStopDto {
  const RouteStopDto({
    required this.stopOrder,
    required this.stationId,
    required this.stationName,
  });
  final int stopOrder;
  final String stationId;
  final String stationName;
  factory RouteStopDto.fromJson(Object? json, {String at = 'RouteStop'}) {
    final map = _object(json, at, {'stopOrder', 'stationId', 'stationName'});
    return RouteStopDto(
      stopOrder: _integer(map['stopOrder'], '$at.stopOrder', minimum: 1),
      stationId: _uuid(map['stationId'], '$at.stationId'),
      stationName: _string(map['stationName'], '$at.stationName'),
    );
  }
}

class ServiceSearchResponseDto {
  ServiceSearchResponseDto({
    required this.originStationId,
    required this.destinationStationId,
    required this.travelDate,
    required List<ServiceChoiceDto> services,
  }) : services = List.unmodifiable(services);
  final String originStationId;
  final String destinationStationId;
  final String travelDate;
  final List<ServiceChoiceDto> services;
  factory ServiceSearchResponseDto.fromJson(Object? json) {
    final map = _object(json, 'ServiceSearchResponse', {
      'originStationId',
      'destinationStationId',
      'travelDate',
      'services',
    });
    return ServiceSearchResponseDto(
      originStationId: _uuid(map['originStationId'], 'originStationId'),
      destinationStationId: _uuid(
        map['destinationStationId'],
        'destinationStationId',
      ),
      travelDate: _date(map['travelDate'], 'travelDate'),
      services: _array(map['services'], 'services')
          .asMap()
          .entries
          .map(
            (e) => ServiceChoiceDto.fromJson(e.value, at: 'services[${e.key}]'),
          )
          .toList(),
    );
  }
}

class ServiceChoiceDto {
  const ServiceChoiceDto({
    required this.serviceId,
    required this.serviceCode,
    this.displayName,
    this.publicServiceNumber,
    required this.routeId,
    required this.routeCode,
    required this.scheduleRevision,
    required this.origin,
    required this.destination,
  });
  final String serviceId, serviceCode, routeId, routeCode;
  final String? displayName, publicServiceNumber;
  final int scheduleRevision;
  final ScheduledCallDto origin, destination;
  factory ServiceChoiceDto.fromJson(
    Object? json, {
    String at = 'ServiceChoice',
  }) {
    final map = _object(
      json,
      at,
      {
        'serviceId',
        'serviceCode',
        'routeId',
        'routeCode',
        'scheduleRevision',
        'origin',
        'destination',
      },
      {'displayName', 'publicServiceNumber'},
    );
    return ServiceChoiceDto(
      serviceId: _uuid(map['serviceId'], '$at.serviceId'),
      serviceCode: _string(map['serviceCode'], '$at.serviceCode'),
      displayName: _nullableString(map, 'displayName', '$at.displayName'),
      publicServiceNumber: _nullableString(
        map,
        'publicServiceNumber',
        '$at.publicServiceNumber',
      ),
      routeId: _uuid(map['routeId'], '$at.routeId'),
      routeCode: _string(map['routeCode'], '$at.routeCode'),
      scheduleRevision: _integer(
        map['scheduleRevision'],
        '$at.scheduleRevision',
        minimum: 1,
      ),
      origin: ScheduledCallDto.fromJson(map['origin'], at: '$at.origin'),
      destination: ScheduledCallDto.fromJson(
        map['destination'],
        at: '$at.destination',
      ),
    );
  }
}

class ScheduledCallDto {
  const ScheduledCallDto({
    required this.stationId,
    required this.stationName,
    required this.stopOrder,
    required this.scheduledAt,
  });
  final String stationId, stationName, scheduledAt;
  final int stopOrder;
  factory ScheduledCallDto.fromJson(
    Object? json, {
    String at = 'ScheduledCall',
  }) {
    final map = _object(json, at, {
      'stationId',
      'stationName',
      'stopOrder',
      'scheduledAt',
    });
    return ScheduledCallDto(
      stationId: _uuid(map['stationId'], '$at.stationId'),
      stationName: _string(map['stationName'], '$at.stationName'),
      stopOrder: _integer(map['stopOrder'], '$at.stopOrder', minimum: 1),
      scheduledAt: _dateTime(map['scheduledAt'], '$at.scheduledAt'),
    );
  }
}

class ServiceDetailDto {
  const ServiceDetailDto({
    required this.serviceId,
    required this.serviceCode,
    this.displayName,
    this.publicServiceNumber,
    required this.route,
    required this.schedule,
  });
  final String serviceId, serviceCode;
  final String? displayName, publicServiceNumber;
  final ServiceRouteDto route;
  final ServiceScheduleDto schedule;
  factory ServiceDetailDto.fromJson(Object? json) {
    final map = _object(
      json,
      'ServiceDetail',
      {'serviceId', 'serviceCode', 'route', 'schedule'},
      {'displayName', 'publicServiceNumber'},
    );
    return ServiceDetailDto(
      serviceId: _uuid(map['serviceId'], 'serviceId'),
      serviceCode: _string(map['serviceCode'], 'serviceCode'),
      displayName: _nullableString(map, 'displayName', 'displayName'),
      publicServiceNumber: _nullableString(
        map,
        'publicServiceNumber',
        'publicServiceNumber',
      ),
      route: ServiceRouteDto.fromJson(map['route']),
      schedule: ServiceScheduleDto.fromJson(map['schedule']),
    );
  }
}

class ServiceRouteDto {
  const ServiceRouteDto({
    required this.routeId,
    required this.routeCode,
    required this.name,
  });
  final String routeId, routeCode, name;
  factory ServiceRouteDto.fromJson(Object? json) {
    final map = _object(json, 'route', {'routeId', 'routeCode', 'name'});
    return ServiceRouteDto(
      routeId: _uuid(map['routeId'], 'route.routeId'),
      routeCode: _string(map['routeCode'], 'route.routeCode'),
      name: _string(map['name'], 'route.name'),
    );
  }
}

class ServiceScheduleDto {
  const ServiceScheduleDto({
    required this.revision,
    required this.firstDepartureTimeLocal,
  });
  final int revision;
  final String firstDepartureTimeLocal;
  factory ServiceScheduleDto.fromJson(Object? json) {
    final map = _object(json, 'schedule', {
      'revision',
      'firstDepartureTimeLocal',
    });
    return ServiceScheduleDto(
      revision: _integer(map['revision'], 'schedule.revision', minimum: 1),
      firstDepartureTimeLocal: _localTime(
        map['firstDepartureTimeLocal'],
        'schedule.firstDepartureTimeLocal',
      ),
    );
  }
}
