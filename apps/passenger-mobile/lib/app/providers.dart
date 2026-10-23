import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_network_api.dart';
import 'package:smartrail_passenger/features/railway_search/data/railway_search_repository_impl.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_search_repository.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:smartrail_passenger/features/catering/data/catering_api.dart';

typedef HttpClientFactory = http.Client Function();

final appConfigProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);

final httpClientFactoryProvider = Provider<HttpClientFactory>(
  (ref) => http.Client.new,
);

/// One shared client is owned by this app-scope provider and closed on disposal.
final sharedHttpClientProvider = Provider<http.Client>((ref) {
  final client = ref.watch(httpClientFactoryProvider)();
  ref.onDispose(client.close);
  return client;
});

final railwayApiClientProvider = Provider<RailwayNetworkApi>((ref) {
  final config = ref.watch(appConfigProvider);
  final baseUri = config.apiBaseUri;
  if (baseUri == null) {
    throw StateError(
      config.configurationError ?? 'Railway API URL is not configured.',
    );
  }
  return RailwayNetworkApi(
    HttpApiClient(
      baseUri: baseUri,
      client: ref.watch(sharedHttpClientProvider),
    ),
  );
});

final railwaySearchRepositoryProvider = Provider<RailwaySearchRepository>(
  (ref) => RailwaySearchRepositoryImpl(ref.watch(railwayApiClientProvider)),
);

final cateringApiProvider = Provider<CateringApi>((ref) {
  final config = ref.watch(appConfigProvider);
  if (config.apiBaseUri == null) {
    throw StateError(config.configurationError ?? 'API URL is not configured.');
  }
  const storage = FlutterSecureStorage();
  return CateringApi(
    HttpApiClient(
      baseUri: config.apiBaseUri!,
      client: ref.watch(sharedHttpClientProvider),
    ),
    ref.watch(sharedHttpClientProvider),
    CateringAuth(storage),
    storage,
  );
});
