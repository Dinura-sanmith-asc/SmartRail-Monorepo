# Holy-Rice catering MVP

Holy-Rice is a separate ASP.NET Core 10 API with its own PostgreSQL database. SmartRail alone calls it using `X-Provider-Key`; the passenger app calls only SmartRail. The provider owns menu, prices, collection availability, order lines, status and idempotency. SmartRail owns dated journey validation, passenger authorization and a minimal correlation row. No payment clearing is implemented.

## API contract

Every `/api/*` provider request requires `X-Provider-Key`. Responses are JSON; failures use safe HTTP errors.

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/health` | Container liveness |
| GET | `/api/menu` | Catalogue, including availability |
| GET | `/api/menu/{id}` | Menu item detail |
| GET | `/api/collection-points` | Provider supported SmartRail station UUIDs |
| POST | `/api/orders` | Create an order with `Idempotency-Key` |
| GET | `/api/orders/{id}` | Authoritative order status and total |
| POST | `/api/orders/{id}/advance` | Development only: Placed → Preparing → Ready → Collected |

`POST /api/orders` accepts `journeyInstanceId`, `collectionStationId`, and `items` with `menuItemId` and `quantity`. A matching key and payload returns the original order with HTTP 200; a conflicting payload returns 409. The key is unique in PostgreSQL, including under concurrent requests. Unit price and total are calculated from current provider menu data, never accepted from the caller.

SmartRail exposes the approved `GET /api/catering/options?journeyId=`, `POST /api/catering/orders`, and `GET /api/catering/orders/{id}` routes. `GET /api/catering/orders` supplies the requested authenticated order list. Only the latter three require a validated passenger Cognito access token and active PostgreSQL Passenger role. Discovery intersects the provider's supported points with the selected journey's authoritative calling points. The SmartRail order `id` is a correlation UUID; the provider's order UUID stays server side.

## Local startup

From the monorepo root, set a random local key with at least 32 characters in the shell or `services/backend/.env`, then run:

```powershell
$env:HOLY_RICE_API_KEY='<random-local-key-of-at-least-32-characters>'
docker compose -f services/backend/docker-compose.yml up -d holy-rice
```

The provider is at `http://127.0.0.1:5110`, its dedicated database at port `5434`. The default development collection stations are Colombo Fort and Galle, whose UUIDs come from SmartRail's `stations-v1.json` test fixture. Override `HOLY_RICE_DEMO_STATIONS` with `uuid|name;uuid|name` values from your local railway seed. Menu and collection points seed only in Development; production requires provider-managed catalogue data and an explicitly applied EF migration.

Use the backend's existing development DB connection string and Cognito settings. Apply the additive correlation migration once, then set these backend environment variables:

```powershell
dotnet ef database update --project services/backend/src/SmartRail.Api/SmartRail.Api.csproj --startup-project services/backend/src/SmartRail.Api/SmartRail.Api.csproj --context CateringDbContext
$env:HolyRice__BaseUrl='http://127.0.0.1:5110'
$env:HolyRice__ApiKey=$env:HOLY_RICE_API_KEY
dotnet run --project services/backend/src/SmartRail.Api/SmartRail.Api.csproj --launch-profile http
```

The provider also runs independently with `HOLY_RICE_DB`, `HOLY_RICE_API_KEY`, and `ASPNETCORE_ENVIRONMENT=Development` supplied to `dotnet run --project services/holy-rice/HolyRice.Api/HolyRice.Api.csproj`. In Development it applies its own migrations and seeds its own data. SmartRail starts independently if the provider is down; catering routes then return 503 without affecting railway routes.

The passenger app needs `SMART_RAIL_API_BASE_URL`, `SMART_RAIL_COGNITO_ISSUER`, `SMART_RAIL_COGNITO_CLIENT_ID`, and `SMART_RAIL_COGNITO_REDIRECT_URI` as Flutter `--dart-define` values. Register `smartrailpassenger://callback` with the existing Cognito public app client and enable authorization code with PKCE. Its `client_id` must be permitted by backend Cognito settings. For example:

```powershell
flutter run --dart-define=SMART_RAIL_API_BASE_URL=http://10.0.2.2:5236 --dart-define=SMART_RAIL_COGNITO_ISSUER=<issuer> --dart-define=SMART_RAIL_COGNITO_CLIENT_ID=<public-client-id> --dart-define=SMART_RAIL_COGNITO_REDIRECT_URI=smartrailpassenger://callback
```

Run that command from `apps/passenger-mobile`. The Android emulator uses `10.0.2.2` for the host backend; use the appropriate host address on other devices. The existing SmartRail manual token issuer is for API smoke tests and does not implement the interactive authorization code flow needed by Flutter AppAuth.

## Verification

```powershell
dotnet test services/holy-rice/tests/HolyRice.Tests/HolyRice.Tests.csproj
dotnet test services/backend/tests/SmartRail.UnitTests/SmartRail.UnitTests.csproj -c Release
# Set SMART_RAIL_TEST_ADMIN_CONNECTION to the dedicated local Compose test DB on port 5433.
dotnet test services/backend/tests/SmartRail.IntegrationTests/SmartRail.IntegrationTests.csproj -c Release
cd apps/passenger-mobile
flutter analyze
flutter test
```

Provider tests create and drop uniquely named databases on the dedicated port 5433 test server. They cover menu, collection points, authentication, server pricing, lifecycle, validation, idempotent retry, conflict and concurrency. Do not point them at development or production databases.

## Design and tasks

1. **SR-074/075/078:** Provider catalogue and point APIs; SmartRail intersects points with published dated journey stops.
2. **SR-077/080/081:** Passenger authorized create; SmartRail writes a correlation reservation before forwarding with a stable derived provider key; provider unique key and persisted request hash prevent duplicate orders.
3. **SR-079:** SmartRail retrieves live provider status for order details and list. `Placed`, `Preparing`, `Ready`, and `Collected` are provider values.
4. **SR-087/088:** No payment entities or clearing. Provider errors and uncertain submissions produce a retryable 503; Flutter retains the request's idempotency key while retrying.
5. **Security:** Cognito and PostgreSQL role checks remain in SmartRail. Provider key is supplied by environment. Passenger subject and access token never enter provider orders. Provider order IDs are not exposed to Flutter.

The backend correlation table migration is `InitialCateringCorrelation`; the provider schema migration is `InitialHolyRice`. Both are additive. Merchant of record, payment collection, liability and cancellation policy remain outside the approved MVP.
