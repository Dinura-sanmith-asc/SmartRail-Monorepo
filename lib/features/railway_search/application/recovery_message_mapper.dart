import 'package:smartrail_passenger/core/network/api_failure.dart';

/// Passenger-safe recovery wording and available actions for one failed read.
///
/// This maps existing client failure classifications to UX behavior. It does
/// not infer new backend response semantics or expose Problem Details text.
class RecoveryMessage {
  const RecoveryMessage({
    required this.message,
    required this.canRetry,
    required this.canGoBack,
    required this.canCorrectCriteria,
    required this.sectionUnavailable,
    this.fieldMessages = const {},
  });

  final String message;
  final bool canRetry;
  final bool canGoBack;
  final bool canCorrectCriteria;
  final bool sectionUnavailable;
  final Map<String, String> fieldMessages;
}

abstract final class RecoveryMessageMapper {
  static RecoveryMessage map(ApiFailure failure) {
    final isDetailRead = switch (failure.operation) {
      RailwayApiOperation.serviceDetail || RailwayApiOperation.routeStops =>
        true,
      RailwayApiOperation.stations || RailwayApiOperation.serviceSearch =>
        false,
    };

    return switch (failure.kind) {
      ApiFailureKind.validation => RecoveryMessage(
        message: 'Check the search details and try again.',
        canRetry: false,
        canGoBack: true,
        canCorrectCriteria: true,
        sectionUnavailable: false,
        fieldMessages: _knownSearchFieldMessages(failure),
      ),
      ApiFailureKind.invalidRequest => const RecoveryMessage(
        message: 'This railway information is unavailable. Go back and choose another service.',
        canRetry: false,
        canGoBack: true,
        canCorrectCriteria: false,
        sectionUnavailable: true,
      ),
      ApiFailureKind.accessDenied => RecoveryMessage(
        message: 'Access to this railway information is unavailable.',
        canRetry: false,
        canGoBack: true,
        canCorrectCriteria: false,
        sectionUnavailable: isDetailRead,
      ),
      ApiFailureKind.notFound => const RecoveryMessage(
        message: 'This railway information could not be found.',
        canRetry: false,
        canGoBack: true,
        canCorrectCriteria: false,
        sectionUnavailable: true,
      ),
      ApiFailureKind.unavailable ||
      ApiFailureKind.unexpected ||
      ApiFailureKind.timeout ||
      ApiFailureKind.network ||
      ApiFailureKind.protocol => RecoveryMessage(
        message: 'Railway information is temporarily unavailable. Please try again.',
        canRetry: true,
        canGoBack: true,
        canCorrectCriteria: false,
        sectionUnavailable: isDetailRead,
      ),
    };
  }

  static Map<String, String> _knownSearchFieldMessages(ApiFailure failure) {
    final errors = failure.problem?.errors ?? const <String, List<String>>{};
    final messages = <String, String>{};
    if (errors.containsKey('origin')) {
      messages['origin'] = 'Check the origin station.';
    }
    if (errors.containsKey('destination')) {
      messages['destination'] = 'Check the destination station.';
    }
    if (errors.containsKey('date')) {
      messages['date'] = 'Check the travel date.';
    }
    return Map.unmodifiable(messages);
  }
}
