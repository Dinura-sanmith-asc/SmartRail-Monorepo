import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';

void main() {
  group('ProblemDetailsParser', () {
    test(
      'parses required RFC 9457 fields and all documented optional fields',
      () {
        final problem = ProblemDetailsParser.parse('''
        {"type":"about:blank","title":"Invalid","status":400,
         "detail":"Correct station","instance":"/api/services",
         "code":"criteria.invalid","errors":{"origin":["Required"]},"traceId":"secret"}
      ''');
        expect(problem, isNotNull);
        expect(problem!.status, 400);
        expect(problem.detail, 'Correct station');
        expect(problem.errors['origin'], ['Required']);
        expect(problem.traceId, 'secret');
      },
    );

    test('accepts absent optional fields and safely returns null for malformed bodies', () {
      expect(
        ProblemDetailsParser.parse(
          '{"type":"about:blank","title":"Oops","status":503}',
        ),
        isNotNull,
      );
      expect(ProblemDetailsParser.parse('not json'), isNull);
      expect(ProblemDetailsParser.parse('[]'), isNull);
      expect(ProblemDetailsParser.parse('{"type":"x","status":"503"}'), isNull);
    });
  });

  group('API failure classification', () {
    test(
      'classifies 400 according to the operation without changing its status',
      () {
        expect(
          ApiFailure.fromStatus(
            statusCode: 400,
            operation: RailwayApiOperation.serviceSearch,
          ).kind,
          ApiFailureKind.validation,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 400,
            operation: RailwayApiOperation.serviceDetail,
          ).kind,
          ApiFailureKind.invalidRequest,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 400,
            operation: RailwayApiOperation.routeStops,
          ).kind,
          ApiFailureKind.invalidRequest,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 400,
            operation: RailwayApiOperation.stations,
          ).kind,
          ApiFailureKind.unexpected,
        );
      },
    );

    test(
      'classifies access, not-found, unavailable and unexpected responses',
      () {
        expect(
          ApiFailure.fromStatus(
            statusCode: 403,
            operation: RailwayApiOperation.stations,
          ).kind,
          ApiFailureKind.accessDenied,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 404,
            operation: RailwayApiOperation.serviceDetail,
          ).kind,
          ApiFailureKind.notFound,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 404,
            operation: RailwayApiOperation.serviceSearch,
          ).kind,
          ApiFailureKind.unexpected,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 503,
            operation: RailwayApiOperation.stations,
          ).kind,
          ApiFailureKind.unavailable,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 500,
            operation: RailwayApiOperation.stations,
          ).kind,
          ApiFailureKind.unexpected,
        );
        expect(
          ApiFailure.fromStatus(
            statusCode: 418,
            operation: RailwayApiOperation.stations,
          ).kind,
          ApiFailureKind.unexpected,
        );
      },
    );

    test('passenger messages never include backend diagnostics', () {
      final failure = ApiFailure.fromStatus(
        statusCode: 500,
        operation: RailwayApiOperation.serviceSearch,
        problem: ProblemDetailsParser.parse(
          '{"type":"x","title":"db password","status":500,"detail":"secret stack","traceId":"trace-secret"}',
        ),
      );
      expect(failure.passengerMessage, isNot(contains('secret')));
      expect(failure.passengerMessage, isNot(contains('password')));
      expect(failure.passengerMessage, isNot(contains('trace')));
    });
  });
}
