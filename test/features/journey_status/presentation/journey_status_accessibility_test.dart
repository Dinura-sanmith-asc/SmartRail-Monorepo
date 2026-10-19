import 'dart:async';
import 'dart:ui' show SemanticsAction, Tristate;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/core/network/api_failure.dart';
import 'package:smartrail_passenger/core/network/problem_details_parser.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_notifier.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_lookup_state.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_status_providers.dart';
import 'package:smartrail_passenger/features/journey_status/application/journey_update_alerts.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_models.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_status_repository.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_timestamp.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_candidates_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_lookup_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_recovery_section.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_status_screen.dart';
import 'package:smartrail_passenger/features/journey_status/presentation/journey_update_alert.dart';
import 'package:smartrail_passenger/features/railway_search/domain/railway_models.dart';

const _journeyId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1';
const _serviceId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1';
const _originId = 'cccccccc-cccc-4ccc-8ccc-ccccccccccc1';
const _destinationId = 'dddddddd-dddd-4ddd-8ddd-ddddddddddd1';
const _sentinels = [
  'SENTINEL_PRIVATE_TOKEN_123',
  'SENTINEL_INTERNAL_ACTOR_456',
  'SENTINEL_TRACE_SECRET_789',
];

void main() {
  testWidgets(
    'SR-103 SHOULD HAVE: candidate choices have distinct accessible names and expose the tapped selection',
    (tester) async {
      final semantics = tester.ensureSemantics();
      try {
        final candidates = [
          _candidate('11111111-1111-4111-8111-111111111111'),
          _candidate('22222222-2222-4222-8222-222222222222'),
        ];
        JourneyCandidate? selected;
        final request = _lookupRequest();

        await tester.pumpWidget(
          ProviderScope(
            overrides: [
              journeyLookupNotifierProvider(
                request,
              ).overrideWith(() => _ReadyLookupNotifier(request, candidates)),
            ],
            child: MaterialApp(
              home: Scaffold(
                body: SizedBox(
                  height: 480,
                  child: JourneyCandidatesScreen(
                    request: request,
                    onJourneySelected: (candidate) => selected = candidate,
                  ),
                ),
              ),
            ),
          ),
        );

        final first = tester.getSemantics(
          find.byKey(const ValueKey('journey-option-1')),
        );
        final second = tester.getSemantics(
          find.byKey(const ValueKey('journey-option-2')),
        );
        expect(first.label, contains('Journey option 1 of 2'));
        expect(second.label, contains('Journey option 2 of 2'));
        expect(first.label, contains('Journey identity is ambiguous'));
        expect(second.label, contains('Journey identity is ambiguous'));
        expect(first.flagsCollection.isButton, isTrue);
        expect(second.flagsCollection.isButton, isTrue);
        expect(first.getSemanticsData().hasAction(SemanticsAction.tap), isTrue);
        expect(
          second.getSemanticsData().hasAction(SemanticsAction.tap),
          isTrue,
        );
        expect(first.flagsCollection.isSelected, Tristate.isFalse);
        expect(second.flagsCollection.isSelected, Tristate.isFalse);

        await tester.tap(find.byKey(const ValueKey('journey-option-2')));
        await tester.pump();

        expect(selected?.journeyInstanceId, candidates[1].journeyInstanceId);
        expect(
          tester
              .getSemantics(find.byKey(const ValueKey('journey-option-2')))
              .flagsCollection
              .isSelected,
          Tristate.isTrue,
        );
        expect(
          tester
              .getSemantics(find.byKey(const ValueKey('journey-option-1')))
              .flagsCollection
              .isSelected,
          Tristate.isFalse,
        );
      } finally {
        semantics.dispose();
      }
    },
  );

  testWidgets(
    'SR-103 SHOULD HAVE: candidate rows follow a usable keyboard focus order',
    (tester) async {
      final request = _lookupRequest();
      final candidates = [
        _candidate('11111111-1111-4111-8111-111111111111'),
        _candidate('22222222-2222-4222-8222-222222222222'),
      ];
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            journeyLookupNotifierProvider(request)
                .overrideWith(() => _ReadyLookupNotifier(request, candidates)),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: SizedBox(
                height: 480,
                child: JourneyCandidatesScreen(
                  request: request,
                  onJourneySelected: (_) {},
                ),
              ),
            ),
          ),
        ),
      );

      for (var index = 1; index <= candidates.length; index++) {
        final option = find.byKey(ValueKey('journey-option-$index'));
        var focused = false;
        for (var attempt = 0; attempt < 8 && !focused; attempt++) {
          await tester.sendKeyEvent(LogicalKeyboardKey.tab);
          await tester.pump();
          final primaryContext = FocusManager.instance.primaryFocus?.context;
          if (primaryContext != null) {
            focused = find
                .ancestor(
                  of: find.byElementPredicate(
                    (element) => identical(element, primaryContext),
                  ),
                  matching: option,
                )
                .evaluate()
                .isNotEmpty;
          }
        }
        expect(
          focused,
          isTrue,
          reason: 'Journey option $index should follow the visible row order.',
        );
      }
    },
  );

  testWidgets(
    'SR-102: each official status is readable as text and announced without relying on color',
    (tester) async {
      final semantics = tester.ensureSemantics();
      try {
        for (final status in JourneyStatus.values) {
          await tester.pumpWidget(
            ProviderScope(
              key: ValueKey(status),
              overrides: [
                journeyStatusRepositoryProvider.overrideWithValue(
                  _StatusRepository(status),
                ),
              ],
              child: MaterialApp(
                home: JourneyStatusScreen(journeyInstanceId: _journeyId),
              ),
            ),
          );
          await tester.pump();
          await tester.pump(const Duration(milliseconds: 100));

          expect(find.text(status.apiValue), findsOneWidget);
          final statusNode = tester.getSemantics(
            find.byKey(const ValueKey('journey-status-value')),
          );
          expect(statusNode.label, contains(status.apiValue));
          expect(
            statusNode.flagsCollection.isLiveRegion,
            isTrue,
            reason:
                'Status changes should be announced to assistive technology.',
          );
        }
      } finally {
        semantics.dispose();
      }
    },
  );

  testWidgets('loading journey status uses a meaningful live announcement', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    try {
      final repository = _PendingStatusRepository();

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            journeyStatusRepositoryProvider.overrideWithValue(repository),
          ],
          child: const MaterialApp(
            home: JourneyStatusScreen(journeyInstanceId: _journeyId),
          ),
        ),
      );
      await tester.pump();
      await tester.pump();

      expect(find.bySemanticsLabel('Loading journey status'), findsOneWidget);
      expect(
        tester
            .getSemantics(find.bySemanticsLabel('Loading journey status'))
            .flagsCollection
            .isLiveRegion,
        isTrue,
      );

      repository.complete();
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 100));
    } finally {
      semantics.dispose();
    }
  });

  testWidgets('in-app change alert has a live, identifying announcement', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    try {
      final update = JourneyUpdate(
        updateId: 'ffffffff-ffff-4fff-8fff-fffffffffff1',
        journeyInstanceId: _journeyId,
        revision: 8,
        kind: JourneyUpdateKind.status,
        status: JourneyStatus.cancelled,
        source: JourneyOperationalSource.approvedRailwayFeed,
        publishedAt: JourneyTimestamp.parse('2026-10-19T09:00:00+05:30'),
        publicChange: const JourneyStatusChange(JourneyStatus.cancelled),
      );
      final alert = JourneyUpdateAlerts.classify(update)!;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(body: JourneyUpdateAlertBanner(alert: alert)),
        ),
      );

      final node = tester.getSemantics(find.byType(JourneyUpdateAlertBanner));
      expect(node.flagsCollection.isLiveRegion, isTrue);
      expect(node.label, contains('Journey cancelled'));
      expect(node.label, contains('Revision 8'));
      expect(node.label, contains(update.updateId));
    } finally {
      semantics.dispose();
    }
  });

  testWidgets('error feedback redacts sentinels and exposes Retry and Back', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    try {
      var retryCount = 0;
      var backCount = 0;
      final failure = ApiFailure.fromStatus(
        statusCode: 503,
        operation: RailwayApiOperation.journeyUpdates,
        problem: ProblemDetails(
          type: _sentinels[0],
          title: _sentinels[1],
          status: 503,
          detail: _sentinels[2],
          traceId: _sentinels[0],
        ),
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: JourneyRecoverySection(
              sectionLabel: 'Published updates',
              failure: failure,
              onBack: () => backCount++,
              onRetry: () => retryCount++,
            ),
          ),
        ),
      );

      const safeMessage =
          'Railway information is temporarily unavailable. Please try again.';
      final message = tester.getSemantics(find.text(safeMessage));
      expect(message.flagsCollection.isLiveRegion, isTrue);
      expect(find.text(safeMessage), findsOneWidget);
      for (final sentinel in _sentinels) {
        expect(find.textContaining(sentinel), findsNothing);
        expect(
          find.bySemanticsLabel(RegExp(RegExp.escape(sentinel))),
          findsNothing,
        );
      }

      final retryButton = find.ancestor(
        of: find.text('Retry'),
        matching: find.byType(TextButton),
      );
      final backButton = find.ancestor(
        of: find.text('Back'),
        matching: find.byType(TextButton),
      );
      expect(tester.getSemantics(retryButton).label, 'Retry');
      expect(tester.getSemantics(backButton).label, 'Back');
      await tester.tap(retryButton);
      await tester.tap(backButton);
      expect(retryCount, 1);
      expect(backCount, 1);
    } finally {
      semantics.dispose();
    }
  });

  testWidgets('Back remains named and operable while journey lookup loads', (
    tester,
  ) async {
    final semantics = tester.ensureSemantics();
    try {
      final repository = _PendingDeparturesRepository();
      final request = _lookupRequest();

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            journeyStatusRepositoryProvider.overrideWithValue(repository),
          ],
          child: MaterialApp(
            home: Builder(
              builder: (context) => Scaffold(
                body: TextButton(
                  onPressed: () => Navigator.of(context).push(
                    MaterialPageRoute<void>(
                      builder: (_) => JourneyLookupScreen(
                        request: request,
                        onJourneySelected: (_) {},
                      ),
                    ),
                  ),
                  child: const Text('Open journey lookup'),
                ),
              ),
            ),
          ),
        ),
      );
      await tester.tap(find.text('Open journey lookup'));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 400));
      expect(tester.takeException(), isNull);
      expect(find.text('Journey status'), findsOneWidget);

      final back = find.bySemanticsLabel('Back');
      expect(back, findsOneWidget);
      final backNode = tester.getSemantics(back);
      expect(backNode.label, 'Back');
      expect(
        backNode.getSemanticsData().hasAction(SemanticsAction.tap),
        isTrue,
      );
      await tester.tap(back);
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.text('Open journey lookup'), findsOneWidget);

      repository.complete();
    } finally {
      semantics.dispose();
    }
  });

  testWidgets(
    'SR-104 SHOULD HAVE: status and refresh remain usable at enlarged text scale',
    (tester) async {
      final semantics = tester.ensureSemantics();
      try {
        final repository = _StatusRepository(JourneyStatus.delayed);
        await tester.pumpWidget(
          ProviderScope(
            overrides: [
              journeyStatusRepositoryProvider.overrideWithValue(repository),
            ],
            child: MaterialApp(
              home: MediaQuery(
                data: const MediaQueryData(textScaler: TextScaler.linear(2)),
                child: const JourneyStatusScreen(journeyInstanceId: _journeyId),
              ),
            ),
          ),
        );
        await tester.pump();
        await tester.pump(const Duration(milliseconds: 100));

        expect(find.text('Delayed'), findsOneWidget);
        final refresh = find.byTooltip('Refresh journey information');
        expect(refresh, findsOneWidget);
        expect(
          tester
              .getSemantics(refresh)
              .getSemanticsData()
              .hasAction(SemanticsAction.tap),
          isTrue,
        );
        expect(tester.takeException(), isNull);
        await tester.tap(refresh);
        await tester.pumpAndSettle();
        expect(repository.snapshotReads, 2);
        expect(repository.updateReads, 2);
      } finally {
        semantics.dispose();
      }
    },
  );
}

JourneyLookupRequest _lookupRequest() => JourneyLookupRequest(
  choice: ScheduledServiceChoice(
    serviceId: _serviceId,
    serviceCode: 'SR1',
    routeId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
    routeCode: 'R1',
    scheduleRevision: 1,
    origin: ScheduledCall(
      stationId: _originId,
      stationName: 'Central Station',
      stopOrder: 1,
      scheduledAt: '2026-10-19T08:30:00+05:30',
    ),
    destination: const ScheduledCall(
      stationId: _destinationId,
      stationName: 'Harbour Station',
      stopOrder: 2,
      scheduledAt: '2026-10-19T10:30:00+05:30',
    ),
  ),
  travelDate: TravelDate.parse('2026-10-19'),
);

JourneyCandidate _candidate(String id) => JourneyCandidate(
  journeyInstanceId: id,
  serviceId: _serviceId,
  operationDate: '2026-10-19',
  stationLocalDepartureDate: '2026-10-19',
  revision: 1,
  status: JourneyStatus.scheduled,
  stationId: _originId,
  stopOrder: 1,
  scheduledDepartureAt: null,
  ambiguity: JourneyAmbiguity.ambiguous,
  stationName: 'Central Station',
);

class _ReadyLookupNotifier extends JourneyLookupNotifier {
  _ReadyLookupNotifier(super.request, this.values);

  final List<JourneyCandidate> values;

  @override
  JourneyLookupState build() =>
      JourneyLookupState(status: JourneyLookupStatus.ready, candidates: values);
}

class _StatusRepository implements JourneyStatusRepository {
  _StatusRepository(this.status);

  final JourneyStatus status;
  int snapshotReads = 0;
  int updateReads = 0;

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => [];

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) async {
    snapshotReads++;
    return JourneyStatusSnapshot(
      journeyInstanceId: _journeyId,
      serviceId: _serviceId,
      scheduleId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
      scheduleRevision: 1,
      operationDate: '2026-10-19',
      firstDepartureOccurrence: '08:30:00',
      revision: 2,
      status: status,
      stops: [
        JourneyStopStatus(
          journeyStopId: 'ffffffff-ffff-4fff-8fff-fffffffffff1',
          stopOrder: 1,
          stationId: _originId,
          stationName: 'Central Station',
          ambiguity: JourneyAmbiguity.none,
          plannedDepartureAt: JourneyTimestamp.parse(
            '2026-10-19T08:30:00+05:30',
          ),
        ),
      ],
      source: JourneyDataSource.scheduledBaseline,
      publishedAt: null,
    );
  }

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) async {
    updateReads++;
    return const [];
  }
}

class _PendingStatusRepository implements JourneyStatusRepository {
  final snapshotCompleter = Completer<JourneyStatusSnapshot>();
  final updatesCompleter = Completer<List<JourneyUpdate>>();

  void complete() {
    if (!snapshotCompleter.isCompleted) {
      snapshotCompleter.complete(
        JourneyStatusSnapshot(
          journeyInstanceId: _journeyId,
          serviceId: _serviceId,
          scheduleId: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1',
          scheduleRevision: 1,
          operationDate: '2026-10-19',
          firstDepartureOccurrence: '08:30:00',
          revision: 1,
          status: JourneyStatus.scheduled,
          stops: const [],
          source: JourneyDataSource.scheduledBaseline,
          publishedAt: null,
        ),
      );
    }
    if (!updatesCompleter.isCompleted) updatesCompleter.complete(const []);
  }

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) async => [];

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) => snapshotCompleter.future;

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) => updatesCompleter.future;
}

class _PendingDeparturesRepository implements JourneyStatusRepository {
  final completer = Completer<List<JourneyCandidate>>();

  void complete() {
    if (!completer.isCompleted) completer.complete(const []);
  }

  @override
  Future<List<JourneyCandidate>> getStationDepartures({
    required String stationId,
    required String date,
  }) => completer.future;

  @override
  Future<JourneyStatusSnapshot> getJourneySnapshot({
    required String journeyInstanceId,
  }) => throw UnimplementedError();

  @override
  Future<List<JourneyUpdate>> getJourneyUpdates({
    required String journeyInstanceId,
  }) => throw UnimplementedError();
}
