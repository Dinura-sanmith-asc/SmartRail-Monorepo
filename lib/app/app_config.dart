/// Environment-specific configuration for the public Railway Network API.
///
/// Tests and embedding code can use [fromValue] to supply configuration without
/// depending on a process-level backend or compile-time define.
class AppConfig {
  const AppConfig._({
    required this.apiBaseUri,
    required this.configurationError,
  });

  static const String _environmentApiBaseUrl = String.fromEnvironment(
    'SMART_RAIL_API_BASE_URL',
  );

  final Uri? apiBaseUri;
  final String? configurationError;

  bool get isConfigured => apiBaseUri != null;

  factory AppConfig.fromEnvironment() =>
      AppConfig.fromValue(_environmentApiBaseUrl);

  /// Builds a validated configuration from an explicit value.
  ///
  /// An absent or invalid value stays unconfigured. It never falls back to a
  /// production host or a localhost address.
  factory AppConfig.fromValue(String? value) {
    final candidate = value?.trim();
    if (candidate == null || candidate.isEmpty) {
      return const AppConfig._(
        apiBaseUri: null,
        configurationError:
            'Set SMART_RAIL_API_BASE_URL to configure railway information.',
      );
    }

    final uri = Uri.tryParse(candidate);
    final isValid =
        uri != null &&
        uri.hasAuthority &&
        uri.host.isNotEmpty &&
        (uri.scheme == 'http' || uri.scheme == 'https') &&
        uri.userInfo.isEmpty &&
        !uri.hasQuery &&
        !uri.hasFragment;

    if (!isValid) {
      return const AppConfig._(
        apiBaseUri: null,
        configurationError: 'SMART_RAIL_API_BASE_URL must be an absolute HTTP or HTTPS URL without credentials, query, or fragment.',
      );
    }

    return AppConfig._(apiBaseUri: uri, configurationError: null);
  }
}
