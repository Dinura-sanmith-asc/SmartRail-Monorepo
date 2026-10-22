import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../authentication/application/app_controller.dart';
import '../../authentication/presentation/home_screen.dart';

class AssignmentsScreen extends ConsumerWidget {
  const AssignmentsScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(appControllerProvider);
    if (!state.signedIn) return const HomeScreen();
    final children = <Widget>[];
    if (state.stale) children.add(const _OfflineNotice());
    if (state.error != null && state.assignments.isEmpty) {
      children.add(
        Padding(
          padding: const EdgeInsets.all(30),
          child: Column(
            children: [
              const Icon(Icons.cloud_off_outlined, size: 42),
              const SizedBox(height: 12),
              Text(state.error!, textAlign: TextAlign.center),
              TextButton.icon(
                onPressed: () =>
                    ref.read(appControllerProvider.notifier).loadAssignments(),
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    } else if (state.loading && state.assignments.isEmpty) {
      children.add(
        const Padding(
          padding: EdgeInsets.all(48),
          child: Center(child: CircularProgressIndicator()),
        ),
      );
    } else if (state.assignments.isEmpty) {
      children.add(
        const Padding(
          padding: EdgeInsets.all(28),
          child: Column(
            children: [
              Icon(Icons.event_available_outlined, size: 44),
              SizedBox(height: 12),
              Text(
                'No assigned journeys',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
              ),
              SizedBox(height: 6),
              Text(
                'Your current duties will appear here.',
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    } else {
      children.addAll(
        state.assignments.map(
          (a) => Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 18,
                vertical: 10,
              ),
              title: Text(
                'Service ${a.journey.serviceId.substring(0, 8)}',
                style: const TextStyle(fontWeight: FontWeight.w700),
              ),
              subtitle: Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  '${a.journey.operationDate} · ${a.journey.stops.length} stops',
                ),
              ),
              trailing: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    a.journey.status,
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(height: 5),
                  const Icon(Icons.chevron_right),
                ],
              ),
              onTap: () => context.push('/journey/${a.journey.id}'),
            ),
          ),
        ),
      );
    }
    return pageShell(
      'My journeys',
      RefreshIndicator(
        onRefresh: () =>
            ref.read(appControllerProvider.notifier).loadAssignments(),
        child: ListView(padding: const EdgeInsets.all(18), children: children),
      ),
    );
  }
}

class _OfflineNotice extends StatelessWidget {
  const _OfflineNotice();
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 12),
    padding: const EdgeInsets.all(13),
    decoration: BoxDecoration(
      color: const Color(0xfffff2d8),
      borderRadius: BorderRadius.circular(12),
    ),
    child: const Text(
      'Offline · saved assignment information may be out of date.',
      style: TextStyle(color: Color(0xff714900)),
    ),
  );
}
