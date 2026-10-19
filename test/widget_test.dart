import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

void main() {
  testWidgets('all foundation routes build and use the normal back stack', (
    tester,
  ) async {
    appRouter.go(AppRoutes.search);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          railwaySearchRepositoryProvider.overrideWithValue(_FakeRepository()),
        ],
        child: SmartRailApp(
          config: AppConfig.fromValue('https://api.smartrail.example'),
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Search railway services'), findsOneWidget);
    expect(find.text('No published stations are available.'), findsOneWidget);

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
    expect(find.text('Search railway services'), findsOneWidget);
  });
}

class _FakeRepository implements RailwaySearchRepository {
  @override
  Future<List<Station>> listStations() async => const [];

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) =>
      throw UnimplementedError();

  @override
  Future<ServiceDetail> getService(String serviceId) =>
      throw UnimplementedError();

  @override
  Future<RouteStops> getRouteStops(String routeId) =>
      throw UnimplementedError();
}
