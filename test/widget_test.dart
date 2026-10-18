import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/router.dart';

void main() {
  testWidgets('all foundation routes build and use the normal back stack', (
    tester,
  ) async {
    appRouter.go(AppRoutes.search);
    await tester.pumpWidget(
      ProviderScope(
        child: SmartRailApp(
          config: AppConfig.fromValue('https://api.smartrail.example'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(
      find.textContaining('Temporary placeholder: railway search'),
      findsOneWidget,
    );

    final resultsPop = appRouter.push<void>(AppRoutes.results);
    await tester.pumpAndSettle();
    expect(
      find.textContaining('Temporary placeholder: scheduled service results'),
      findsOneWidget,
    );

    final detailsPop = appRouter.push<void>(
      '/railway/services/phase-one-placeholder-id',
    );
    await tester.pumpAndSettle();
    expect(
      find.textContaining('Temporary placeholder for service'),
      findsOneWidget,
    );
    expect(find.textContaining('phase-one-placeholder-id'), findsOneWidget);

    appRouter.pop();
    await tester.pumpAndSettle();
    await detailsPop;
    expect(
      find.textContaining('Temporary placeholder: scheduled service results'),
      findsOneWidget,
    );

    appRouter.pop();
    await tester.pumpAndSettle();
    await resultsPop;
    expect(
      find.textContaining('Temporary placeholder: railway search'),
      findsOneWidget,
    );
  });
}
