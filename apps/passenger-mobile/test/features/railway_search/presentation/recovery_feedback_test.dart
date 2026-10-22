import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/retry_section.dart';

void main() {
  testWidgets('search 400 gives correction feedback and no retry', (
    tester,
  ) async {
    await _pump(
      tester,
      ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.serviceSearch,
        problem: ProblemDetailsParser.parse(
          '{"type":"about:blank","title":"Invalid","status":400,'
          '"errors":{"origin":["private diagnostic"]}}',
        ),
      ),
    );

    expect(find.text('Check your information'), findsOneWidget);
    expect(find.text('Check the origin station.'), findsOneWidget);
    expect(find.text('Retry'), findsNothing);
    expect(find.text('private diagnostic'), findsNothing);
    expect(find.text('Back'), findsOneWidget);
  });

  testWidgets('403 is access unavailable with Back only', (tester) async {
    await _pump(
      tester,
      ApiFailure.fromStatus(
        statusCode: 403,
        operation: RailwayApiOperation.serviceSearch,
      ),
    );

    expect(find.text('Access unavailable'), findsOneWidget);
    expect(find.text('Back'), findsOneWidget);
    expect(find.text('Retry'), findsNothing);
    expect(find.text('Sign in'), findsNothing);
  });

  testWidgets('detail 400 and 404 are unavailable without blind retry', (
    tester,
  ) async {
    for (final failure in [
      ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.serviceDetail,
      ),
      ApiFailure.fromStatus(
        statusCode: 404,
        operation: RailwayApiOperation.routeStops,
      ),
    ]) {
      await _pump(tester, failure);
      expect(find.text('Information unavailable'), findsOneWidget);
      expect(find.text('Back'), findsOneWidget);
      expect(find.text('Retry'), findsNothing);
    }
  });

  testWidgets('retryable failures have safe Retry and Back actions', (
    tester,
  ) async {
    var retries = 0;
    var backs = 0;
    await _pump(
      tester,
      ApiFailure.fromStatus(
        statusCode: 503,
        operation: RailwayApiOperation.routeStops,
        problem: ProblemDetailsParser.parse(
          '{"type":"about:blank","title":"Unavailable",'
          '"status":503,"detail":"secret backend trace"}',
        ),
      ),
      onRetry: () => retries++,
      onBack: () => backs++,
    );

    expect(
      find.text(
        'Railway information is temporarily unavailable. Please try again.',
      ),
      findsOneWidget,
    );
    expect(find.textContaining('secret backend trace'), findsNothing);
    await tester.tap(find.text('Retry'));
    await tester.tap(find.text('Back'));
    expect(retries, 1);
    expect(backs, 1);

    for (final failure in [
      ApiFailure.fromStatus(
        statusCode: 500,
        operation: RailwayApiOperation.serviceDetail,
      ),
      ApiFailure.network(RailwayApiOperation.routeStops),
    ]) {
      await _pump(tester, failure);
      expect(find.text('Information unavailable'), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);
      expect(find.text('Back'), findsOneWidget);
    }
  });
}

Future<void> _pump(
  WidgetTester tester,
  ApiFailure failure, {
  VoidCallback? onRetry,
  VoidCallback? onBack,
}) async {
  await tester.pumpWidget(
    MaterialApp(
      home: Scaffold(
        body: RetrySection(
          failure: failure,
          onRetry: onRetry ?? () {},
          onBack: onBack ?? () {},
        ),
      ),
    ),
  );
  await tester.pumpAndSettle();
}
