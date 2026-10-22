/// Client-side record of when displayed railway data was last retrieved.
///
/// This is not a railway-source update timestamp and must not be presented as
/// one. It stays in memory with the active view state.
class RetrievalMetadata {
  const RetrievalMetadata._({
    required this.lastRetrievedAt,
    required this.isNotRefreshed,
  });

  factory RetrievalMetadata.success(DateTime clientRetrievedAt) =>
      RetrievalMetadata._(
        lastRetrievedAt: clientRetrievedAt,
        isNotRefreshed: false,
      );

  final DateTime lastRetrievedAt;
  final bool isNotRefreshed;

  String? get label => isNotRefreshed ? 'Not refreshed' : null;

  RetrievalMetadata afterFailedRefresh() => RetrievalMetadata._(
    lastRetrievedAt: lastRetrievedAt,
    isNotRefreshed: true,
  );

  RetrievalMetadata afterSuccessfulRefresh(DateTime clientRetrievedAt) =>
      RetrievalMetadata.success(clientRetrievedAt);
}
