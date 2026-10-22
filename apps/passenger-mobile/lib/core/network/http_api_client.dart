import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';

class HttpApiClient {
  HttpApiClient({
    required this.baseUri,
    required this.client,
    this.timeout = const Duration(seconds: 10),
  }) {
    if (!baseUri.hasAuthority ||
        !{'http', 'https'}.contains(baseUri.scheme) ||
        baseUri.host.isEmpty) {
      throw ArgumentError.value(
        baseUri,
        'baseUri',
        'Must be an absolute HTTP(S) URI.',
      );
    }
    if (timeout <= Duration.zero || timeout > const Duration(minutes: 2)) {
      throw ArgumentError.value(
        timeout,
        'timeout',
        'Must be positive and at most two minutes.',
      );
    }
  }

  final Uri baseUri;
  final http.Client client;
  final Duration timeout;

  Future<String> getText(
    Uri uri, {
    required RailwayApiOperation operation,
    Future<void>? abortTrigger,
  }) async {
    try {
      final request = abortTrigger == null
          ? http.Request('GET', uri)
          : http.AbortableRequest('GET', uri, abortTrigger: abortTrigger);
      request.headers['Accept'] = 'application/json, application/problem+json';
      final stopwatch = Stopwatch()..start();
      final streamed = await client.send(request).timeout(timeout);
      final remaining = timeout - stopwatch.elapsed;
      final response = await http.Response.fromStream(streamed)
          .timeout(remaining > Duration.zero ? remaining : Duration.zero);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        String body;
        try {
          body = utf8.decode(response.bodyBytes, allowMalformed: false);
        } on FormatException {
          body = '';
        }
        final problem = ProblemDetailsParser.parse(body);
        throw ApiFailure.fromStatus(
          statusCode: response.statusCode,
          operation: operation,
          problem: problem,
        );
      }
      return utf8.decode(response.bodyBytes, allowMalformed: false);
    } on ApiFailure {
      rethrow;
    } on TimeoutException catch (error) {
      throw ApiFailure.timeout(operation, error);
    } on FormatException catch (error) {
      throw ApiFailure.protocol(operation, error);
    } on http.ClientException catch (error) {
      throw ApiFailure.network(operation, error);
    } on Exception catch (error) {
      throw ApiFailure.network(operation, error);
    }
  }

  Uri endpoint(String path, {Map<String, String>? queryParameters}) {
    final uri = baseUri.resolve(path);
    return queryParameters == null
        ? uri
        : uri.replace(queryParameters: queryParameters);
  }
}
