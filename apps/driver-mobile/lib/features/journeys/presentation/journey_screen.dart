import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:go_router/go_router.dart';

import '../../../app/router.dart';
import '../../authentication/application/app_controller.dart';
import '../../authentication/presentation/home_screen.dart';
import '../../assignments/domain/models.dart';

class JourneyScreen extends ConsumerWidget {
  const JourneyScreen({super.key, required this.journeyId});
  final String journeyId;
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final s = ref.watch(appControllerProvider);
    if (!s.signedIn) {
      return const HomeScreen();
    }
    final assignment = s.assignments
        .where((a) => a.journey.id == journeyId)
        .firstOrNull;
    if (assignment == null) {
      return pageShell(
        'Journey',
        const Center(
          child: Text('This journey is not in your current assignments.'),
        ),
      );
    }
    final j = assignment.journey;
    return pageShell(
      'Journey details',
      ListView(
        padding: const EdgeInsets.all(18),
        children: [
          if (s.stale)
            const Card(
              color: Color(0xfffff2d8),
              child: Padding(
                padding: EdgeInsets.all(13),
                child: Text(
                  'Offline · this saved information may be out of date.',
                ),
              ),
            ),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'SERVICE ${j.serviceId.substring(0, 8).toUpperCase()}',
                    style: const TextStyle(
                      color: Color(0xff1769aa),
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.1,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    j.status,
                    style: const TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text('Operating date  ${j.operationDate}'),
                  const SizedBox(height: 4),
                  Text(
                    'Official revision  ${j.revision}',
                    style: TextStyle(color: Colors.blueGrey.shade600),
                  ),
                  if (j.lastUpdatedAt != null)
                    Text(
                      'Last updated  ${DateFormat('d MMM, HH:mm').format(j.lastUpdatedAt!.toLocal())}',
                      style: TextStyle(color: Colors.blueGrey.shade600),
                    ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 18),
          const Text(
            'Scheduled stops',
            style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 10),
          if (j.stops.isEmpty)
            const Card(
              child: Padding(
                padding: EdgeInsets.all(18),
                child: Text('Stop information is not available.'),
              ),
            )
          else
            ...j.stops.map(
              (stop) => _StopTile(stop: stop, isLast: stop == j.stops.last),
            ),
          const SizedBox(height: 18),
          const Text(
            'Recent official updates',
            style: TextStyle(fontSize: 19, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 10),
          if (assignment.updates.isEmpty)
            const Card(
              child: Padding(
                padding: EdgeInsets.all(16),
                child: Text('No operational updates have been published.'),
              ),
            )
          else
            ...assignment.updates
                .take(8)
                .map(
                  (u) => Card(
                    child: ListTile(
                      leading: const Icon(
                        Icons.campaign_outlined,
                        color: Color(0xff1769aa),
                      ),
                      title: Text(u.summary),
                      subtitle: Text(
                        'Official status ${u.status} · revision ${u.revision} · ${DateFormat('d MMM, HH:mm').format(u.publishedAt.toLocal())}',
                      ),
                    ),
                  ),
                ),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: s.stale
                ? null
                : () => context.push('/journey/$journeyId/observation'),
            icon: const Icon(Icons.edit_note),
            label: const Text('Report an observation'),
          ),
          const Padding(
            padding: EdgeInsets.only(top: 9),
            child: Text(
              'Driver observations are evidence for Control review and do not change official journey status.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 14, color: Color(0xff61778a)),
            ),
          ),
        ],
      ),
    );
  }
}

class _StopTile extends StatelessWidget {
  const _StopTile({required this.stop, required this.isLast});
  final JourneyStop stop;
  final bool isLast;
  @override
  Widget build(BuildContext context) {
    String time(DateTime? d) =>
        d == null ? '—' : DateFormat('HH:mm').format(d.toLocal());
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 58,
            child: Padding(
              padding: const EdgeInsets.only(top: 18),
              child: Text(
                '${stop.order}'.padLeft(2, '0'),
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Color(0xff1769aa),
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(0, 15, 14, 15),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    stop.stationName?.isNotEmpty == true
                        ? stop.stationName!
                        : 'Station ${stop.stationId.substring(0, 8)}',
                    style: const TextStyle(
                      fontWeight: FontWeight.w700,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 7),
                  Text(
                    'Scheduled  ${time(stop.plannedArrivalAt)} arr · ${time(stop.plannedDepartureAt)} dep',
                    style: TextStyle(color: Colors.blueGrey.shade700),
                  ),
                  if (stop.operationalArrivalAt != null ||
                      stop.operationalDepartureAt != null)
                    Text(
                      'Operational  ${time(stop.operationalArrivalAt)} arr · ${time(stop.operationalDepartureAt)} dep',
                      style: const TextStyle(
                        color: Color(0xff1769aa),
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  if (stop.platform != null)
                    Text(
                      'Platform ${stop.platform}',
                      style: TextStyle(color: Colors.blueGrey.shade700),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
