import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_driver_mobile/features/authentication/presentation/home_screen.dart';
import 'package:smartrail_driver_mobile/features/authentication/application/app_controller.dart';
import 'package:smartrail_driver_mobile/features/assignments/domain/models.dart';
import 'package:smartrail_driver_mobile/features/observations/presentation/observation_screen.dart';

class _TestController extends AppController {
  _TestController({AppState? initial})
    : initial = initial ?? const AppState(authReady: true);
  final AppState initial;
  @override
  AppState build() => initial;
}

void main() {
  testWidgets('sign in entry explains secure organization authentication', (
    tester,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [appControllerProvider.overrideWith(_TestController.new)],
        child: const MaterialApp(home: HomeScreen()),
      ),
    );
    await tester.pump();
    expect(find.text('Sign in securely'), findsOneWidget);
    expect(find.textContaining('secure sign-in service'), findsOneWidget);
  });

  testWidgets('observation form requires nonblank text', (tester) async {
    final initial = AppState(
      token: 'test-token',
      identity: const DriverIdentity(subject: 'driver-sub', roles: ['Driver']),
      authReady: true,
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          appControllerProvider.overrideWith(
            () => _TestController(initial: initial),
          ),
        ],
        child: const MaterialApp(
          home: ObservationScreen(journeyId: 'journey-1'),
        ),
      ),
    );
    await tester.pump();
    await tester.tap(find.text('Send for review'));
    await tester.pump();
    expect(find.text('Enter an observation.'), findsOneWidget);
  });
}
