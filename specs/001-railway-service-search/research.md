# Research: Railway Search and Service Details

**Date**: 2026-10-18  
**Scope**: Planning decisions for the approved passenger feature. The repository's
`pubspec.yaml` uses Dart `^3.13.2`; installed Flutter is 3.47.2 / Dart 3.13.2. Research
checked current package metadata and official Flutter/Android guidance on 2026-10-18.
Pub.dev metadata lists minimum Dart SDKs of 3.12 for `flutter_riverpod 3.4.3` and
`go_router 18.1.0`, 3.4 for `http 1.6.0`, and 3.9 for `intl 0.20.3`; each is compatible
with the repository's Dart 3.13.2 constraint. Confirm the dependency solver when adding
them because this planning stage does not change `pubspec.lock`.

## Decision: Feature-based Flutter boundaries

**Decision**: Keep the existing single Flutter application. Organize this feature into
presentation, application/state, domain, and data/API folders, with small shared `app/`
and `core/` folders.

**Rationale**: This directly satisfies the passenger constitution and gives the workflow
independently testable repository/state boundaries without a second app package, generated
layers, or a general-purpose architecture framework. The repository currently contains a
single generated counter app and one counter widget test.

**Alternatives considered**: Layer-only app-wide directories can obscure feature ownership;
separate Dart packages or Clean Architecture frameworks add setup without any other feature
in this first slice.

## Decision: Riverpod for state and dependency composition

**Decision**: Propose `flutter_riverpod ^3.4.3`, using hand-written `Notifier`/
`AsyncNotifier` providers, repository providers and provider overrides. Do not adopt
`riverpod_generator` or annotation/code-generation dependencies.

**Rationale**: Search and detail have asynchronous states plus dependent dependencies.
Riverpod exposes loading/error/data patterns, keeps orchestration outside widgets, and
supports container/provider override testing. It also provides the app's dependency
composition boundary, so no separate DI package is needed. Pub metadata shows 3.4.3
published for Flutter; the installed SDK is current enough to evaluate compatibility when
implementation begins. Riverpod's [testing guide](https://riverpod.dev/docs/how_to/testing)
describes isolated containers, provider overrides and widget testing.

**Alternatives considered**: Built-in `ChangeNotifier`/Provider is viable for smaller state,
but async screen states, independent section retry, and overrides across several test types
make Riverpod's explicit providers useful. BLoC adds event/state ceremony for this small
read-only flow. Generator-based Riverpod is deferred to avoid generated files and a build
step.

## Decision: `go_router` for route stack

**Decision**: Propose `go_router ^18.1.0`, configured with the three in-app paths and normal
push/pop behavior. Without `go_router_builder`, treat path/query parameters as parsed values,
not statically typed generated route classes. Use path IDs for details and the current result
summary as in-memory route extra; deep-link restoration is not a feature requirement.

**Rationale**: It is Flutter-published and provides declarative routes with nested/pushed
pages and a familiar back stack. It supports Android and iOS and current Flutter 3.47.2.
The feature needs a clear Search → Results → Details stack with preserved prior state, not
authentication redirects or nested navigators.

**Alternatives considered**: `Navigator` 1.0 is built in and could implement three screens;
`go_router` gives stable named paths and makes route behavior testable with modest setup.
AutoRoute and typed-route generator packages add code generation and are not justified by
three local routes.

## Decision: `http` as the HTTP boundary

**Decision**: Propose `http ^1.6.0`; own one injected `http.Client` in the composition root,
pass it to the repository data source, and close it with its owner. Use `AbortableRequest`
where transport cancellation is supported; request-generation checks remain the correctness
guard. Do not add Dio or a retry plugin.

**Rationale**: The API is four GET reads with no upload, interceptors, token refresh, or
complex retry protocol. The Dart-published package is cross-platform and its docs recommend
`Client` injection for testability; `http/testing.dart` supplies deterministic mock-client
support. Bounded timeouts and response mapping live in the small client wrapper.

**Alternatives considered**: Dio includes interceptors and broader request controls but
those features are unused here. Native `dart:io` clients would reduce portability and are
discouraged by Flutter networking guidance. The current contract does not require a
platform-specific native networking implementation.

## Decision: Core JSON parsing; handwritten DTOs

**Decision**: Use `dart:convert` plus explicit `fromJson` mapping for the small fixed set of
contract objects. Avoid `json_serializable`, Freezed, `build_runner`, and generated unions.

**Rationale**: The OpenAPI schemas are small and closed (`additionalProperties: false`).
Explicit required-field/type validation can identify contract drift and avoid generated
build steps. Models and parser tests are specified in [data-model.md](data-model.md).

**Alternatives considered**: `json_serializable` is appropriate if the API model count or
schema churn grows, but it is not necessary for six response shapes in this feature. Avoid
manual dynamic JSON outside the data boundary.

The official [Dart JSON serialization guidance](https://dart.dev/libraries/serialization/json)
documents both core `dart:convert` mapping and generated `json_serializable` as supported
approaches; the small contract surface favors explicit mapping for this feature.

## Decision: `intl` only for localized labels

**Decision**: Propose `intl ^0.20.3` for localized human-readable calendar/date labels;
keep request dates and scheduled time components as contract-preserving values.

**Rationale**: It is Dart-published and provides date/number formatting. Current version
requires Dart 3.9, which fits this project's Dart 3.13.2. Use locale formatting only after
extracting the source's local calendar/date/clock components. Never call `toLocal()` or
convert an offset-bearing scheduled timestamp to a device-zone instant.

**Alternatives considered**: Handwritten date labels would avoid a package but would not
localize month/day order. `DateTime` alone is not a locale formatter and normal parsing of
an offset-bearing ISO timestamp can normalize its offset; preserve the raw API value.

## Decision: Built-in tests, fakes, and HTTP MockClient

**Decision**: Use existing `flutter_test`, fake repository implementations, Riverpod
provider overrides, and the `MockClient` available through `http/testing.dart`. Do not add
Mockito, Mocktail, or code generation initially.

**Rationale**: The repositories and client will be injectable, which enables deterministic
state and response testing without mocking the UI state owner itself. Flutter already has
`flutter_test` in `dev_dependencies`; provider overrides and `MockClient` cover the main
cases with no extra dev dependency.

**Alternatives considered**: Mocktail provides convenient interaction verification and
Mockito provides generated/manual mocks, but neither is needed to assert request paths,
results, transitions, or recovery behavior through fakes.

## Decision: Environment-specific API URL and emulator networking

**Decision**: Read an environment-specific `SMART_RAIL_API_BASE_URL` at startup from a
compile-time define or platform flavor. Keep the API root out of source constants and source
control configuration. Require HTTPS for shared/demo environments. For local Android
emulator-to-host development, use the Android emulator host alias `10.0.2.2` and a reachable
port; physical devices must use a reachable host/LAN address or deployed HTTPS host. Android
release manifest must declare Internet permission.

**Rationale**: OpenAPI's server URL is relative to an approved environment API base URL and
the specification says the base URL is external configuration. Flutter documents Android
Internet permission, and Android documents `10.0.2.2` as the emulator alias for host
loopback ([Android emulator network addresses](https://developer.android.com/studio/run/emulator-networking-address),
[Flutter networking](https://docs.flutter.dev/data-and-backend/networking)). No API base URL
is in the repository.

**Alternatives considered**: Hardcoding localhost fails on physical devices and can leak a
development address into a release. A dotenv package is unnecessary for one public URL;
build-time defines/flavors suffice. Do not add a production cleartext exception.

## Resolved technical unknowns and residual risks

- No numeric backend latency or service-volume SLO is approved. The client will provide
  immediate loading feedback and use a configurable bounded timeout; target values can be
  tuned after the actual backend is available.
- No usable environment base URL or seeded backend instance was found in the Flutter
  repository/configuration. The design is complete with an external URL input, but a real
  integration run needs the backend owner to provide a reachable address and data fixture.
- Android manifest currently lacks the Internet permission in `src/main`; implementation
  must add it for release builds. No iOS networking entitlement is needed for this iOS app.
- The OpenAPI operations do not enumerate 403, while the approved spec/RBAC test scope
  requires a passenger-facing access-denied state. Implement the clarified response behavior
  if an upstream gateway/platform returns it; do not infer an operation-specific new API
  contract. The OpenAPI `default` response documents unexpected Problem Details.
- Status timestamps and other unsupported operational state stay out of this feature.

## External references

- [flutter_riverpod 3.4.3](https://pub.dev/packages/flutter_riverpod/versions/3.4.3)
- [Riverpod testing](https://riverpod.dev/docs/how_to/testing)
- [go_router 18.1.0](https://pub.dev/packages/go_router/versions/18.1.0)
- [http 1.6.0](https://pub.dev/packages/http/versions/1.6.0)
- [intl 0.20.3](https://pub.dev/packages/intl/versions/0.20.3)
- [Flutter networking](https://docs.flutter.dev/data-and-backend/networking)
- [Android emulator network addresses](https://developer.android.com/studio/run/emulator-networking-address)
- [Dart JSON serialization](https://dart.dev/libraries/serialization/json)
- [Flutter testing overview](https://docs.flutter.dev/testing/overview)
