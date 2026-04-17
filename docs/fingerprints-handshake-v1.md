# Fingerprints And Handshake V1

OverseerProtocol computes fingerprints for future host/client compatibility checks.

## Fingerprints

`FingerprintFeature` produces:

- `activePreset`
- `presetFingerprint`
- `configFingerprint`

The preset fingerprint hashes the selected built-in preset name. The config fingerprint hashes relevant `.cfg` state, including toggles, validation mode, multipliers, item/moon/spawn overrides, lobby rules, and runtime rules.

No JSON tuning files are fingerprint inputs.

## Handshake

`HandshakeFeature` builds a `ProtocolHandshakeDefinition` with:

- OverseerProtocol version;
- active preset;
- preset fingerprint;
- config fingerprint;
- lobby handshake rules;
- enabled feature flags.

This is not yet sent over the network. It is ready for a future connection approval or host/client negotiation layer.
