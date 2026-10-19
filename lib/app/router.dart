import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/search_results_screen.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_screen.dart';

abstract final class AppRoutes {
  static const String search = '/railway/search';
  static const String results = '/railway/results';
  static const String serviceDetails = '/railway/services/:serviceId';
}

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.search,
  routes: [
    GoRoute(
      path: AppRoutes.search,
      name: 'railway-search',
      builder: (context, state) => const RailwaySearchScreen(),
    ),
    GoRoute(
      path: AppRoutes.results,
      name: 'railway-results',
      builder: (context, state) => const SearchResultsScreen(),
    ),
    GoRoute(
      path: AppRoutes.serviceDetails,
      name: 'railway-service-details',
      builder: (context, state) {
        final selection =
            state.extra!
                as ({ScheduledServiceChoice choice, TravelDate travelDate});
        return ServiceDetailScreen(
          choice: selection.choice,
          travelDate: selection.travelDate,
        );
      },
    ),
  ],
);
