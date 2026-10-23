import 'dart:convert';

import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/core/network/http_api_client.dart';

class CateringAuth {
  CateringAuth(this.storage);
  final FlutterSecureStorage storage;
  static const _issuer = String.fromEnvironment('SMART_RAIL_COGNITO_ISSUER');
  static const _clientId = String.fromEnvironment(
    'SMART_RAIL_COGNITO_CLIENT_ID',
  );
  static const _redirect = String.fromEnvironment(
    'SMART_RAIL_COGNITO_REDIRECT_URI',
  );

  Future<String> accessToken() async {
    if (_issuer.isEmpty || _clientId.isEmpty || _redirect.isEmpty) {
      throw StateError('Catering sign-in is not configured.');
    }
    final expiry = DateTime.tryParse(
      await storage.read(key: 'catering_expiry') ?? '',
    );
    final saved = await storage.read(key: 'catering_access_token');
    if (saved != null &&
        expiry != null &&
        expiry.isAfter(DateTime.now().add(const Duration(minutes: 1)))) {
      return saved;
    }
    final refresh = await storage.read(key: 'catering_refresh_token');
    if (refresh != null) {
      try {
        final result = await const FlutterAppAuth().token(
          TokenRequest(
            _clientId,
            _redirect,
            issuer: _issuer,
            refreshToken: refresh,
            scopes: ['openid', 'email', 'profile'],
          ),
        );
        if (result.accessToken != null) {
          await _save(
            result.accessToken!,
            result.accessTokenExpirationDateTime,
            result.refreshToken ?? refresh,
          );
          return result.accessToken!;
        }
      } catch (_) {
        await storage.delete(key: 'catering_refresh_token');
      }
    }
    final result = await const FlutterAppAuth().authorizeAndExchangeCode(
      AuthorizationTokenRequest(
        _clientId,
        _redirect,
        issuer: _issuer,
        scopes: ['openid', 'email', 'profile'],
      ),
    );
    if (result.accessToken == null) {
      throw StateError('Sign-in was cancelled.');
    }
    await _save(
      result.accessToken!,
      result.accessTokenExpirationDateTime,
      result.refreshToken,
    );
    return result.accessToken!;
  }

  Future<void> _save(String token, DateTime? expiry, String? refresh) async {
    await storage.write(key: 'catering_access_token', value: token);
    await storage.write(
      key: 'catering_expiry',
      value: (expiry ?? DateTime.now().add(const Duration(minutes: 30)))
          .toIso8601String(),
    );
    if (refresh != null) {
      await storage.write(key: 'catering_refresh_token', value: refresh);
    }
  }
}

class CateringItem {
  CateringItem(Map<String, dynamic> json)
    : id = json['id'] as String,
      name = json['name'] as String,
      description = json['description'] as String,
      category = json['category'] as String,
      price = (json['price'] as num).toDouble(),
      currency = json['currency'] as String,
      available = json['available'] as bool;
  final String id, name, description, category, currency;
  final double price;
  final bool available;
}

class CateringPoint {
  CateringPoint(Map<String, dynamic> json)
    : stationId = json['stationId'] as String,
      stationName = json['stationName'] as String,
      instructions = json['instructions'] as String;
  final String stationId, stationName, instructions;
}

class CateringOptions {
  CateringOptions(Map<String, dynamic> json)
    : items = (json['items'] as List)
          .map((e) => CateringItem(e as Map<String, dynamic>))
          .toList(),
      points = (json['collectionPoints'] as List)
          .map((e) => CateringPoint(e as Map<String, dynamic>))
          .toList();
  final List<CateringItem> items;
  final List<CateringPoint> points;
}

class CateringOrder {
  CateringOrder(Map<String, dynamic> json)
    : id = json['id'] as String,
      status = json['status'] as String,
      collectionStationId = json['collectionStationId'] as String,
      total = (json['total'] as num?)?.toDouble(),
      currency = json['currency'] as String,
      items = (json['items'] as List).cast<Map<String, dynamic>>();
  final String id, status, collectionStationId, currency;
  final double? total;
  final List<Map<String, dynamic>> items;
}

class CateringApi {
  CateringApi(this.client, this.httpClient, this.auth, this.storage);
  final HttpApiClient client;
  final http.Client httpClient;
  final CateringAuth auth;
  final FlutterSecureStorage storage;

  Future<dynamic> _send(
    String method,
    String path, {
    Object? body,
    String? key,
    bool authenticated = false,
  }) async {
    final request = http.Request(method, client.endpoint(path));
    request.headers['Accept'] = 'application/json';
    if (authenticated) {
      request.headers['Authorization'] = 'Bearer ${await auth.accessToken()}';
    }
    if (key != null) request.headers['Idempotency-Key'] = key;
    if (body != null) {
      request.headers['Content-Type'] = 'application/json';
      request.body = jsonEncode(body);
    }
    final streamed = await httpClient
        .send(request)
        .timeout(const Duration(seconds: 10));
    final response = await http.Response.fromStream(streamed);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      if (response.statusCode == 401) {
        await storage.delete(key: 'catering_access_token');
        throw StateError('Sign in again to order food.');
      }
      if (response.statusCode == 503) {
        throw StateError('Catering is temporarily unavailable. Retry safely.');
      }
      throw StateError('Catering request failed (${response.statusCode}).');
    }
    return jsonDecode(response.body);
  }

  Future<CateringOptions> options(String journeyId) async => CateringOptions(
    await _send('GET', '/api/catering/options?journeyId=$journeyId')
        as Map<String, dynamic>,
  );
  Future<CateringOrder> create(
    String journeyId,
    String stationId,
    Map<String, int> cart,
    String key,
  ) async {
    final body = {
      'journeyInstanceId': journeyId,
      'collectionStationId': stationId,
      'items': cart.entries.toList()..sort((a, b) => a.key.compareTo(b.key)),
    };
    final requestBody = {
      'journeyInstanceId': journeyId,
      'collectionStationId': stationId,
      'items': (body['items'] as List<MapEntry<String, int>>)
          .map((e) => {'menuItemId': e.key, 'quantity': e.value})
          .toList(),
    };
    final fingerprint = jsonEncode(requestBody);
    final saved = await storage.read(key: 'catering_pending_order');
    final savedKey = await storage.read(key: 'catering_pending_key');
    final stableKey = saved == fingerprint && savedKey != null ? savedKey : key;
    await storage.write(key: 'catering_pending_order', value: fingerprint);
    await storage.write(key: 'catering_pending_key', value: stableKey);
    final result = CateringOrder(
      await _send(
        'POST',
        '/api/catering/orders',
        authenticated: true,
        key: stableKey,
        body: requestBody,
      ) as Map<String, dynamic>,
    );
    await storage.delete(key: 'catering_pending_order');
    await storage.delete(key: 'catering_pending_key');
    return result;
  }

  Future<List<CateringOrder>> orders() async =>
      (await _send('GET', '/api/catering/orders', authenticated: true) as List)
          .map((e) => CateringOrder(e as Map<String, dynamic>))
          .toList();
  Future<CateringOrder> order(String id) async => CateringOrder(
    await _send('GET', '/api/catering/orders/$id', authenticated: true)
        as Map<String, dynamic>,
  );
}
