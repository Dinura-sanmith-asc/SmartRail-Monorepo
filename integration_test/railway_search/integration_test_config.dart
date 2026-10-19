import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

/// Explicit inputs for real-backend acceptance tests.
///
/// No mock or default railway data is substituted when a value is missing.
class IntegrationTestConfig {
  const IntegrationTestConfig({
    required this.apiBaseUri,
    required this.originStationId,
    required this.destinationStationId,
    required this.travelDate,
  });

  static const _baseUrl = String.fromEnvironment('SMART_RAIL_API_BASE_URL');
  static const _originId = String.fromEnvironment(
    'SMART_RAIL_TEST_ORIGIN_ID',
  );
  static const _destinationId = String.fromEnvironment(
    'SMART_RAIL_TEST_DESTINATION_ID',
  );
  static const _travelDate = String.fromEnvironment('SMART_RAIL_TEST_DATE');

  final Uri apiBaseUri;
  final String originStationId;
  final String destinationStationId;
  final TravelDate travelDate;

  factory IntegrationTestConfig.fromEnvironment() {
    final appConfig = AppConfig.fromValue(_baseUrl);
    final baseUri = appConfig.apiBaseUri;
    if (baseUri == null) {
      throw StateError(
        'Real-backend tests require a valid SMART_RAIL_API_BASE_URL. '
        'No mock backend is substituted.',
      );
    }
    if (!_isUuid(_originId) || !_isUuid(_destinationId)) {
      throw StateError(
        'Real-backend tests require seeded SMART_RAIL_TEST_ORIGIN_ID and '
        'SMART_RAIL_TEST_DESTINATION_ID UUID values.',
      );
    }
    if (_originId == _destinationId) {
      throw StateError('The seeded origin and destination IDs must differ.');
    }
    final TravelDate date;
    try {
      date = TravelDate.parse(_travelDate);
    } on FormatException catch (error) {
      throw StateError(
        'Real-backend tests require SMART_RAIL_TEST_DATE in YYYY-MM-DD format: $error',
      );
    }
    return IntegrationTestConfig(
      apiBaseUri: baseUri,
      originStationId: _originId,
      destinationStationId: _destinationId,
      travelDate: date,
    );
  }

  static bool _isUuid(String value) => RegExp(
    r'^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$',
  ).hasMatch(value);
}
