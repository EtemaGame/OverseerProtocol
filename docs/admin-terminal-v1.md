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
op progression
op handshake
op multiplayer
op sync snapshot
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
- `op progression`: Reports ship/player progression summary.
- `op progression grant ship <amount>`: Debug command that grants ship XP.
- `op progression reset ship`: Debug command that resets ship progression.
- `op handshake`: Reports the current handshake summary.
- `op multiplayer`: Reports experimental multiplayer status.
- `op multiplayer apply`: Re-applies experimental multiplayer rules.
- `op sync snapshot`: Reports a reserved sync snapshot summary for future state sync.
- `op validate`: Explains the current dry-run validation workflow.

## Next Hook

The experimental hook patches the game `Terminal` input flow and passes submitted text to `AdminCommandService.Execute`.

It is controlled by:

```ini
[Admin]
EnableAdminTerminalCommands = false
AdminCommandPrefix = op
```

If `AdminCommandResult.Handled` is true, render `AdminCommandResult.Message` in the terminal and prevent vanilla command handling for that input.
