import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';

class _TrackingClient extends http.BaseClient {
  int closeCount = 0;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async =>
      http.StreamedResponse(const Stream<List<int>>.empty(), 200);

  @override
  void close() => closeCount++;
}

void main() {
  test('journey provider invalidation keeps the app-owned client alive', () {
    final client = _TrackingClient();
    final container = ProviderContainer(
      overrides: [
        appConfigProvider.overrideWithValue(
          AppConfig.fromValue('https://journeys.example.test'),
        ),
        httpClientFactoryProvider.overrideWithValue(() => client),
      ],
    );

    container.read(journeyStatusApiProvider);
    container.invalidate(journeyStatusApiProvider);
    expect(client.closeCount, 0);

    container.dispose();
    expect(client.closeCount, 1);
  });
}
