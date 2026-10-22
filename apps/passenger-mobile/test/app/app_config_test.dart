import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/app/app_config.dart';

void main() {
  group('AppConfig.fromValue', () {
    test('accepts an explicitly supplied absolute HTTP API base URL', () {
      final config = AppConfig.fromValue('https://api.smartrail.example/v1');

      expect(config.isConfigured, isTrue);
      expect(config.apiBaseUri, Uri.parse('https://api.smartrail.example/v1'));
      expect(config.configurationError, isNull);
    });

    test(
      'keeps a missing API URL unconfigured instead of defaulting to localhost',
      () {
        final config = AppConfig.fromValue(null);

        expect(config.isConfigured, isFalse);
        expect(config.apiBaseUri, isNull);
        expect(config.configurationError, isNotEmpty);
        expect(config.apiBaseUri?.host, isNot('localhost'));
      },
    );

    test('reports malformed API URLs as an unconfigured state', () {
      final config = AppConfig.fromValue('https://[');

      expect(config.isConfigured, isFalse);
      expect(config.apiBaseUri, isNull);
      expect(config.configurationError, isNotEmpty);
    });

    test('rejects unsupported schemes and URLs without a host', () {
      expect(
        AppConfig.fromValue('ftp://api.smartrail.example').isConfigured,
        isFalse,
      );
      expect(AppConfig.fromValue('https:///railway').isConfigured, isFalse);
    });
  });
}
