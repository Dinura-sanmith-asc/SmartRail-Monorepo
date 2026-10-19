import 'package:flutter/material.dart';

class LoadingFeedback extends StatelessWidget {
  const LoadingFeedback({required this.message, super.key});

  final String message;

  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    label: message,
    child: Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        const SizedBox.square(
          dimension: 24,
          child: CircularProgressIndicator(strokeWidth: 2.5),
        ),
        const SizedBox(width: 12),
        ExcludeSemantics(child: Text(message)),
      ],
    ),
  );
}

class EmptyFeedback extends StatelessWidget {
  const EmptyFeedback({required this.title, required this.message, super.key});

  final String title;
  final String message;

  @override
  Widget build(BuildContext context) => _FeedbackContent(
    icon: Icons.inbox_outlined,
    title: title,
    message: message,
  );
}

class ValidationFeedback extends StatelessWidget {
  const ValidationFeedback({required this.message, super.key});

  final String message;

  @override
  Widget build(BuildContext context) => Semantics(
    liveRegion: true,
    child: _FeedbackContent(
      icon: Icons.info_outline,
      title: 'Check your information',
      message: message,
    ),
  );
}

class UnavailableFeedback extends StatelessWidget {
  const UnavailableFeedback({
    required this.message,
    this.onRetry,
    this.onBack,
    super.key,
  });

  final String message;
  final VoidCallback? onRetry;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) => _FeedbackContent(
    icon: Icons.cloud_off_outlined,
    title: 'Information unavailable',
    message: message,
    actions: [
      if (onRetry != null)
        FilledButton.icon(
          onPressed: onRetry,
          icon: const Icon(Icons.refresh),
          label: const Text('Retry'),
        ),
      if (onBack != null)
        OutlinedButton.icon(
          onPressed: onBack,
          icon: const Icon(Icons.arrow_back),
          label: const Text('Back'),
        ),
    ],
  );
}

class AccessDeniedFeedback extends StatelessWidget {
  const AccessDeniedFeedback({
    required this.message,
    required this.onBack,
    super.key,
  });

  final String message;
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) => _FeedbackContent(
    icon: Icons.lock_outline,
    title: 'Access unavailable',
    message: message,
    actions: [
      OutlinedButton.icon(
        onPressed: onBack,
        icon: const Icon(Icons.arrow_back),
        label: const Text('Back'),
      ),
    ],
  );
}

class _FeedbackContent extends StatelessWidget {
  const _FeedbackContent({
    required this.icon,
    required this.title,
    required this.message,
    this.actions = const [],
  });

  final IconData icon;
  final String title;
  final String message;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Semantics(
      container: true,
      liveRegion: true,
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(icon, size: 36, color: theme.colorScheme.primary),
                const SizedBox(height: 12),
                Text(
                  title,
                  style: theme.textTheme.titleMedium,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(message, textAlign: TextAlign.center),
                if (actions.isNotEmpty) ...[
                  const SizedBox(height: 20),
                  Wrap(
                    alignment: WrapAlignment.center,
                    spacing: 12,
                    runSpacing: 12,
                    children: actions,
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
