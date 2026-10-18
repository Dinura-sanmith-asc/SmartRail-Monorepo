import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';

void main() {
  test(
    'app-scoped provider owns the shared client and closes it exactly once',
    () {
      final client = _CloseTrackingClient();
      final container = ProviderContainer(
        overrides: [
          appConfigProvider.overrideWithValue(
            AppConfig.fromValue('https://railway.example.test'),
          ),
          httpClientFactoryProvider.overrideWithValue(() => client),
        ],
      );

      container.read(railwayApiClientProvider);
      expect(client.closeCount, 0);
      container.dispose();
      expect(client.closeCount, 1);
    },
  );
}

class _CloseTrackingClient extends http.BaseClient {
  int closeCount = 0;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async =>
      http.StreamedResponse(Stream.value(utf8.encode('{}')), 200);

  @override
  void close() => closeCount++;
}
