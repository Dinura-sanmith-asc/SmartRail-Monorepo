import 'package:flutter/material.dart';
import 'package:smartrail_passenger/app/theme/app_theme.dart';

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
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Row(
          children: [
            Icon(Icons.train_rounded, color: Color(0xFF126C73)),
            SizedBox(width: 10),
            Text('SmartRail'),
          ],
        ),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 14, 20, 28),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 580),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'GOOD TO GO',
                    style: theme.textTheme.labelMedium?.copyWith(
                      color: theme.colorScheme.primary,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 2,
                    ),
                  ),
                  const SizedBox(height: 7),
                  Text(
                    'Where to next?',
                    style: theme.textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                      color: const Color(0xFF123039),
                    ),
                  ),
                  const SizedBox(height: 18),
                  Container(
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: [Color(0xFF153F4B), Color(0xFF126C73)],
                      ),
                      borderRadius: BorderRadius.circular(28),
                    ),
                    child: Stack(
                      children: [
                        Positioned(
                          right: -35,
                          top: -35,
                          child: Icon(
                            Icons.route_rounded,
                            size: 195,
                            color: Colors.white.withValues(alpha: 0.09),
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.all(25),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Icon(
                                Icons.directions_railway_filled_rounded,
                                color: Color(0xFFFFD59E),
                                size: 35,
                              ),
                              const SizedBox(height: 18),
                              const Text(
                                'Your next journey starts here',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 25,
                                  height: 1.12,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              const SizedBox(height: 9),
                              const Text(
                                'Find a train, follow its journey, and plan the little things along the way.',
                                style: TextStyle(
                                  color: Color(0xFFD7EFED),
                                  height: 1.45,
                                ),
                              ),
                              const SizedBox(height: 22),
                              FilledButton.icon(
                                key: const Key('home-search-action'),
                                onPressed: onSearch,
                                style: FilledButton.styleFrom(
                                  backgroundColor: Colors.white,
                                  foregroundColor: const Color(0xFF14535D),
                                ),
                                icon: const Icon(Icons.search_rounded),
                                label: const Text('Find trains'),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: AppSpacing.xl),
                  Text(
                    'Your travel tools',
                    style: theme.textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 5),
                  Text(
                    'Everything you need for a smoother trip.',
                    style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
                  ),
                  const SizedBox(height: 14),
                  Card(
                    margin: EdgeInsets.zero,
                    child: ListTile(
                      key: const Key('home-journey-status-action'),
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 18,
                        vertical: 12,
                      ),
                      leading: const _ToolIcon(Icons.near_me_rounded),
                      title: const Text(
                        'Journey status',
                        style: TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: const Text(
                        'See live updates for a dated journey.',
                      ),
                      trailing: const Icon(Icons.arrow_forward_rounded),
                      onTap: onJourneyStatus,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Card(
                    margin: EdgeInsets.zero,
                    child: ListTile(
                      contentPadding: const EdgeInsets.symmetric(
                        horizontal: 18,
                        vertical: 12,
                      ),
                      leading: const _ToolIcon(Icons.restaurant_rounded),
                      title: const Text(
                        'Food for the ride',
                        style: TextStyle(fontWeight: FontWeight.w700),
                      ),
                      subtitle: const Text(
                        'Pick a journey, then explore Holy-Rice.',
                      ),
                      trailing: const Icon(Icons.arrow_forward_rounded),
                      onTap: onSearch,
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

class _ToolIcon extends StatelessWidget {
  const _ToolIcon(this.icon);
  final IconData icon;
  @override
  Widget build(BuildContext context) => Container(
    width: 48,
    height: 48,
    decoration: BoxDecoration(
      color: Theme.of(context).colorScheme.surfaceContainerLow,
      borderRadius: BorderRadius.circular(15),
    ),
    child: Icon(icon, color: Theme.of(context).colorScheme.primary),
  );
}
