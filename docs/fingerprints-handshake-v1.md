# Fingerprints And Handshake V1

OverseerProtocol now computes stable fingerprints for future host/client compatibility checks.

## Fingerprints

`FingerprintFeature` currently produces:

- `activePreset`
- `presetFingerprint`
- `configFingerprint`

The preset fingerprint hashes these files for the active preset:

- `preset.json`
- `items.override.json`
- `spawns.override.json`
- `moons.override.json`
- `lobby-rules.json`
- `runtime-rules.json`

The config fingerprint hashes the relevant `.cfg` values:

- active preset
- feature toggles
- validation mode
- runtime multipliers
- semantic difficulty profile

## Handshake

`HandshakeFeature` builds a `ProtocolHandshakeDefinition` with:

- OverseerProtocol version.
- active preset.
- preset fingerprint.
- config fingerprint.
- lobby handshake rules.
- enabled feature flags.

This is not yet sent over the network. It is ready for a future connection approval / host-client negotiation layer.

`HandshakeCompatibilityService` can compare a host/client handshake pair and returns warnings/errors for:

- OverseerProtocol version mismatch.
- Active preset mismatch.
- Preset fingerprint mismatch.
- Config fingerprint mismatch.
- Max player rule mismatch.
- Late join mode mismatch.

## Admin Commands

The admin command service exposes:

```text
op fingerprint
op handshake
```

Once a terminal hook exists, these commands should help diagnose host/client mismatch issues without reading logs manually.
