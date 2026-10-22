import 'package:flutter/material.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_recovery_mapper.dart';

class JourneyRecoverySection extends StatelessWidget {
  const JourneyRecoverySection({
    required this.sectionLabel,
    required this.failure,
    required this.onBack,
    required this.onRetry,
    this.retryKey,
    super.key,
  });

  final String sectionLabel;
  final ApiFailure failure;
  final VoidCallback onBack;
  final VoidCallback onRetry;
  final Key? retryKey;

  @override
  Widget build(BuildContext context) {
    final recovery = JourneyRecoveryMapper.map(failure);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(sectionLabel, style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        Semantics(liveRegion: true, child: Text(recovery.message)),
        if (recovery.canRetry)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(
              key: retryKey,
              onPressed: onRetry,
              child: const Text('Retry'),
            ),
          ),
        if (recovery.canGoBack)
          Align(
            alignment: Alignment.centerLeft,
            child: TextButton(onPressed: onBack, child: const Text('Back')),
          ),
      ],
    );
  }
}
