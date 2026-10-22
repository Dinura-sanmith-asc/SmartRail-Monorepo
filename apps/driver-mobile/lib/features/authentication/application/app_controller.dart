import 'dart:async';
import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../../../core/config/app_config.dart';
import '../../../core/network/api.dart';
import '../../assignments/domain/models.dart';

const storage = FlutterSecureStorage();
final appControllerProvider = NotifierProvider<AppController, AppState>(
  AppController.new,
);

class AppState {
  const AppState({
    this.token,
    this.identity,
    this.assignments = const [],
    this.loading = false,
    this.stale = false,
    this.error,
    this.success,
    this.observation,
    this.authReady = false,
  });
  final String? token, error, success;
  final DriverObservation? observation;
  final DriverIdentity? identity;
  final List<Assignment> assignments;
  final bool loading, stale, authReady;
  bool get signedIn => token != null && identity != null && identity!.isDriver;
  AppState copy({
    String? token,
    DriverIdentity? identity,
    List<Assignment>? assignments,
    bool? loading,
    bool? stale,
    String? error,
    String? success,
    DriverObservation? observation,
    bool? authReady,
    bool clearError = false,
    bool clearSuccess = false,
    bool clearSession = false,
  }) => AppState(
    token: clearSession ? null : token ?? this.token,
    identity: clearSession ? null : identity ?? this.identity,
    assignments: assignments ?? this.assignments,
    loading: loading ?? this.loading,
    stale: stale ?? this.stale,
    error: clearError ? null : error ?? this.error,
    success: clearSuccess ? null : success ?? this.success,
    observation: clearSuccess ? null : observation ?? this.observation,
    authReady: authReady ?? this.authReady,
  );
}

class AppController extends Notifier<AppState> {
  static const _auth = FlutterAppAuth();
  late Dio _dio;
  Future<String?>? _refreshing;
  RailApi get _api => RailApi(_dio);
  @override
  AppState build() {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: const Duration(seconds: 10),
        receiveTimeout: const Duration(seconds: 15),
        sendTimeout: const Duration(seconds: 15),
        headers: {'Accept': 'application/json'},
      ),
    );
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (o, h) {
          final t = state.token;
          if (t != null) o.headers['Authorization'] = 'Bearer $t';
          h.next(o);
        },
        onError: (e, h) async {
          if (e.response?.statusCode == 401 &&
              e.requestOptions.extra['retriedAfterRefresh'] != true) {
            final renewed = await (_refreshing ??= _refresh()).whenComplete(
              () => _refreshing = null,
            );
            if (renewed != null) {
              state = state.copy(token: renewed);
              final request = e.requestOptions
                ..headers['Authorization'] = 'Bearer $renewed'
                ..extra['retriedAfterRefresh'] = true;
              try {
                h.resolve(await _dio.fetch(request));
                return;
              } catch (retryError) {
                if (retryError is DioException &&
                    retryError.response?.statusCode == 401) {
                  await signOut(expired: true);
                }
                h.reject(retryError is DioException ? retryError : e);
                return;
              }
            } else {
              await signOut(expired: true);
            }
          }
          h.next(e);
        },
      ),
    );
    unawaited(restore());
    return const AppState();
  }

  Future<void> restore() async {
    try {
      var access = await storage.read(key: 'access_token');
      final expires =
          int.tryParse((await storage.read(key: 'expires_at')) ?? '') ?? 0;
      if (access == null) {
        state = state.copy(authReady: true);
        return;
      }
      if (DateTime.now().millisecondsSinceEpoch >= expires - 60000) {
        access = await _refresh();
      }
      if (access == null) {
        await signOut();
        return;
      }
      state = state.copy(token: access, authReady: true);
      await _resolveIdentityAndLoad();
    } catch (_) {
      state = state.copy(
        authReady: true,
        error: 'Could not restore your session. Sign in again.',
      );
    }
  }

  Future<void> signIn() async {
    if (!AppConfig.authConfigured) {
      state = state.copy(
        error: 'Sign-in is not configured yet. Set the Cognito issuer and app client ID.',
      );
      return;
    }
    state = state.copy(loading: true, clearError: true);
    try {
      final result = await _auth.authorizeAndExchangeCode(
        AuthorizationTokenRequest(
          AppConfig.clientId,
          AppConfig.redirectUri,
          issuer: AppConfig.issuer,
          scopes: AppConfig.scopes,
          promptValues: const ['login'],
        ),
      );
      final token = result.accessToken;
      if (token == null) throw StateError('No access token was returned.');
      await _storeTokens(
        result.accessToken,
        result.refreshToken,
        result.accessTokenExpirationDateTime,
      );
      state = state.copy(token: token, loading: false, authReady: true);
      await _resolveIdentityAndLoad();
    } on FlutterAppAuthUserCancelledException {
      state = state.copy(loading: false);
    } catch (_) {
      state = state.copy(
        loading: false,
        error: 'Sign-in could not be completed. Check your connection or contact your administrator.',
      );
    }
  }

  Future<void> _storeTokens(
    String? accessToken,
    String? refreshToken,
    DateTime? expiration,
  ) async {
    if (accessToken != null) {
      await storage.write(key: 'access_token', value: accessToken);
    }
    if (refreshToken != null) {
      await storage.write(key: 'refresh_token', value: refreshToken);
    }
    await storage.write(
      key: 'expires_at',
      value: (expiration ?? DateTime.now().add(const Duration(minutes: 45)))
          .millisecondsSinceEpoch
          .toString(),
    );
  }

  Future<String?> _refresh() async {
    final refresh = await storage.read(key: 'refresh_token');
    if (refresh == null || !AppConfig.authConfigured) return null;
    try {
      final result = await _auth.token(
        TokenRequest(
          AppConfig.clientId,
          AppConfig.redirectUri,
          issuer: AppConfig.issuer,
          refreshToken: refresh,
          scopes: AppConfig.scopes,
        ),
      );
      if (result.accessToken == null) return null;
      await _storeTokens(
        result.accessToken,
        result.refreshToken,
        result.accessTokenExpirationDateTime,
      );
      return result.accessToken;
    } catch (_) {
      return null;
    }
  }

  Future<void> _resolveIdentityAndLoad() async {
    try {
      final identity = await _api.me();
      if (!identity.isDriver) {
        await signOut();
        state = state.copy(
          error:
              'Your SmartRail account does not have the current Driver role.',
        );
        return;
      }
      await storage.write(
        key: 'cached_identity',
        value: jsonEncode({
          'subject': identity.subject,
          'roles': identity.roles,
        }),
      );
      state = state.copy(identity: identity, error: null);
      await loadAssignments();
    } on DioException catch (e) {
      if (_isNetworkFailure(e)) {
        final identityJson = await storage.read(key: 'cached_identity');
        final cachedJson = await storage.read(key: 'cached_assignments');
        if (identityJson != null && cachedJson != null) {
          final identity = DriverIdentity.fromJson(
            jsonDecode(identityJson) as Map<String, dynamic>,
          );
          if (identity.isDriver) {
            final assignments = (jsonDecode(cachedJson) as List)
                .map((e) => Assignment.fromJson(e as Map<String, dynamic>))
                .toList();
            state = state.copy(
              identity: identity,
              assignments: assignments,
              stale: true,
              loading: false,
              error: 'Network unavailable. Saved duties may be out of date.',
            );
            return;
          }
        }
      }
      _handleApiError(e);
    }
  }

  Future<void> loadAssignments() async {
    state = state.copy(loading: true, clearError: true);
    try {
      final values = await _api.assignments();
      final data = jsonEncode(values.map(_assignmentJson).toList());
      await storage.write(key: 'cached_assignments', value: data);
      await storage.write(
        key: 'cached_at',
        value: DateTime.now().toUtc().toIso8601String(),
      );
      state = state.copy(
        assignments: values,
        loading: false,
        stale: false,
        clearError: true,
      );
    } on DioException catch (e) {
      if (e.response?.statusCode == 403) {
        await storage.delete(key: 'cached_assignments');
        await storage.delete(key: 'cached_identity');
        state = state.copy(
          assignments: const [],
          stale: false,
          loading: false,
          error: _message(e),
        );
        return;
      }
      final cached = await storage.read(key: 'cached_assignments');
      if (cached != null) {
        final parsed = (jsonDecode(cached) as List)
            .map((e) => Assignment.fromJson(e as Map<String, dynamic>))
            .toList();
        state = state.copy(
          assignments: parsed,
          loading: false,
          stale: true,
          error: _message(e),
        );
      } else {
        state = state.copy(loading: false, error: _message(e));
      }
    }
  }

  Future<void> submitObservation(
    String journeyId,
    String requestId,
    String text,
  ) async {
    state = state.copy(loading: true, clearError: true, clearSuccess: true);
    try {
      final observation = await _api.submitObservation(
        journeyId,
        requestId,
        text.trim(),
      );
      state = state.copy(
        loading: false,
        success: 'Observation sent to Control for review. It does not change official journey status.',
        observation: observation,
      );
    } on DioException catch (e) {
      state = state.copy(loading: false, error: _message(e));
    }
  }

  Future<void> signOut({bool expired = false}) async {
    await storage.deleteAll();
    state = AppState(
      authReady: true,
      error: expired ? 'Your session expired. Sign in again.' : null,
    );
  }

  void dismissMessages() =>
      state = state.copy(clearError: true, clearSuccess: true);
  void _handleApiError(DioException e) {
    state = state.copy(loading: false, error: _message(e));
  }

  String _message(DioException e) => switch (e.response?.statusCode) {
    401 => 'Your session expired. Sign in again.',
    403 => 'Access denied for this SmartRail account.',
    404 => 'This journey is no longer available.',
    409 =>
      'This request conflicts with a recent update. Refresh and try again.',
    503 => 'SmartRail is temporarily unavailable. Try again shortly.',
    _
        when e.type == DioExceptionType.connectionError ||
            e.type == DioExceptionType.connectionTimeout ||
            e.type == DioExceptionType.receiveTimeout =>
      'Network unavailable. Showing saved information as stale when available.',
    _ => 'Could not complete the request. Please retry or contact your administrator.',
  };
  bool _isNetworkFailure(DioException e) =>
      e.type == DioExceptionType.connectionError ||
      e.type == DioExceptionType.connectionTimeout ||
      e.type == DioExceptionType.receiveTimeout;
  Map<String, dynamic> _assignmentJson(Assignment a) => {
    'assignmentId': a.id,
    'driverSubject': a.driverSubject,
    'journey': {
      'snapshot': {
        'journeyInstanceId': a.journey.id,
        'serviceId': a.journey.serviceId,
        'scheduleId': a.journey.scheduleId,
        'scheduleRevision': a.journey.scheduleRevision,
        'operationDate': a.journey.operationDate,
        'firstDepartureOccurrence': a.journey.firstDepartureOccurrence,
        'revision': a.journey.revision,
        'status': a.journey.status,
        'source': a.journey.source,
        'publishedAt': a.journey.publishedAt?.toIso8601String(),
        'lastUpdatedAt': a.journey.lastUpdatedAt?.toIso8601String(),
        'eta': a.journey.eta?.toJson(),
        'stops': a.journey.stops
            .map(
              (s) => {
                'journeyStopId': s.id,
                'stopOrder': s.order,
                'stationId': s.stationId,
                'stationName': s.stationName,
                'ambiguity': s.ambiguity,
                'platform': s.platform,
                'timingSource': s.timingSource,
                'timingUpdatedAt': s.timingUpdatedAt?.toIso8601String(),
                'plannedArrivalAt': s.plannedArrivalAt?.toIso8601String(),
                'plannedDepartureAt': s.plannedDepartureAt?.toIso8601String(),
                'operationalArrivalAt': s.operationalArrivalAt
                    ?.toIso8601String(),
                'operationalDepartureAt': s.operationalDepartureAt
                    ?.toIso8601String(),
              },
            )
            .toList(),
      },
      'updates': a.updates
          .map(
            (u) => {
              'updateId': u.updateId,
              'journeyInstanceId': u.journeyInstanceId,
              'revision': u.revision,
              'kind': u.kind,
              'status': u.status,
              'source': u.source,
              'publishedAt': u.publishedAt.toIso8601String(),
              'correctionOfUpdateId': u.correctionOfUpdateId,
              'publicChange': u.publicChange.toJson(),
            },
          )
          .toList(),
    },
  };
}
