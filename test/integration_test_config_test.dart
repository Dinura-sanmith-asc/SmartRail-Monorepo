import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import '../integration_test/railway_search/integration_test_config.dart';

void main() {
  group('real-backend integration configuration', () {
    test('validates an explicit API URL through AppConfig', () {
      final config = AppConfig.fromValue('https://railway.example.test');

      expect(config.apiBaseUri, Uri.parse('https://railway.example.test'));
      expect(config.configurationError, isNull);
    });

    test('does not supply an implicit backend URL', () {
      final config = AppConfig.fromValue(null);

      expect(config.apiBaseUri, isNull);
      expect(config.configurationError, contains('SMART_RAIL_API_BASE_URL'));
    });

    test('fails fast when real backend configuration is missing', () {
      expect(
        () => IntegrationTestConfig.fromValues(
          apiBaseUrl: '',
          originStationId: '11111111-1111-4111-8111-111111111111',
          destinationStationId: '22222222-2222-4222-8222-222222222222',
          travelDate: '2026-10-19',
        ),
        throwsA(isA<StateError>()),
      );
    });

    test('requires distinct seeded UUID stations and a calendar date', () {
      const stationId = '11111111-1111-4111-8111-111111111111';
      expect(
        () => IntegrationTestConfig.fromValues(
          apiBaseUrl: 'https://railway.example.test',
          originStationId: stationId,
          destinationStationId: stationId,
          travelDate: '2026-10-19',
        ),
        throwsA(isA<StateError>()),
      );
      expect(
        () => IntegrationTestConfig.fromValues(
          apiBaseUrl: 'https://railway.example.test',
          originStationId: stationId,
          destinationStationId: '22222222-2222-4222-8222-222222222222',
          travelDate: 'tomorrow',
        ),
        throwsA(isA<StateError>()),
      );
    });
  });
}
