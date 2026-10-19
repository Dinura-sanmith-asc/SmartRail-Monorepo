import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app_config.dart';

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
  });
}
