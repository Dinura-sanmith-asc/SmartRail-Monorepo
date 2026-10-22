import 'package:flutter_test/flutter_test.dart';

import '../integration_test/journey_status/journey_status_integration_config.dart';

void main() {
  group('journey status integration configuration', () {
    const origin = '11111111-1111-4111-8111-111111111111';
    const service = '22222222-2222-4222-8222-222222222222';
    const valid = {
      'SMART_RAIL_API_BASE_URL': 'https://railway.example.test',
      'SMART_RAIL_TEST_ORIGIN_STATION_ID': origin,
      'SMART_RAIL_TEST_SERVICE_ID': service,
      'SMART_RAIL_TEST_DATE': '2026-10-19',
    };

    test('accepts all explicitly supplied integration defines', () {
      final config = JourneyStatusIntegrationConfig.fromValues(valid);

      expect(config.apiBaseUri, Uri.parse('https://railway.example.test'));
      expect(config.originStationId, origin);
      expect(config.serviceId, service);
      expect(config.testDate, '2026-10-19');
    });

    for (final key in valid.keys) {
      test('fails with setup guidance when $key is missing', () {
        final values = Map<String, String>.of(valid)..remove(key);

        expect(
          () => JourneyStatusIntegrationConfig.fromValues(values),
          throwsA(
            isA<StateError>().having(
              (error) => error.message,
              'message',
              contains(key),
            ),
          ),
        );
      });
    }

    test('rejects malformed API URL, station UUID, service UUID and date', () {
      for (final invalid in [
        {...valid, 'SMART_RAIL_API_BASE_URL': 'not-a-url'},
        {...valid, 'SMART_RAIL_TEST_ORIGIN_STATION_ID': 'station-one'},
        {...valid, 'SMART_RAIL_TEST_SERVICE_ID': 'service-one'},
        {...valid, 'SMART_RAIL_TEST_DATE': '2026-02-30'},
        {...valid, 'SMART_RAIL_TEST_DATE': '19/10/2026'},
      ]) {
        expect(
          () => JourneyStatusIntegrationConfig.fromValues(invalid),
          throwsA(isA<StateError>()),
        );
      }
    });

    test('never supplies a fallback or mock backend', () {
      expect(
        () => JourneyStatusIntegrationConfig.fromValues(const {}),
        throwsA(isA<StateError>()),
      );
    });
  });
}
