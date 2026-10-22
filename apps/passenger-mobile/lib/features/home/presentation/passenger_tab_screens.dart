import 'package:flutter/material.dart';

/// Entry point for the approved journey-status flow. A dated service must be
/// selected before the app can request its published journey status.
class MyJourneyTabScreen extends StatelessWidget {
  const MyJourneyTabScreen({required this.onExplore, super.key});

  final VoidCallback onExplore;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('My Journey')),
    body: _TabLandingContent(
      icon: Icons.route_outlined,
      title: 'View a journey',
      message: 'Search for a scheduled service and select a dated journey to view its published status and updates.',
      buttonLabel: 'Explore services',
      onPressed: onExplore,
    ),
  );
}

/// Services currently means published scheduled railway services. This page
/// links to the existing search and does not imply ticketing or catering.
class ServicesTabScreen extends StatelessWidget {
  const ServicesTabScreen({required this.onSearch, super.key});

  final VoidCallback onSearch;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Services')),
    body: _TabLandingContent(
      icon: Icons.train_outlined,
      title: 'Scheduled railway services',
      message:
          'Find published services by origin, destination, and travel date.',
      buttonLabel: 'Search services',
      onPressed: onSearch,
    ),
  );
}

class _TabLandingContent extends StatelessWidget {
  const _TabLandingContent({
    required this.icon,
    required this.title,
    required this.message,
    required this.buttonLabel,
    required this.onPressed,
  });

  final IconData icon;
  final String title;
  final String message;
  final String buttonLabel;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => Center(
    child: ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 440),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 48, color: Theme.of(context).colorScheme.primary),
            const SizedBox(height: 16),
            Text(
              title,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 24),
            FilledButton.icon(
              onPressed: onPressed,
              icon: const Icon(Icons.search),
              label: Text(buttonLabel),
            ),
          ],
        ),
      ),
    ),
  );
}
