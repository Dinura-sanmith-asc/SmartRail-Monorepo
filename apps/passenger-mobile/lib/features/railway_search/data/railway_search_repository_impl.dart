import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_api_dtos.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';

class RailwaySearchRepositoryImpl implements RailwaySearchRepository {
  const RailwaySearchRepositoryImpl(this._api);

  final RailwayNetworkApi _api;

  @override
  Future<List<Station>> listStations() async {
    final response = await _api.listStations();
    return List.unmodifiable(
      response.items.map(
        (station) => Station(
          id: station.stationId,
          name: station.name,
          locationDetails: station.locationDetails,
          serviceDetails: station.serviceDetails,
        ),
      ),
    );
  }

  @override
  Future<ServiceSearchResponse> search(SearchCriteria criteria) async {
    if (!criteria.isSubmittable) {
      throw ArgumentError.value(
        criteria,
        'criteria',
        'Origin, destination and date must be present and station IDs must differ.',
      );
    }
    final response = await _api.searchServices(
      origin: criteria.originStationId!,
      destination: criteria.destinationStationId!,
      date: criteria.travelDate!,
    );
    return ServiceSearchResponse(
      originStationId: response.originStationId,
      destinationStationId: response.destinationStationId,
      travelDate: TravelDate.parse(response.travelDate),
      services: response.services
          .map(
            (service) => ScheduledServiceChoice(
              serviceId: service.serviceId,
              serviceCode: service.serviceCode,
              routeId: service.routeId,
              routeCode: service.routeCode,
              scheduleRevision: service.scheduleRevision,
              origin: _mapCall(service.origin),
              destination: _mapCall(service.destination),
            ),
          )
          .toList(),
    );
  }

  @override
  Future<ServiceDetail> getService(String serviceId) async {
    final detail = await _api.getService(serviceId);
    return ServiceDetail(
      serviceId: detail.serviceId,
      serviceCode: detail.serviceCode,
      route: ServiceRoute(
        routeId: detail.route.routeId,
        routeCode: detail.route.routeCode,
        name: detail.route.name,
      ),
      schedule: ServiceSchedule(
        revision: detail.schedule.revision,
        firstDepartureTimeLocal: detail.schedule.firstDepartureTimeLocal,
      ),
    );
  }

  @override
  Future<RouteStops> getRouteStops(String routeId) async {
    final route = await _api.getRouteStops(routeId);
    return RouteStops(
      routeId: route.routeId,
      routeCode: route.routeCode,
      name: route.name,
      stops: route.stops
          .map(
            (stop) => RouteStop(
              stopOrder: stop.stopOrder,
              stationId: stop.stationId,
              stationName: stop.stationName,
            ),
          )
          .toList(),
    );
  }

  ScheduledCall _mapCall(ScheduledCallDto call) => ScheduledCall(
    stationId: call.stationId,
    stationName: call.stationName,
    stopOrder: call.stopOrder,
    scheduledAt: call.scheduledAt,
  );
}
