# SmartRail Driver Mobile

Standalone Flutter client for the approved Driver Operations MVP. The app uses the existing backend as its authorization authority and adds no backend routes.

## Backend routes used

- `GET /api/me` — validated subject and active role labels. The app requires `Driver`; token group claims are ignored.
- `GET /api/driver/assignments` — current assignments, each with a genuine Feature 003 journey snapshot and published updates. The snapshot supplies the app's journey detail; there is no separate protected journey-detail route.
- `POST /api/driver/journeys/{id}/observations` — body is exactly `{ "requestId": "<uuid>", "text": "<trimmed plain text>" }`. The returned server-authored observation reference, author, UTC receipt time and journey revision are parsed for confirmation.

The third approved Feature 005 route, `GET /api/operations/journeys/{id}/observations`, is for Controller review and is intentionally not called by this Driver app. Protected requests use `Authorization: Bearer <Cognito access token>`; the backend validates the token and current PostgreSQL role/assignment evidence. It does not authorize based on Cognito groups.

## Configure and run

The local, Git-ignored `config/dev.json` contains the issuer and public app client already configured for the SmartRail API and Operations Web. The app client must support Authorization Code + PKCE without a client secret. Register `smartraildriver://auth/callback` as an allowed callback URL on Cognito app client `3i20qp5j006fdfr3pbd9bfaj5r` in user pool `ap-southeast-2_6rIjW3fm4`. Keep the existing web callback URL. As of 2026-10-20, Cognito returned its error page for the mobile callback, so this AWS setting still needs to be saved before mobile sign-in can complete.

Android emulator, with the Aspire API listening on the host at port 5237 (the emulator reaches host localhost through `10.0.2.2`):

In VS Code, select **SmartRail Driver (local emulator)** in Run and Debug. Its launch configuration passes `config/dev.json` automatically. A plain Run/Debug launch without these Dart defines displays the "Sign-in is not configured" message.

```powershell
flutter run --dart-define-from-file=config/dev.json
```

The debug Android manifest permits cleartext HTTP only for local development. Use HTTPS for a physical device or deployed environment. For a physical device, set `API_BASE_URL` to the backend's reachable HTTPS address (or a trusted development tunnel); `localhost` on the phone points to the phone. Register the callback URI above on the Cognito app client and provide an HTTPS issuer URL with working OIDC discovery metadata.

The supplied Cognito user ID is deliberately absent from app configuration. Sign in as that account and compare the Profile subject with the authenticated `GET /api/me.subject`; `/api/me.roles` must include `Driver`. SmartRail's PostgreSQL role mapping and current journey assignment remain separate prerequisites for duties to appear.

Build a debug APK with the same defines:

```powershell
flutter build apk --debug --dart-define-from-file=config/dev.json
```

The app stores tokens and its last successful assignment projection in `flutter_secure_storage`; saved duties are labeled stale when refresh fails. It does not queue observations offline. Access token expiry triggers refresh-token exchange where a refresh token is available; a rejected refresh or unrecoverable 401 clears the session. Logout clears all locally stored session and assignment data.

## MVP coverage and limits

Includes secure sign-in entry, PKCE browser sign-in, session restore/refresh/sign-out, backend role check, dashboard, assignment list/detail, station stops and planned/operational times, official status and published update history, observation form with length/blank validation and idempotent retries, retry/error/empty/denied/offline states, and visibly stale cached assignments. No GPS, ETA prediction, signalling, dispatch, braking, door control, or status publication is implemented.

Approved Driver Must Haves implemented here include SR-064, SR-065, SR-066, SR-069, SR-072, SR-093, SR-109 and SR-114. SR-071 and operational notices are Should Have and are not promoted into this MVP.

## Verification

```powershell
flutter pub get
dart format lib test
flutter analyze
flutter test
flutter build apk --debug
```

Android build requires accepted Android SDK licenses. Live Cognito and backend verification requires the deployment's real issuer, public app client, backend URL and a provisioned Driver account; no authentication bypass is included.
