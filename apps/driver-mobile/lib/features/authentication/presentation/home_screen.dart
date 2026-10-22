import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../application/app_controller.dart';
import '../../assignments/domain/models.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(appControllerProvider);
    if (!state.authReady ||
        (state.loading && state.token != null && state.identity == null)) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (state.signedIn) {
      return _Dashboard(state: state);
    }
    final children = <Widget>[
      Container(
        width: 62,
        height: 62,
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: .12),
          borderRadius: BorderRadius.circular(18),
        ),
        child: const Icon(Icons.train_rounded, color: Colors.white, size: 34),
      ),
      const SizedBox(height: 42),
      const Text(
        'SMART RAIL',
        style: TextStyle(
          color: Color(0xff90c7f5),
          fontWeight: FontWeight.w700,
          letterSpacing: 2.2,
        ),
      ),
      const SizedBox(height: 12),
      const Text(
        'Your duties,\nclearly in view.',
        style: TextStyle(
          color: Colors.white,
          fontSize: 38,
          height: 1.12,
          fontWeight: FontWeight.w700,
        ),
      ),
      const SizedBox(height: 18),
      Text(
        'Secure access to assigned journeys and current railway information.',
        style: TextStyle(
          color: Colors.white.withValues(alpha: .76),
          fontSize: 16,
          height: 1.5,
        ),
      ),
      const SizedBox(height: 34),
    ];
    if (state.error != null) {
      children.add(
        _Message(
          state.error!,
          onClose: () =>
              ref.read(appControllerProvider.notifier).dismissMessages(),
        ),
      );
    }
    children.add(
      SizedBox(
        width: double.infinity,
        height: 54,
        child: FilledButton.icon(
          onPressed: state.loading
              ? null
              : () => ref.read(appControllerProvider.notifier).signIn(),
          icon: const Icon(Icons.open_in_browser),
          label: Text(state.loading ? 'Connecting…' : 'Sign in securely'),
        ),
      ),
    );
    children.add(const SizedBox(height: 16));
    children.add(
      const Text(
        'Authentication is handled by your organization’s secure sign-in service.',
        style: TextStyle(color: Color(0xffc3d7e9), fontSize: 13, height: 1.45),
      ),
    );
    return Scaffold(
      backgroundColor: const Color(0xff092846),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(28),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 430),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: children,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Dashboard extends ConsumerWidget {
  const _Dashboard({required this.state});
  final AppState state;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final next = state.assignments.isEmpty ? null : state.assignments.first;
    final items = <Widget>[
      if (state.stale) const _StaleBanner(),
      Text(
        'Good day',
        style: TextStyle(color: Colors.blueGrey.shade600, fontSize: 15),
      ),
      const SizedBox(height: 4),
      Text(
        state.identity?.subject ?? 'Driver',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: const TextStyle(
          fontSize: 25,
          fontWeight: FontWeight.w700,
          color: Color(0xff102b46),
        ),
      ),
      const SizedBox(height: 22),
      Container(
        padding: const EdgeInsets.all(22),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xff1769aa), Color(0xff104273)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          borderRadius: BorderRadius.circular(24),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'CURRENT DUTIES',
              style: TextStyle(
                color: Color(0xffc8e3fb),
                fontSize: 12,
                letterSpacing: 1.5,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 12),
            Text(
              '${state.assignments.length}',
              style: const TextStyle(
                color: Colors.white,
                fontSize: 42,
                fontWeight: FontWeight.w700,
              ),
            ),
            Text(
              state.assignments.length == 1
                  ? 'assigned journey'
                  : 'assigned journeys',
              style: const TextStyle(color: Colors.white70),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                const Icon(
                  Icons.verified_outlined,
                  color: Colors.white70,
                  size: 18,
                ),
                const SizedBox(width: 8),
                Text(
                  next?.journey.status ?? 'No active assignment',
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
      const SizedBox(height: 24),
      const _SectionTitle('Next journey'),
    ];
    if (state.loading && state.assignments.isEmpty) {
      items.add(
        const Padding(
          padding: EdgeInsets.all(30),
          child: Center(child: CircularProgressIndicator()),
        ),
      );
    } else if (state.error != null && state.assignments.isEmpty) {
      items.add(
        _ErrorCard(
          message: state.error!,
          retry: () =>
              ref.read(appControllerProvider.notifier).loadAssignments(),
        ),
      );
    } else if (next == null) {
      items.add(const _EmptyCard());
    } else {
      items.add(
        _AssignmentCard(
          assignment: next,
          onTap: () => context.push('/journey/${next.journey.id}'),
        ),
      );
    }
    items.addAll([
      const SizedBox(height: 24),
      const _SectionTitle('Quick access'),
      Row(
        children: [
          Expanded(
            child: _QuickTile(
              icon: Icons.route_rounded,
              title: 'My journeys',
              onTap: () => context.push('/assignments'),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: _QuickTile(
              icon: Icons.person_outline,
              title: 'My profile',
              onTap: () => context.push('/profile'),
            ),
          ),
        ],
      ),
    ]);
    if (state.error != null && state.assignments.isNotEmpty) {
      items.add(
        Padding(
          padding: const EdgeInsets.only(top: 16),
          child: Text(
            state.error!,
            style: const TextStyle(color: Color(0xff91610a)),
          ),
        ),
      );
    }
    return Scaffold(
      appBar: AppBar(
        title: const Text('SmartRail'),
        actions: [
          IconButton(
            onPressed: () => context.push('/profile'),
            icon: const Icon(Icons.account_circle_outlined),
            tooltip: 'Profile',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () =>
            ref.read(appControllerProvider.notifier).loadAssignments(),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
          children: items,
        ),
      ),
    );
  }
}

class _AssignmentCard extends StatelessWidget {
  const _AssignmentCard({required this.assignment, required this.onTap});
  final Assignment assignment;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Card(
    child: InkWell(
      borderRadius: BorderRadius.circular(20),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Service ${assignment.journey.serviceId.substring(0, 8)}',
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 16,
                    ),
                  ),
                ),
                _StatusChip(assignment.journey.status),
              ],
            ),
            const SizedBox(height: 10),
            Text(
              'Operating date  ${assignment.journey.operationDate}',
              style: TextStyle(color: Colors.blueGrey.shade600),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                const Icon(
                  Icons.pin_drop_outlined,
                  size: 18,
                  color: Color(0xff1769aa),
                ),
                const SizedBox(width: 7),
                Expanded(
                  child: Text(
                    '${assignment.journey.stops.length} scheduled stops',
                    style: const TextStyle(fontWeight: FontWeight.w500),
                  ),
                ),
                const Icon(Icons.arrow_forward_ios_rounded, size: 15),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);
  final String text;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: Text(
      text,
      style: const TextStyle(
        fontSize: 18,
        fontWeight: FontWeight.w700,
        color: Color(0xff102b46),
      ),
    ),
  );
}

class _QuickTile extends StatelessWidget {
  const _QuickTile({
    required this.icon,
    required this.title,
    required this.onTap,
  });
  final IconData icon;
  final String title;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Card(
    child: InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(18),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 18, horizontal: 14),
        child: Row(
          children: [
            Icon(icon, color: const Color(0xff1769aa)),
            const SizedBox(width: 10),
            Flexible(
              child: Text(
                title,
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}

class _EmptyCard extends StatelessWidget {
  const _EmptyCard();
  @override
  Widget build(BuildContext context) => const Card(
    child: Padding(
      padding: EdgeInsets.all(22),
      child: Column(
        children: [
          Icon(
            Icons.event_available_outlined,
            size: 30,
            color: Color(0xff5981a3),
          ),
          SizedBox(height: 10),
          Text(
            'No assigned journeys',
            style: TextStyle(fontWeight: FontWeight.w700),
          ),
          SizedBox(height: 5),
          Text(
            'Pull down to check for current duties.',
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
  );
}

class _ErrorCard extends StatelessWidget {
  const _ErrorCard({required this.message, required this.retry});
  final String message;
  final VoidCallback retry;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(18),
      child: Column(
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton.icon(
            onPressed: retry,
            icon: const Icon(Icons.refresh),
            label: const Text('Try again'),
          ),
        ],
      ),
    ),
  );
}

class _StaleBanner extends StatelessWidget {
  const _StaleBanner();
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 18),
    padding: const EdgeInsets.all(13),
    decoration: BoxDecoration(
      color: const Color(0xfffff2d8),
      borderRadius: BorderRadius.circular(14),
    ),
    child: const Row(
      children: [
        Icon(Icons.wifi_off_rounded, color: Color(0xff9c6200)),
        SizedBox(width: 10),
        Expanded(
          child: Text(
            'Offline · showing saved information that may be out of date.',
            style: TextStyle(
              color: Color(0xff714900),
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    ),
  );
}

class _Message extends StatelessWidget {
  const _Message(this.text, {required this.onClose});
  final String text;
  final VoidCallback onClose;
  @override
  Widget build(BuildContext context) => Container(
    margin: const EdgeInsets.only(bottom: 18),
    padding: const EdgeInsets.all(14),
    decoration: BoxDecoration(
      color: const Color(0xffffe9e7),
      borderRadius: BorderRadius.circular(12),
    ),
    child: Row(
      children: [
        Expanded(
          child: Text(text, style: const TextStyle(color: Color(0xff7b211c))),
        ),
        IconButton(onPressed: onClose, icon: const Icon(Icons.close, size: 18)),
      ],
    ),
  );
}

class _StatusChip extends StatelessWidget {
  const _StatusChip(this.status);
  final String status;
  @override
  Widget build(BuildContext context) {
    final color = status == 'Running'
        ? const Color(0xff18794e)
        : status == 'Cancelled' || status == 'Interrupted'
        ? const Color(0xffb42318)
        : const Color(0xff526b83);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        status,
        style: TextStyle(
          color: color,
          fontSize: 12,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}
