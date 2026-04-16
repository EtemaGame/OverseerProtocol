# References Setup

OverseerProtocol needs two kinds of compile references:

- Public modding/runtime libraries such as BepInEx and Harmony.
- Local game assemblies from a legitimate Lethal Company install.

## Important

Do not download `Assembly-CSharp.dll` or other Lethal Company game assemblies from third-party internet mirrors. They are game files and should come from your local install.

## Automated Helper

From the repo root:

```powershell
pwsh tools/setup-references.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\Lethal Company" -DownloadBepInEx
```

What it does:

- Downloads BepInEx 5.4.22 from the official GitHub release when `-DownloadBepInEx` is supplied.
- Copies `BepInEx.dll` and `0Harmony.dll` into `references/bepinex`.
- Copies game DLLs from the local game install into `references/game`.

Expected game files:

- `Assembly-CSharp.dll`
- `Unity.Netcode.Runtime.dll`
- `UnityEngine.dll`
- `UnityEngine.CoreModule.dll`

## Manual Layout

```text
references/
  bepinex/
    BepInEx.dll
    0Harmony.dll
  game/
    Assembly-CSharp.dll
    Unity.Netcode.Runtime.dll
    UnityEngine.dll
    UnityEngine.CoreModule.dll
```

After references are in place, run:

```powershell
dotnet build OverseerProtocol.slnx
```

## Verify

Use:

```powershell
powershell -ExecutionPolicy Bypass -File tools\verify-env.ps1
```

Expected before copying game assemblies:

- `BepInEx.dll`: OK
- `0Harmony.dll`: OK
- game assemblies: missing

Expected after copying from a local Lethal Company install:

- all BepInEx and game references: OK
