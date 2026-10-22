import 'dart:convert';

class ProblemDetails {
  ProblemDetails({
    required this.type,
    required this.title,
    required this.status,
    this.detail,
    this.instance,
    this.code,
    this.errors = const {},
    this.traceId,
  });

  final String type;
  final String title;
  final int status;
  final String? detail;
  final String? instance;
  final String? code;
  final Map<String, List<String>> errors;
  final String? traceId;
}

/// Parses RFC 9457 data. Invalid bodies return null so HTTP status handling remains usable.
abstract final class ProblemDetailsParser {
  static ProblemDetails? parse(String source) {
    try {
      final decoded = jsonDecode(source);
      if (decoded is! Map<String, dynamic>) return null;
      final type = decoded['type'];
      final title = decoded['title'];
      final status = decoded['status'];
      if (type is! String || title is! String || status is! int) return null;
      String? optionalString(String key) {
        final value = decoded[key];
        if (value == null) return null;
        if (value is! String) {
          throw const FormatException('Invalid Problem Details field.');
        }
        return value;
      }

      final rawErrors = decoded['errors'];
      final errors = <String, List<String>>{};
      if (rawErrors != null) {
        if (rawErrors is! Map<String, dynamic>) return null;
        for (final entry in rawErrors.entries) {
          if (entry.value is! List ||
              (entry.value as List).any((item) => item is! String)) {
            return null;
          }
          errors[entry.key] = List.unmodifiable(
            (entry.value as List).cast<String>(),
          );
        }
      }
      return ProblemDetails(
        type: type,
        title: title,
        status: status,
        detail: optionalString('detail'),
        instance: optionalString('instance'),
        code: optionalString('code'),
        errors: Map.unmodifiable(errors),
        traceId: optionalString('traceId'),
      );
    } on FormatException {
      return null;
    }
  }
}
