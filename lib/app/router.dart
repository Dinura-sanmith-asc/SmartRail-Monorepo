import 'package:go_router/go_router.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/railway_search_screen.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/search_results_screen.dart';
import 'package:smartrail_passenger/features/railway_search/presentation/service_detail_screen.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_lookup_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_status_screen.dart';

abstract final class AppRoutes {
  static const String search = '/railway/search';
  static const String results = '/railway/results';
  static const String serviceDetails = '/railway/services/:serviceId';
  static const String journeyLookup = '/journeys/lookup';
  static const String journeyStatus = '/journeys/:journeyInstanceId';
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
      path: AppRoutes.journeyLookup,
      name: 'journey-lookup',
      builder: (context, state) {
        final selection =
            state.extra!
                as ({ScheduledServiceChoice choice, TravelDate travelDate});
        final request = JourneyLookupRequest(
          choice: selection.choice,
          travelDate: selection.travelDate,
        );
        return JourneyLookupScreen(
          request: request,
          onJourneySelected: (candidate) => context.pushNamed(
            'journey-status',
            pathParameters: {'journeyInstanceId': candidate.journeyInstanceId},
            extra: (
              choice: selection.choice,
              travelDate: selection.travelDate,
              journeyInstanceId: candidate.journeyInstanceId,
            ),
          ),
        );
      },
    ),
    GoRoute(
      path: AppRoutes.journeyStatus,
      name: 'journey-status',
      builder: (context, state) {
        final selection =
            state.extra!
                as ({
                  ScheduledServiceChoice choice,
                  TravelDate travelDate,
                  String journeyInstanceId,
                });
        final pathId = state.pathParameters['journeyInstanceId']!;
        if (selection.journeyInstanceId != pathId) {
          throw StateError(
            'Journey route identity does not match its context.',
          );
        }
        return JourneyStatusScreen(journeyInstanceId: pathId);
      },
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
