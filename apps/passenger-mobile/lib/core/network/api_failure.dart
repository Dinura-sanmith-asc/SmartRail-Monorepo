import 'package:smartrail_passenger/core/network/problem_details_parser.dart';

enum RailwayApiOperation {
  stations,
  routeStops,
  serviceSearch,
  serviceDetail,
  stationDepartures,
  journeySnapshot,
  journeyUpdates,
}

enum ApiFailureKind {
  validation,
  invalidRequest,
  accessDenied,
  notFound,
  unavailable,
  unexpected,
  timeout,
  network,
  protocol,
}

class ApiFailure implements Exception {
  const ApiFailure({
    required this.kind,
    required this.operation,
    this.statusCode,
    this.problem,
    this.cause,
  });

  final ApiFailureKind kind;
  final RailwayApiOperation operation;
  final int? statusCode;
  final ProblemDetails? problem;
  final Object? cause;

  String get passengerMessage => switch (kind) {
    ApiFailureKind.validation => 'Check the search details and try again.',
    ApiFailureKind.invalidRequest => 'This railway information is unavailable.',
    ApiFailureKind.accessDenied =>
      'Access to this railway information is unavailable.',
    ApiFailureKind.notFound =>
      'This railway information is no longer available.',
    ApiFailureKind.timeout ||
    ApiFailureKind.network ||
    ApiFailureKind.unavailable ||
    ApiFailureKind.unexpected ||
    ApiFailureKind.protocol =>
      'Railway information is temporarily unavailable. Please try again.',
  };

  factory ApiFailure.fromStatus({
    required int statusCode,
    required RailwayApiOperation operation,
    ProblemDetails? problem,
  }) {
    final isJourneyRead = switch (operation) {
      RailwayApiOperation.stationDepartures ||
      RailwayApiOperation.journeySnapshot ||
      RailwayApiOperation.journeyUpdates => true,
      RailwayApiOperation.stations ||
      RailwayApiOperation.routeStops ||
      RailwayApiOperation.serviceSearch ||
      RailwayApiOperation.serviceDetail => false,
    };
    final kind = switch (statusCode) {
      400 when operation == RailwayApiOperation.serviceSearch =>
        ApiFailureKind.validation,
      400 when isJourneyRead => ApiFailureKind.invalidRequest,
      400
          when operation == RailwayApiOperation.routeStops ||
              operation == RailwayApiOperation.serviceDetail =>
        ApiFailureKind.invalidRequest,
      400 => ApiFailureKind.unexpected,
      401 when isJourneyRead => ApiFailureKind.accessDenied,
      403 => ApiFailureKind.accessDenied,
      404 when isJourneyRead => ApiFailureKind.notFound,
      404
          when operation == RailwayApiOperation.routeStops ||
              operation == RailwayApiOperation.serviceDetail =>
        ApiFailureKind.notFound,
      503 => ApiFailureKind.unavailable,
      >= 500 => ApiFailureKind.unexpected,
      _ => ApiFailureKind.unexpected,
    };
    return ApiFailure(
      kind: kind,
      operation: operation,
      statusCode: statusCode,
      problem: problem,
    );
  }

  factory ApiFailure.protocol(RailwayApiOperation operation, [Object? cause]) =>
      ApiFailure(
        kind: ApiFailureKind.protocol,
        operation: operation,
        cause: cause,
      );

  factory ApiFailure.timeout(RailwayApiOperation operation, [Object? cause]) =>
      ApiFailure(
        kind: ApiFailureKind.timeout,
        operation: operation,
        cause: cause,
      );

  factory ApiFailure.network(RailwayApiOperation operation, [Object? cause]) =>
      ApiFailure(
        kind: ApiFailureKind.network,
        operation: operation,
        cause: cause,
      );

  @override
  String toString() =>
      'ApiFailure(${kind.name}, ${operation.name}, status: $statusCode)';
}
