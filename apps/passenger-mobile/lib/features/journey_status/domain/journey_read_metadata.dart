/// Client-side retrieval time for one independently read railway section.
/// It is intentionally separate from backend publication timestamps.
class JourneyReadMetadata {
  const JourneyReadMetadata({
    required this.lastRetrievedAt,
    this.notRefreshed = false,
  });

  final DateTime lastRetrievedAt;
  final bool notRefreshed;

  factory JourneyReadMetadata.success(DateTime retrievedAt) =>
      JourneyReadMetadata(lastRetrievedAt: retrievedAt);

  JourneyReadMetadata get afterFailure =>
      JourneyReadMetadata(lastRetrievedAt: lastRetrievedAt, notRefreshed: true);

  JourneyReadMetadata afterSuccess(DateTime retrievedAt) =>
      JourneyReadMetadata.success(retrievedAt);
}
