# SmartRail station display

This public screen shows today's arrivals and departures for one published station. It reads the SmartRail API every 30 seconds and shows the last known data if a refresh fails.

## Local development

Start the backend API, then run `npm install` and `npm run dev` from this directory. Vite uses port 5174. The local development configuration points to Colombo Fort (`10000000-0000-0000-0000-000000000001`) and `http://127.0.0.1:5237/api`.

To display another station, copy `.env.example` to `.env.local` and change `VITE_STATION_ID` to the station ID returned by `GET /api/stations`. Restart Vite after changing environment values. `VITE_API_BASE_URL` must include `/api`. The screen never stores credentials in these public variables.

Production builds require `VITE_API_BASE_URL` and `VITE_STATION_ID` for the target station. `VITE_STATION_DISPLAY_SOURCE=live` selects the API in development; fixture data is reserved for explicitly configured demos.

The backend prepares today's published timetable journeys when the public station departure endpoint is read, so the board does not depend on a Controller first opening the operations dashboard. An unpublished station or unavailable API is shown as an error instead of as an empty timetable.
