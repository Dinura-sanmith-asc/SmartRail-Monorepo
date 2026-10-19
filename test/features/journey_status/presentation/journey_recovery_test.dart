import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_recovery_section.dart';

void main() {
  testWidgets('temporary failures show section, Back, and Retry safely', (
    tester,
  ) async {
    var backCount = 0;
    var retryCount = 0;
    const sentinels = [
      'SENTINEL_PRIVATE_TOKEN_123',
      'SENTINEL_INTERNAL_ACTOR_456',
      'SENTINEL_TRACE_SECRET_789',
    ];
    final failure = ApiFailure(
      kind: ApiFailureKind.unavailable,
      operation: RailwayApiOperation.journeyUpdates,
      problem: ProblemDetails(
        type: sentinels[0],
        title: sentinels[1],
        status: 503,
        detail: sentinels[2],
        traceId: sentinels[0],
      ),
    );

    await tester.pumpWidget(
      MaterialApp(
        home: Scaffold(
          body: JourneyRecoverySection(
            sectionLabel: 'Published updates',
            failure: failure,
            onBack: () => backCount++,
            onRetry: () => retryCount++,
          ),
        ),
      ),
    );
    expect(find.text('Published updates'), findsOneWidget);
    expect(find.text('Back'), findsOneWidget);
    expect(find.text('Retry'), findsOneWidget);
    final semantics = tester.getSemantics(find.byType(JourneyRecoverySection));
    for (final sentinel in sentinels) {
      expect(semantics.label, isNot(contains(sentinel)));
      expect(find.textContaining(sentinel), findsNothing);
    }

    await tester.tap(find.text('Retry'));
    await tester.tap(find.text('Back'));
    expect(retryCount, 1);
    expect(backCount, 1);
  });

  testWidgets('invalid and access failures have Back without Retry', (
    tester,
  ) async {
    for (final failure in [
      ApiFailure.fromStatus(
        statusCode: 400,
        operation: RailwayApiOperation.journeySnapshot,
      ),
      ApiFailure.fromStatus(
        statusCode: 403,
        operation: RailwayApiOperation.journeySnapshot,
      ),
      ApiFailure.fromStatus(
        statusCode: 404,
        operation: RailwayApiOperation.journeySnapshot,
      ),
    ]) {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: JourneyRecoverySection(
              sectionLabel: 'Journey status',
              failure: failure,
              onBack: () {},
              onRetry: () {},
            ),
          ),
        ),
      );
      expect(find.text('Back'), findsOneWidget);
      expect(find.text('Retry'), findsNothing);
    }
  });
}
