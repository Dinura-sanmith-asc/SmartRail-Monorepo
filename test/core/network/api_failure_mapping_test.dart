import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';
import 'package:smartrail_passenger/features/railway_search/application/recovery_message_mapper.dart';

void main() {
  group('ApiFailure operation-specific classification', () {
    test('search 400 is correctable and section 400 is not retryable', () {
      final search = ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.serviceSearch,
      );
      final detail = ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.serviceDetail,
      );
      final route = ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.routeStops,
      );

      expect(search.kind, ApiFailureKind.validation);
      expect(detail.kind, ApiFailureKind.invalidRequest);
      expect(route.kind, ApiFailureKind.invalidRequest);
      expect(RecoveryMessageMapper.map(search).canCorrectCriteria, isTrue);
      for (final failure in [detail, route]) {
        final message = RecoveryMessageMapper.map(failure);
        expect(message.sectionUnavailable, isTrue);
        expect(message.canRetry, isFalse);
        expect(message.canGoBack, isTrue);
      }
    });

    test('search validation field feedback uses known fields and safe text', () {
      final failure = ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.serviceSearch,
        problem: ProblemDetailsParser.parse(
          '{"type":"about:blank","title":"Invalid","status":400,'
          '"errors":{"origin":["SQL connection string"],'
          '"unknownField":["internal trace"]}}',
        ),
      );
      final message = RecoveryMessageMapper.map(failure);

      expect(message.fieldMessages, {'origin': 'Check the origin station.'});
      expect(message.fieldMessages.values.join(' '), isNot(contains('SQL')));
      expect(message.fieldMessages.keys, isNot(contains('unknownField')));
    });

    test('403 is Back-only without sign-in or retry', () {
      final message = RecoveryMessageMapper.map(
        ApiFailure.fromStatus(
          statusCode: 403,
          operation: RailwayApiOperation.serviceDetail,
        ),
      );

      expect(message.canGoBack, isTrue);
      expect(message.canRetry, isFalse);
      expect(message.canCorrectCriteria, isFalse);
      expect(message.message.toLowerCase(), isNot(contains('sign in')));
      expect(message.message.toLowerCase(), contains('access'));
    });

    test('detail 404 affects its section and does not blindly retry', () {
      final message = RecoveryMessageMapper.map(
        ApiFailure.fromStatus(
          statusCode: 404,
          operation: RailwayApiOperation.routeStops,
        ),
      );

      expect(message.sectionUnavailable, isTrue);
      expect(message.canGoBack, isTrue);
      expect(message.canRetry, isFalse);
    });

    test('server, unavailable, timeout, and network failures can retry', () {
      final failures = [
        ApiFailure.fromStatus(
          statusCode: 500,
          operation: RailwayApiOperation.serviceSearch,
        ),
        ApiFailure.fromStatus(
          statusCode: 503,
          operation: RailwayApiOperation.routeStops,
        ),
        ApiFailure.timeout(RailwayApiOperation.serviceDetail),
        ApiFailure.network(RailwayApiOperation.serviceSearch),
      ];

      for (final failure in failures) {
        final message = RecoveryMessageMapper.map(failure);
        expect(message.canRetry, isTrue, reason: failure.toString());
        expect(message.canGoBack, isTrue);
        expect(message.message, isNot(contains('trace')));
      }
    });

    test('protocol failure uses safe unavailable wording with retry', () {
      final message = RecoveryMessageMapper.map(
        ApiFailure.protocol(
          RailwayApiOperation.serviceDetail,
          StateError('internal diagnostic'),
        ),
      );

      expect(message.canRetry, isTrue);
      expect(message.message, contains('temporarily unavailable'));
      expect(message.message, isNot(contains('internal diagnostic')));
    });

    test('malformed error body falls back to safe status behavior', () {
      final malformedProblem = ProblemDetailsParser.parse(
        '{"type":"about:blank","title":"secret details","status":"503"}',
      );
      final failure = ApiFailure.fromStatus(
        statusCode: 503,
        operation: RailwayApiOperation.serviceSearch,
        problem: malformedProblem,
      );
      final message = RecoveryMessageMapper.map(failure);

      expect(malformedProblem, isNull);
      expect(message.canRetry, isTrue);
      expect(message.message, isNot(contains('secret')));
    });

    test('unexpected default status is safely retryable', () {
      final message = RecoveryMessageMapper.map(
        ApiFailure.fromStatus(
          statusCode: 418,
          operation: RailwayApiOperation.serviceSearch,
        ),
      );

      expect(message.canRetry, isTrue);
      expect(message.message, contains('temporarily unavailable'));
    });
  });
}
