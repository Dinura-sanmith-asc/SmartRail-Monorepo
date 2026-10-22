import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Persistent passenger navigation shared by the app's top-level destinations.
class PassengerNavigationShell extends StatelessWidget {
  const PassengerNavigationShell({
    required this.location,
    required this.child,
    super.key,
  });

  final String location;
  final Widget child;

  int get _selectedIndex {
    if (location == '/home') return 0;
    if (location == '/my-journey' || location.startsWith('/journeys/')) {
      return 2;
    }
    if (location == '/services') return 3;
    return 1;
  }

  static const _destinations = <String>[
    '/home',
    '/railway/search',
    '/my-journey',
    '/services',
  ];

  @override
  Widget build(BuildContext context) => Scaffold(
    body: child,
    bottomNavigationBar: NavigationBar(
      selectedIndex: _selectedIndex,
      onDestinationSelected: (index) => context.go(_destinations[index]),
      destinations: const [
        NavigationDestination(
          key: ValueKey('passenger-tab-home'),
          icon: Icon(Icons.train_outlined),
          selectedIcon: Icon(Icons.train_rounded),
          label: 'Home',
        ),
        NavigationDestination(
          key: ValueKey('passenger-tab-explore'),
          icon: Icon(Icons.explore_outlined),
          selectedIcon: Icon(Icons.explore),
          label: 'Explore',
        ),
        NavigationDestination(
          key: ValueKey('passenger-tab-my-journey'),
          icon: Icon(Icons.calendar_month_outlined),
          selectedIcon: Icon(Icons.calendar_month),
          label: 'My Journey',
        ),
        NavigationDestination(
          key: ValueKey('passenger-tab-services'),
          icon: Icon(Icons.confirmation_number_outlined),
          selectedIcon: Icon(Icons.confirmation_number),
          label: 'Services',
        ),
      ],
    ),
  );
}
