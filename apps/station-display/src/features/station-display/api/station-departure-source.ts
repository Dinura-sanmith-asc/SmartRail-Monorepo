import type { StationDepartureSnapshot } from '../model/display-model'

/** Internal boundary shared by deterministic fixtures and any future approved live adapter. */
export interface StationDepartureSource {
  getInitialSnapshot(stationId: string): Promise<StationDepartureSnapshot>
  revalidate(stationId: string): Promise<StationDepartureSnapshot>
  subscribe(
    listener: (snapshot: StationDepartureSnapshot) => void,
    refreshHandlers?: {
      onRevalidated?: (snapshot: StationDepartureSnapshot) => void
      onRefreshFailure?: (message: string) => void
    },
  ): () => void
  dispose?(): void
}
