import 'package:flutter/material.dart';
import 'package:smartrail_passenger/app/theme/app_theme.dart';

/// Passenger landing screen for scheduled service search and journey status.
class HomeScreen extends StatelessWidget {
  const HomeScreen({
    required this.onSearch,
    required this.onJourneyStatus,
    super.key,
  });

  final VoidCallback onSearch;
  final VoidCallback onJourneyStatus;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            Icon(Icons.train_rounded, color: colors.primary),
            const SizedBox(width: AppSpacing.sm),
            const Text('SmartRail'),
          ],
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Welcome to SmartRail', style: textTheme.headlineSmall),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    'Where are you travelling today?',
                    style: textTheme.bodyLarge?.copyWith(
                      color: colors.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(AppSpacing.md),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          const _SearchHint(
                            icon: Icons.trip_origin,
                            label: 'From (origin)',
                          ),
                          const SizedBox(height: AppSpacing.sm),
                          const _SearchHint(
                            icon: Icons.location_on_outlined,
                            label: 'To (destination)',
                          ),
                          const SizedBox(height: AppSpacing.sm),
                          const _SearchHint(
                            icon: Icons.calendar_month_outlined,
                            label: 'Date of travel',
                          ),
                          const SizedBox(height: AppSpacing.md),
                          FilledButton.icon(
                            key: const Key('home-search-action'),
                            onPressed: onSearch,
                            icon: const Icon(Icons.search),
                            label: const Text('Find trains'),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: AppSpacing.lg),
                  Text('QUICK INQUIRIES', style: textTheme.labelLarge),
                  const SizedBox(height: AppSpacing.sm),
                  Card(
                    child: ListTile(
                      key: const Key('home-journey-status-action'),
                      leading: Icon(
                        Icons.notifications_none_rounded,
                        color: colors.primary,
                      ),
                      title: const Text('Journey status'),
                      subtitle: const Text(
                        'Choose a dated service to view published status and updates.',
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: onJourneyStatus,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.md),
                  Card(
                    color: colors.surfaceContainerLow,
                    child: const Padding(
                      padding: EdgeInsets.all(AppSpacing.md),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Icon(Icons.verified_outlined),
                          SizedBox(width: AppSpacing.sm),
                          Expanded(
                            child: Text(
                              'Search scheduled services by station and date. Published journey status is shown separately when available.',
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _SearchHint extends StatelessWidget {
  const _SearchHint({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: colors.surfaceContainerLow,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(icon, color: colors.primary),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label.toUpperCase(),
                  style: Theme.of(context).textTheme.labelSmall,
                ),
                const Text('Choose a station or date'),
              ],
            ),
          ),
          const Icon(Icons.chevron_right),
        ],
      ),
    );
  }
}
