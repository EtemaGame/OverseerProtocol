# Admin Terminal V1

Admin tooling is currently implemented as a command service, not yet as a patched in-game terminal UI.

This keeps the command contract stable while avoiding fragile Terminal patches before runtime testing is available.

## Command Prefix

All commands use the `op` prefix:

```text
op help
op preset
op paths
op export
op reload
op reset
op fingerprint
op rules
op perks
op handshake
op validate
```

## Current Commands

- `op help`: Lists available commands.
- `op preset`: Reports the active preset from `.cfg`.
- `op paths`: Reports OverseerProtocol data roots.
- `op export`: Runs catalog export immediately.
- `op reload`: Restores the captured vanilla snapshot, then reapplies current config, presets, overrides, and multipliers.
- `op reset`: Restores the captured vanilla runtime snapshot without reapplying overrides.
- `op fingerprint`: Reports the current preset/config fingerprints.
- `op rules`: Reports current lobby/runtime rules summary.
- `op perks`: Reports current perk catalog and progression summary.
- `op handshake`: Reports the current handshake summary.
- `op validate`: Explains the current dry-run validation workflow.

## Next Hook

Once runtime testing is available, patch the game `Terminal` input flow and pass submitted text to `AdminCommandService.Execute`.

If `AdminCommandResult.Handled` is true, render `AdminCommandResult.Message` in the terminal and prevent vanilla command handling for that input.
