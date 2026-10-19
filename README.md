# Bomome Telemetry (Beta 0.1.0)

Provider-neutral event routing. No Unity dependency, SDK, network client, game rules,
automatic collection or account identifiers. This is a new package, not a replacement
of any existing consumer analytics implementation.

## Features
- Immutable bounded events with scalar properties, event ID and UTC timestamp.
- Independent master switch, explicit consent and category filtering.
- Per-sink category routing, isolated sink errors and delivery diagnostics.
- Explicit tutorial/step funnel helper; skipping never implies completion.
- No polling or unbounded offline queue. Unknown/denied events are suppressed,
  not retained for later transmission.

## Integration
Create ITelemetrySink adapters in a separate SDK integration assembly/package.
Register one route per stable provider ID, then register ITelemetryService with
the application's service registry. TelemetryService defaults to Unknown consent.
Set Consent=Granted only after the application's privacy policy permits collection.
Never send player names or other PII as event properties.

Track(new TelemetryEvent("level_start", TelemetryCategory.Progression, fields))
dispatches synchronously on the caller's thread. Call on the Unity main thread for
Unity SDKs. Adapters own initialization, field limits, provider-native mapping,
bounded retry policy and SDK automatic-event/consent configuration. The router
does not make a third-party SDK consent-safe by itself.

TutorialTelemetry records started, step_started, step_completed, step_skipped,
completed and skipped events in Tutorial, never Progression.

ProtoFactory owns config assets, consent persistence, runtime observation and a
development console sink. Its starter has no vendor SDKs and starts with Unknown
consent. BattleTroops has not adopted this package.

## Validation
ProtoFactory/Tools/RunStarterChecks.ps1 compiles the actual library sources with
Unity's bundled .NET runtime and checks routing, immutability, privacy and funnels.
This does not verify any provider SDK or dashboard.
