class Station {
  Station({
    required this.id,
    required this.name,
    List<String>? locationDetails,
    List<String>? serviceDetails,
  }) : locationDetails = locationDetails == null
           ? null
           : List.unmodifiable(locationDetails),
       serviceDetails = serviceDetails == null
           ? null
           : List.unmodifiable(serviceDetails) {
    if (name.isEmpty) {
      throw ArgumentError.value(name, 'name', 'Must not be empty.');
    }
  }

  final String id;
  final String name;
  final List<String>? locationDetails;
  final List<String>? serviceDetails;
}

class TravelDate {
  const TravelDate._(this.year, this.month, this.day);

  factory TravelDate({
    required int year,
    required int month,
    required int day,
  }) {
    final check = DateTime.utc(year, month, day);
    if (check.year != year || check.month != month || check.day != day) {
      throw ArgumentError('TravelDate must be a valid calendar date.');
    }
    return TravelDate._(year, month, day);
  }

  factory TravelDate.parse(String value) {
    final match = RegExp(r'^(\d{4})-(\d{2})-(\d{2})$').firstMatch(value);
    if (match == null) throw FormatException('Invalid travel date: $value');
    return TravelDate(
      year: int.parse(match[1]!),
      month: int.parse(match[2]!),
      day: int.parse(match[3]!),
    );
  }

  final int year;
  final int month;
  final int day;

  String toIsoDate() =>
      '${year.toString().padLeft(4, '0')}-${month.toString().padLeft(2, '0')}-${day.toString().padLeft(2, '0')}';

  @override
  String toString() => toIsoDate();

  @override
  bool operator ==(Object other) =>
      other is TravelDate &&
      year == other.year &&
      month == other.month &&
      day == other.day;

  @override
  int get hashCode => Object.hash(year, month, day);
}

class SearchCriteria {
  const SearchCriteria({
    this.originStationId,
    this.destinationStationId,
    this.travelDate,
  });

  final String? originStationId;
  final String? destinationStationId;
  final TravelDate? travelDate;

  bool get isSubmittable =>
      originStationId != null &&
      destinationStationId != null &&
      travelDate != null &&
      originStationId != destinationStationId;
}

class RouteStop {
  RouteStop({
    required this.stopOrder,
    required this.stationId,
    required this.stationName,
  }) {
    if (stopOrder < 1) {
      throw ArgumentError.value(stopOrder, 'stopOrder', 'Must be at least 1.');
    }
  }

  final int stopOrder;
  final String stationId;
  final String stationName;
}

class RouteStops {
  RouteStops({
    required this.routeId,
    required this.routeCode,
    required this.name,
    required List<RouteStop> stops,
  }) : stops = List.unmodifiable(stops);

  final String routeId;
  final String routeCode;
  final String name;
  final List<RouteStop> stops;
}

class ScheduledCall {
  const ScheduledCall({
    required this.stationId,
    required this.stationName,
    required this.stopOrder,
    required this.scheduledAt,
  }) : assert(stopOrder >= 1);

  final String stationId;
  final String stationName;
  final int stopOrder;
  final String scheduledAt;
}

class ScheduledServiceChoice {
  ScheduledServiceChoice({
    required this.serviceId,
    required this.serviceCode,
    required this.routeId,
    required this.routeCode,
    required this.scheduleRevision,
    required this.origin,
    required this.destination,
  }) {
    if (scheduleRevision < 1) {
      throw ArgumentError.value(
        scheduleRevision,
        'scheduleRevision',
        'Must be at least 1.',
      );
    }
  }

  final String serviceId;
  final String serviceCode;
  final String routeId;
  final String routeCode;
  final int scheduleRevision;
  final ScheduledCall origin;
  final ScheduledCall destination;
}

class ServiceSearchResponse {
  ServiceSearchResponse({
    required this.originStationId,
    required this.destinationStationId,
    required this.travelDate,
    required List<ScheduledServiceChoice> services,
  }) : services = List.unmodifiable(services);

  final String originStationId;
  final String destinationStationId;
  final TravelDate travelDate;
  final List<ScheduledServiceChoice> services;
}

class ServiceRoute {
  const ServiceRoute({
    required this.routeId,
    required this.routeCode,
    required this.name,
  });

  final String routeId;
  final String routeCode;
  final String name;
}

class ServiceSchedule {
  ServiceSchedule({
    required this.revision,
    required this.firstDepartureTimeLocal,
  }) {
    if (revision < 1) {
      throw ArgumentError.value(revision, 'revision', 'Must be at least 1.');
    }
  }

  final int revision;
  final String firstDepartureTimeLocal;
}

class ServiceDetail {
  const ServiceDetail({
    required this.serviceId,
    required this.serviceCode,
    required this.route,
    required this.schedule,
  });

  final String serviceId;
  final String serviceCode;
  final ServiceRoute route;
  final ServiceSchedule schedule;
}
