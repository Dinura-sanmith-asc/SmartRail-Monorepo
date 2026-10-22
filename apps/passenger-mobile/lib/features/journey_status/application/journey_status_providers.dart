import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_api.dart';
import 'package:smartrail_passenger/features/journey_status/data/journey_status_repository_impl.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';

final journeyStatusApiProvider = Provider<JourneyStatusApiClient>((ref) {
  final AppConfig config = ref.watch(appConfigProvider);
  final baseUri = config.apiBaseUri;
  if (baseUri == null) {
    throw StateError(
      config.configurationError ?? 'Railway API URL is not configured.',
    );
  }
  final sharedClient = ref.watch(sharedHttpClientProvider);
  return JourneyStatusApi(
    HttpApiClient(baseUri: baseUri, client: sharedClient),
  );
});

final journeyStatusRepositoryProvider = Provider<JourneyStatusRepository>(
  (ref) => JourneyStatusRepositoryImpl(ref.watch(journeyStatusApiProvider)),
);
