# Station Display T030 local seed

This command creates one synthetic Development-only journey for live API/display verification. It is limited by the seeder to `127.0.0.1:5432/smartrail_dev` and uses the existing journey initialization and timetable publication handlers. It does not add an HTTP endpoint or change normal API authorization.

Prerequisite: run the normal local development seeder first so its published `Matara (development sample)` stop exists. Apply reviewed migrations separately if the seed command reports pending migrations.

From the SmartRail repository root, run:

```powershell
.\dev\local-data\Invoke-DevSeed.ps1 -Action StationDisplayT030
```

The seeded operation date is **2026-10-20**. It uses the published Colombo Fort and Galle station IDs and includes the existing development Matara sample as the route's third stop. Scheduled departures are 15:00 Colombo Fort and 17:05 Galle, Asia/Colombo. The data is synthetic local test data and is not an authoritative railway timetable. The command is idempotent for this manifest and refuses conflicting records.

For the Station Display, use `VITE_STATION_ID=10000000-0000-0000-0000-000000000001` for Colombo Fort or `VITE_STATION_ID=10000000-0000-0000-0000-000000000002` for Galle, with `VITE_STATION_DISPLAY_SOURCE=live`. Vite reads these public values when the dev server starts.
