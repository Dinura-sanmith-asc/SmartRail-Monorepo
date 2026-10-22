import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_passenger/features/railway_search/application/retrieval_metadata.dart';

void main() {
  group('RetrievalMetadata', () {
    final retrievedAt = DateTime.utc(2026, 10, 19, 3, 15);

    test('successful retrieval records client time as refreshed', () {
      final metadata = RetrievalMetadata.success(retrievedAt);

      expect(metadata.lastRetrievedAt, retrievedAt);
      expect(metadata.isNotRefreshed, isFalse);
      expect(metadata.label, isNull);
    });

    test('failed refresh preserves retrieval time and marks data not refreshed', () {
      final metadata = RetrievalMetadata.success(retrievedAt).afterFailedRefresh();

      expect(metadata.lastRetrievedAt, retrievedAt);
      expect(metadata.isNotRefreshed, isTrue);
      expect(metadata.label, 'Not refreshed');
    });

    test('new successful retrieval replaces time and clears stale marker', () {
      final nextTime = DateTime.utc(2026, 10, 19, 3, 30);
      final metadata = RetrievalMetadata.success(retrievedAt)
          .afterFailedRefresh()
          .afterSuccessfulRefresh(nextTime);

      expect(metadata.lastRetrievedAt, nextTime);
      expect(metadata.isNotRefreshed, isFalse);
      expect(metadata.label, isNull);
    });

    test('does not invent a source update time', () {
      final metadata = RetrievalMetadata.success(retrievedAt);

      expect(metadata.toString(), isNot(contains('source')));
      expect(metadata.toString(), isNot(contains('updatedAt')));
    });
  });
}
