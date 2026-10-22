import 'package:smartrail_passenger/core/network/api_failure.dart';

class JourneyRecoveryMessage {
  const JourneyRecoveryMessage({
    required this.message,
    required this.canRetry,
    required this.canGoBack,
  });

  final String message;
  final bool canRetry;
  final bool canGoBack;
}

abstract final class JourneyRecoveryMapper {
  static JourneyRecoveryMessage map(ApiFailure failure) {
    return switch (failure.kind) {
      ApiFailureKind.invalidRequest => JourneyRecoveryMessage(
        message: switch (failure.operation) {
          RailwayApiOperation.stationDepartures => 'Dated journey information is unavailable for this selection. Go back and try again.',
          RailwayApiOperation.journeySnapshot ||
          RailwayApiOperation.journeyUpdates => 'This journey identifier is unavailable. Go back to select another journey.',
          RailwayApiOperation.stations ||
          RailwayApiOperation.routeStops ||
          RailwayApiOperation.serviceSearch ||
          RailwayApiOperation.serviceDetail =>
            'This railway information is unavailable. Go back and try again.',
        },
        canRetry: false,
        canGoBack: true,
      ),
      ApiFailureKind.accessDenied => const JourneyRecoveryMessage(
        message: 'Access to this railway information is unavailable.',
        canRetry: false,
        canGoBack: true,
      ),
      ApiFailureKind.notFound => JourneyRecoveryMessage(
        message: switch (failure.operation) {
          RailwayApiOperation.stationDepartures =>
            'This station information is no longer available.',
          RailwayApiOperation.journeySnapshot ||
          RailwayApiOperation.journeyUpdates =>
            'This journey information is no longer available.',
          RailwayApiOperation.stations ||
          RailwayApiOperation.routeStops ||
          RailwayApiOperation.serviceSearch ||
          RailwayApiOperation.serviceDetail =>
            'This railway information is no longer available.',
        },
        canRetry: false,
        canGoBack: true,
      ),
      ApiFailureKind.validation => const JourneyRecoveryMessage(
        message: 'Check the search details and try again.',
        canRetry: false,
        canGoBack: true,
      ),
      ApiFailureKind.unavailable ||
      ApiFailureKind.unexpected ||
      ApiFailureKind.timeout ||
      ApiFailureKind.network ||
      ApiFailureKind.protocol => const JourneyRecoveryMessage(
        message:
            'Railway information is temporarily unavailable. Please try again.',
        canRetry: true,
        canGoBack: true,
      ),
    };
  }
}
