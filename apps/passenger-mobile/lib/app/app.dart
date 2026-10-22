import 'package:flutter/material.dart';
import 'package:smartrail_passenger/app/app_config.dart';
import 'package:smartrail_passenger/app/router.dart';
import 'package:smartrail_passenger/app/theme/app_theme.dart';

class SmartRailApp extends StatelessWidget {
  const SmartRailApp({required this.config, super.key});

  final AppConfig config;

  @override
  Widget build(BuildContext context) => MaterialApp.router(
    title: 'SmartRail Passenger',
    theme: AppTheme.light(),
    routerConfig: appRouter,
    builder: (context, child) {
      if (!config.isConfigured) {
        return _ConfigurationRequiredScreen(
          message: config.configurationError!,
        );
      }
      return child ?? const SizedBox.shrink();
    },
  );
}

class _ConfigurationRequiredScreen extends StatelessWidget {
  const _ConfigurationRequiredScreen({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('SmartRail Passenger')),
    body: Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 480),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.settings_outlined,
                size: 36,
                color: Theme.of(context).colorScheme.primary,
              ),
              const SizedBox(height: AppSpacing.md),
              Text(
                'Application configuration required',
                style: Theme.of(context).textTheme.titleMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AppSpacing.sm),
              Text(message, textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    ),
  );
}
