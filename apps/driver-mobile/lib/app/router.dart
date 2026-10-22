import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../features/assignments/presentation/assignments_screen.dart';
import '../features/authentication/presentation/home_screen.dart';
import '../features/journeys/presentation/journey_screen.dart';
import '../features/observations/presentation/observation_screen.dart';
import '../features/profile/presentation/profile_screen.dart';

final appRouter = GoRouter(
  initialLocation: '/',
  routes: [
    GoRoute(path: '/', builder: (context, state) => const HomeScreen()),
    GoRoute(
      path: '/assignments',
      builder: (context, state) => const AssignmentsScreen(),
    ),
    GoRoute(
      path: '/journey/:id',
      builder: (context, state) =>
          JourneyScreen(journeyId: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/journey/:id/observation',
      builder: (context, state) =>
          ObservationScreen(journeyId: state.pathParameters['id']!),
    ),
    GoRoute(
      path: '/profile',
      builder: (context, state) => const ProfileScreen(),
    ),
  ],
);

Widget pageShell(
  String title,
  Widget body, {
  List<Widget> actions = const [],
}) => Scaffold(
  appBar: AppBar(title: Text(title), actions: actions),
  body: SafeArea(child: body),
);
