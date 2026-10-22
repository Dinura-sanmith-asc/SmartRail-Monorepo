import 'dart:convert';

import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_api_dtos.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

class RailwayNetworkApi {
  const RailwayNetworkApi(this._http);

  final HttpApiClient _http;

  Future<StationListDto> listStations({Future<void>? abortTrigger}) async =>
      _decode(
        _http.endpoint('/api/stations'),
        RailwayApiOperation.stations,
        (json) => StationListDto.fromJson(json),
        abortTrigger,
      );

  Future<RouteStopsDto> getRouteStops(
    String id, {
    Future<void>? abortTrigger,
  }) async => _decode(
    _http.endpoint('/api/routes/${Uri.encodeComponent(id)}/stops'),
    RailwayApiOperation.routeStops,
    (json) => RouteStopsDto.fromJson(json),
    abortTrigger,
  );

  Future<ServiceSearchResponseDto> searchServices({
    required String origin,
    required String destination,
    required TravelDate date,
    Future<void>? abortTrigger,
  }) async {
    final dateText = date.toIsoDate();
    return _decode(
      _http.endpoint(
        '/api/services',
        queryParameters: {
          'origin': origin,
          'destination': destination,
          'date': dateText,
        },
      ),
      RailwayApiOperation.serviceSearch,
      (json) => ServiceSearchResponseDto.fromJson(json),
      abortTrigger,
    );
  }

  Future<ServiceDetailDto> getService(
    String id, {
    Future<void>? abortTrigger,
  }) async => _decode(
    _http.endpoint('/api/services/${Uri.encodeComponent(id)}'),
    RailwayApiOperation.serviceDetail,
    (json) => ServiceDetailDto.fromJson(json),
    abortTrigger,
  );

  Future<T> _decode<T>(
    Uri uri,
    RailwayApiOperation operation,
    T Function(Object?) parse,
    Future<void>? abortTrigger,
  ) async {
    try {
      final body = await _http.getText(
        uri,
        operation: operation,
        abortTrigger: abortTrigger,
      );
      try {
        return parse(jsonDecode(body));
      } on FormatException catch (error) {
        throw ApiFailure.protocol(operation, error);
      }
    } on ApiFailure {
      rethrow;
    }
  }
}
