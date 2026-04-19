# Admin Terminal V1

Admin tooling is intentionally small. The terminal is only for actions that are useful while testing; read-only status belongs in the in-game Overseer panel.

## Command Prefix

All commands use the `op` prefix:

```text
op help
op reload
```

## Current Commands

- `op help`: Lists available commands.
- `op reload`: Restores the captured vanilla snapshot, then reapplies current config and runtime tuning.

Paths, export, reset, lobby, multiplayer, players, handshake, fingerprint, sync, validation, perk, and progression inspection are not terminal commands. Use logs for diagnostics and the in-game Overseer panel for read-only status. The panel uses the `Open Overseer Panel` InputUtils keybind. Ship perk information is host-only, and players only see their own player progression.

## Next Hook

The terminal hook patches the game `Terminal` input flow and passes submitted text to `AdminCommandService.Execute`.

It is controlled by:

```ini
[General]
AdminCommandPrefix = op
```

If `AdminCommandResult.Handled` is true, render `AdminCommandResult.Message` in the terminal and prevent vanilla command handling for that input.
