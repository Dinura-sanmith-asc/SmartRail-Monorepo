import 'package:flutter/material.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/widgets/async_feedback.dart';
import 'package:smartrail_passenger/features/railway_search/application/recovery_message_mapper.dart';

/// Renders safe passenger recovery feedback for a single failed read.
class RetrySection extends StatelessWidget {
  const RetrySection({
    required this.failure,
    this.onRetry,
    this.onBack,
    super.key,
  });

  final ApiFailure failure;
  final VoidCallback? onRetry;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    final recovery = RecoveryMessageMapper.map(failure);
    if (failure.kind == ApiFailureKind.accessDenied) {
      return AccessDeniedFeedback(
        message: recovery.message,
        onBack: onBack ?? () {},
      );
    }
    if (failure.kind == ApiFailureKind.validation) {
      return Semantics(
        liveRegion: true,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ValidationFeedback(message: recovery.message),
            for (final fieldMessage in recovery.fieldMessages.values)
              Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: 24,
                  vertical: 2,
                ),
                child: Align(
                  alignment: Alignment.center,
                  child: Text(fieldMessage, textAlign: TextAlign.center),
                ),
              ),
            if (recovery.canGoBack && onBack != null) ...[
              const SizedBox(height: 16),
              OutlinedButton.icon(
                onPressed: onBack,
                icon: const Icon(Icons.arrow_back),
                label: const Text('Back'),
              ),
            ],
          ],
        ),
      );
    }
    return UnavailableFeedback(
      message: recovery.message,
      onRetry: recovery.canRetry ? onRetry : null,
      onBack: recovery.canGoBack ? onBack : null,
    );
  }
}
