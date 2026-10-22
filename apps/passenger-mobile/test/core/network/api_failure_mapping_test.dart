import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';
import 'package:smartrail_passenger/features/railway_search/application/recovery_message_mapper.dart';

void main() {
  group('ApiFailure operation-specific classification', () {
    test('journey reads classify documented statuses by operation', () {
      const operations = [
        RailwayApiOperation.stationDepartures,
        RailwayApiOperation.journeySnapshot,
        RailwayApiOperation.journeyUpdates,
      ];

      for (final operation in operations) {
        expect(
          ApiFailure.fromStatus(statusCode: 400, operation: operation).kind,
          ApiFailureKind.invalidRequest,
          reason: '${operation.name} 400',
        );
        expect(
          ApiFailure.fromStatus(statusCode: 404, operation: operation).kind,
          ApiFailureKind.notFound,
          reason: '${operation.name} 404',
        );
        for (final status in [401, 403]) {
          expect(
            ApiFailure.fromStatus(
              statusCode: status,
              operation: operation,
            ).kind,
            ApiFailureKind.accessDenied,
            reason: '${operation.name} $status',
          );
        }
        expect(
          ApiFailure.fromStatus(statusCode: 503, operation: operation).kind,
          ApiFailureKind.unavailable,
          reason: '${operation.name} 503',
        );
        expect(
          ApiFailure.fromStatus(statusCode: 500, operation: operation).kind,
          ApiFailureKind.unexpected,
          reason: '${operation.name} 500',
        );
      }
    });

    test(
      'journey recovery is safe and retryable only for temporary failures',
      () {
        const operations = [
          RailwayApiOperation.stationDepartures,
          RailwayApiOperation.journeySnapshot,
          RailwayApiOperation.journeyUpdates,
        ];

        for (final operation in operations) {
          final invalid = RecoveryMessageMapper.map(
            ApiFailure.fromStatus(statusCode: 400, operation: operation),
          );
          expect(invalid.canGoBack, isTrue, reason: '${operation.name} 400');
          expect(invalid.canRetry, isFalse, reason: '${operation.name} 400');

          final missing = RecoveryMessageMapper.map(
            ApiFailure.fromStatus(statusCode: 404, operation: operation),
          );
          expect(missing.canGoBack, isTrue, reason: '${operation.name} 404');
          expect(missing.canRetry, isFalse, reason: '${operation.name} 404');

          for (final status in [401, 403]) {
            final denied = RecoveryMessageMapper.map(
              ApiFailure.fromStatus(statusCode: status, operation: operation),
            );
            expect(denied.canGoBack, isTrue);
            expect(denied.canRetry, isFalse);
            expect(denied.message.toLowerCase(), isNot(contains('sign in')));
          }

          for (final status in [500, 503]) {
            final temporary = RecoveryMessageMapper.map(
              ApiFailure.fromStatus(statusCode: status, operation: operation),
            );
            expect(temporary.canGoBack, isTrue);
            expect(temporary.canRetry, isTrue);
          }
          expect(
            RecoveryMessageMapper.map(ApiFailure.network(operation)).canRetry,
            isTrue,
          );
          expect(
            RecoveryMessageMapper.map(ApiFailure.timeout(operation)).canRetry,
            isTrue,
          );
        }
      },
    );

    test('synthetic private diagnostics never reach passenger messages', () {
      const sentinels = [
        'SENTINEL_PRIVATE_TOKEN_123',
        'SENTINEL_INTERNAL_ACTOR_456',
        'SENTINEL_TRACE_SECRET_789',
      ];
      final problem = ProblemDetailsParser.parse('''
        {"type":"about:blank",
         "title":"SENTINEL_INTERNAL_ACTOR_456",
         "status":503,
         "detail":"SENTINEL_PRIVATE_TOKEN_123",
         "instance":"/internal/SENTINEL_TRACE_SECRET_789",
         "code":"SENTINEL_INTERNAL_ACTOR_456",
         "traceId":"SENTINEL_TRACE_SECRET_789",
         "errors":{"authorization":["SENTINEL_PRIVATE_TOKEN_123"]}}
      ''');
      final failure = ApiFailure.fromStatus(
        statusCode: 503,
        operation: RailwayApiOperation.journeySnapshot,
        problem: problem,
      );
      final recovery = RecoveryMessageMapper.map(failure);
      final passengerOutput = [
        failure.passengerMessage,
        recovery.message,
        ...recovery.fieldMessages.values,
      ].join(' ');

      expect(problem, isNotNull);
      for (final sentinel in sentinels) {
        expect(passengerOutput, isNot(contains(sentinel)));
      }
    });

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

    test(
      'search validation field feedback uses known fields and safe text',
      () {
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
      },
    );

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
