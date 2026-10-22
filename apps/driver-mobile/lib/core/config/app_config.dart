class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5237',
  );
  static const issuer = String.fromEnvironment('COGNITO_ISSUER');
  static const clientId = String.fromEnvironment('COGNITO_CLIENT_ID');
  static const redirectUri = String.fromEnvironment(
    'COGNITO_REDIRECT_URI',
    defaultValue: 'smartraildriver://auth/callback',
  );
  static const scopesValue = String.fromEnvironment(
    'COGNITO_SCOPES',
    defaultValue: 'openid email',
  );
  static List<String> get scopes => scopesValue
      .split(RegExp(r'\s+'))
      .where((scope) => scope.isNotEmpty)
      .toList();
  static bool get authConfigured => issuer.isNotEmpty && clientId.isNotEmpty;
}
