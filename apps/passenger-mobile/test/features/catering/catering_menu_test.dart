import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'package:smartrail_passenger/app/providers.dart';
import 'package:smartrail_passenger/core/network/http_api_client.dart';
import 'package:smartrail_passenger/features/catering/data/catering_api.dart';
import 'package:smartrail_passenger/features/catering/presentation/catering_screens.dart';

class FakeCateringApi extends CateringApi {
  FakeCateringApi()
    : super(
        HttpApiClient(
          baseUri: Uri.parse('http://localhost/'),
          client: http.Client(),
        ),
        http.Client(),
        CateringAuth(const FlutterSecureStorage()),
        const FlutterSecureStorage(),
      );
  int submissions = 0;
  bool fail = false;
  @override
  Future<CateringOptions> options(String journeyId) async {
    if (fail) throw StateError('Provider unavailable');
    return CateringOptions({
      'items': [
        {
          'id': 'item-1',
          'category': 'Rice',
          'name': 'Chicken rice',
          'description': 'Lunch',
          'price': 950,
          'currency': 'LKR',
          'available': true,
        },
        {
          'id': 'item-2',
          'category': 'Rice',
          'name': 'Unavailable meal',
          'description': 'Sold out',
          'price': 700,
          'currency': 'LKR',
          'available': false,
        },
      ],
      'collectionPoints': [
        {
          'stationId': 'station-1',
          'stationName': 'Colombo Fort',
          'instructions': 'Counter',
        },
      ],
    });
  }

  @override
  Future<CateringOrder> create(
    String journeyId,
    String stationId,
    Map<String, int> cart,
    String key,
  ) async {
    submissions++;
    return CateringOrder({
      'id': 'order-12345678',
      'status': 'Placed',
      'collectionStationId': stationId,
      'total': 1900,
      'currency': 'LKR',
      'items': [
        {'name': 'Chicken rice', 'quantity': 2},
      ],
    });
  }
}

void main() {
  testWidgets(
    'menu cart and collection submit once and show provider confirmation',
    (tester) async {
      final api = FakeCateringApi();
      await tester.pumpWidget(
        ProviderScope(
          overrides: [cateringApiProvider.overrideWithValue(api)],
          child: const MaterialApp(
            home: CateringMenuScreen(
              journeyId: 'journey-1',
              journeyLabel: 'SRL-1 · 2026-11-03',
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Chicken rice'), findsOneWidget);
      expect(find.textContaining('Unavailable'), findsWidgets);
      await tester.tap(find.byTooltip('Add Chicken rice'));
      await tester.tap(find.byTooltip('Add Chicken rice'));
      await tester.pump();
      await tester.scrollUntilVisible(
        find.text('Estimated total: 1900.00 LKR'),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('Estimated total: 1900.00 LKR'), findsOneWidget);
      await tester.tap(find.text('Collection station'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Colombo Fort').last);
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.text('Submit order'),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.text('Submit order'));
      await tester.pumpAndSettle();
      expect(api.submissions, 1);
      expect(find.text('Order placed'), findsOneWidget);
      expect(find.text('Total: 1900.00 LKR'), findsOneWidget);
    },
  );

  testWidgets('provider failure shows a retry action', (tester) async {
    final api = FakeCateringApi()..fail = true;
    await tester.pumpWidget(
      ProviderScope(
        overrides: [cateringApiProvider.overrideWithValue(api)],
        child: const MaterialApp(
          home: CateringMenuScreen(journeyId: 'journey-1'),
        ),
      ),
    );
    await tester.pumpAndSettle();
    expect(find.text('Retry'), findsOneWidget);
    api.fail = false;
    await tester.tap(find.text('Retry'));
    await tester.pumpAndSettle();
    expect(find.text('Chicken rice'), findsOneWidget);
  });
}
