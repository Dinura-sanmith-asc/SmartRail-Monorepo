import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/journey_status/domain/journey_read_metadata.dart';

void main() {
  test('records client retrieval time and stale marker independently', () {
    final fetchedAt = DateTime.utc(2026, 10, 19, 4, 0);
    final initial = JourneyReadMetadata.success(fetchedAt);
    expect(initial.lastRetrievedAt, fetchedAt);
    expect(initial.notRefreshed, isFalse);
    final failed = initial.afterFailure;
    expect(failed.notRefreshed, isTrue);
    expect(failed.lastRetrievedAt, fetchedAt);
    final recovered = failed.afterSuccess(DateTime.utc(2026, 10, 19, 5));
    expect(recovered.notRefreshed, isFalse);
    expect(recovered.lastRetrievedAt, DateTime.utc(2026, 10, 19, 5));
  });
}
