import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_recovery_mapper.dart';

void main() {
  test('400 and 404 are safe back-only feedback for every journey read', () {
    for (final operation in RailwayApiOperation.values.skip(4)) {
      final invalid = JourneyRecoveryMapper.map(
        ApiFailure.fromStatus(statusCode: 400, operation: operation),
      );
      expect(invalid.canRetry, isFalse);
      expect(invalid.canGoBack, isTrue);
      expect(invalid.message, isNotEmpty);

      final missing = JourneyRecoveryMapper.map(
        ApiFailure.fromStatus(statusCode: 404, operation: operation),
      );
      expect(missing.canRetry, isFalse);
      expect(missing.canGoBack, isTrue);
    }
  });

  test('access denial does not offer retry or sign-in', () {
    for (final code in [401, 403]) {
      final message = JourneyRecoveryMapper.map(
        ApiFailure.fromStatus(
          statusCode: code,
          operation: RailwayApiOperation.journeySnapshot,
        ),
      );
      expect(message.canRetry, isFalse);
      expect(message.canGoBack, isTrue);
      expect(message.message.toLowerCase(), isNot(contains('sign in')));
      expect(message.message.toLowerCase(), isNot(contains('login')));
    }
  });

  test('temporary failures allow retry only for the affected read', () {
    for (final failure in [
      ApiFailure.fromStatus(
        statusCode: 500,
        operation: RailwayApiOperation.journeyUpdates,
      ),
      ApiFailure.fromStatus(
        statusCode: 503,
        operation: RailwayApiOperation.journeyUpdates,
      ),
      ApiFailure.network(RailwayApiOperation.journeyUpdates),
      ApiFailure.timeout(RailwayApiOperation.journeyUpdates),
      ApiFailure.protocol(RailwayApiOperation.journeyUpdates),
    ]) {
      final message = JourneyRecoveryMapper.map(failure);
      expect(message.canRetry, isTrue);
      expect(message.canGoBack, isTrue);
    }
  });
}
