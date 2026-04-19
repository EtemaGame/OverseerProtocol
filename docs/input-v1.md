# Input V1

OverseerProtocol uses LethalCompanyInputUtils for the in-game Overseer panel keybind.

The implementation registers an `LcInputActions` action named `Open Overseer Panel`, so it appears in Lethal Company's remap controls menu under OverseerProtocol.

## Dependency

OverseerProtocol requires `com.rune580.LethalCompanyInputUtils`. The dev profile should include `Rune580-LethalCompany_InputUtils`.

## Default Bind

Default keyboard binding: `P`.

Change it from the in-game controls menu:

1. Open Settings.
2. Open Change Keybinds.
3. Show legacy controls if needed.
4. Find OverseerProtocol.
5. Rebind `Open Overseer Panel`.

## Implementation

OverseerProtocol does not expose a BepInEx config key for this binding. The keybind is owned by InputUtils and is changed only through the in-game controls menu.

The `OverseerInputActions` class subscribes after InputUtils finishes loading the action asset, then emits the `OpenPanelPressed` event used by the panel.
