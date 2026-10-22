import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

abstract interface class RailwaySearchRepository {
  Future<List<Station>> listStations();

  Future<ServiceSearchResponse> search(SearchCriteria criteria);

  Future<ServiceDetail> getService(String serviceId);

  Future<RouteStops> getRouteStops(String routeId);
}
