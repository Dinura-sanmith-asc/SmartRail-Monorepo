/// Explicit configuration for real-backend Feature 002 integration tests.
///
/// Missing values fail closed; this class never provides mock or local
/// fallback data.
class JourneyStatusIntegrationConfig {
  JourneyStatusIntegrationConfig._({
    required this.apiBaseUri,
    required this.originStationId,
    required this.serviceId,
    required this.testDate,
  });

  static const _apiUrlDefine = 'SMART_RAIL_API_BASE_URL';
  static const _originDefine = 'SMART_RAIL_TEST_ORIGIN_STATION_ID';
  static const _serviceDefine = 'SMART_RAIL_TEST_SERVICE_ID';
  static const _dateDefine = 'SMART_RAIL_TEST_DATE';

  final Uri apiBaseUri;
  final String originStationId;
  final String serviceId;
  final String testDate;

  factory JourneyStatusIntegrationConfig.fromEnvironment() =>
      JourneyStatusIntegrationConfig.fromValues(const {
        _apiUrlDefine: String.fromEnvironment(_apiUrlDefine),
        _originDefine: String.fromEnvironment(_originDefine),
        _serviceDefine: String.fromEnvironment(_serviceDefine),
        _dateDefine: String.fromEnvironment(_dateDefine),
      });

  /// Creates validated configuration from the four documented dart-defines.
  /// This separate constructor keeps validation deterministic in unit tests.
  factory JourneyStatusIntegrationConfig.fromValues(
    Map<String, String> values,
  ) {
    String requiredValue(String name) {
      final value = values[name]?.trim();
      if (value == null || value.isEmpty) {
        throw StateError(
          'Integration setup error: provide --dart-define=$name=<value>.',
        );
      }
      return value;
    }

    final apiUrl = requiredValue(_apiUrlDefine);
    final apiUri = Uri.tryParse(apiUrl);
    if (apiUri == null ||
        !apiUri.isAbsolute ||
        !const {'http', 'https'}.contains(apiUri.scheme.toLowerCase()) ||
        apiUri.host.isEmpty ||
        apiUri.userInfo.isNotEmpty ||
        apiUri.hasQuery ||
        apiUri.hasFragment) {
      throw StateError(
        'Integration setup error: $_apiUrlDefine must be an absolute HTTP(S) base URL without credentials, query, or fragment.',
      );
    }

    final origin = requiredValue(_originDefine);
    if (!_isUuid(origin)) {
      throw StateError(
        'Integration setup error: $_originDefine must be a UUID.',
      );
    }

    final service = requiredValue(_serviceDefine);
    if (!_isUuid(service)) {
      throw StateError(
        'Integration setup error: $_serviceDefine must be a UUID.',
      );
    }

    final date = requiredValue(_dateDefine);
    if (!_isCalendarDate(date)) {
      throw StateError(
        'Integration setup error: $_dateDefine must be a valid YYYY-MM-DD calendar date.',
      );
    }

    return JourneyStatusIntegrationConfig._(
      apiBaseUri: apiUri,
      originStationId: origin,
      serviceId: service,
      testDate: date,
    );
  }

  static bool _isUuid(String value) =>
      RegExp(r'^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$')
          .hasMatch(value);

  static bool _isCalendarDate(String value) {
    if (!RegExp(r'^\d{4}-\d{2}-\d{2}$').hasMatch(value)) return false;
    final parts = value.split('-').map(int.parse).toList(growable: false);
    final parsed = DateTime.utc(parts[0], parts[1], parts[2]);
    return parsed.year == parts[0] &&
        parsed.month == parts[1] &&
        parsed.day == parts[2];
  }
}
