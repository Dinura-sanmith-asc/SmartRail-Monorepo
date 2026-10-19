import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';

void main() {
  test(
    'repository uses configured base URI and shared anonymous client',
    () async {
      final requests = <http.Request>[];
      var clientsCreated = 0;
      final container = ProviderContainer(
        overrides: [
          appConfigProvider.overrideWithValue(
            AppConfig.fromValue('https://journeys.example.test'),
          ),
          httpClientFactoryProvider.overrideWithValue(() {
            clientsCreated++;
            return MockClient((request) async {
              requests.add(request);
              return http.Response('[]', 200);
            });
          }),
        ],
      );
      addTearDown(container.dispose);

      final repository = container.read(journeyStatusRepositoryProvider);
      await repository.getJourneyUpdates(
        journeyInstanceId: '11111111-1111-4111-8111-111111111111',
      );

      expect(clientsCreated, 1);
      expect(
        requests.single.url.toString(),
        'https://journeys.example.test/api/journeys/11111111-1111-4111-8111-111111111111/updates',
      );
      expect(requests.single.headers, isNot(contains('Authorization')));
    },
  );
}
