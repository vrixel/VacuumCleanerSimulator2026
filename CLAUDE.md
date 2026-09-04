# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Vacuum Cleaner Simulator 2026: a Goat-Simulator-style physics sandbox where the player is a vacuum cleaner.
Unity 6000.3.23f1, C#, built-in render pipeline, legacy Input Manager. Targets Steam (Windows 64-bit) first,
Xbox / Microsoft Store second (see `docs/PUBLISHING.md`). Audience 8+, so no violence, no language.
Repo: https://github.com/vrixel/VacuumCleanerSimulator2026 (private). The repo root is the Unity project root.

## Commands

```powershell
powershell -File tools\compile-check.ps1     # compile all scripts against Unity's DLLs with the .NET SDK (seconds, no editor, no license)
powershell -File tools\build.ps1             # batch-mode Win64 build -> Builds\Win64\VacuumCleanerSimulator2026.exe, log in Builds\build.log
powershell -File tools\build.ps1 -Run        # same, then launch the exe (player log in Builds\player.log)
powershell -File tools\run.ps1               # launch the last build
```

Builds are local only. There is no GitHub Actions workflow on purpose (Unity in CI burns minutes and needs a license
server); do not add one. The editor path is resolved in `tools/common.ps1`, override with `$env:VCS_UNITY`.

Run `compile-check.ps1` after every code change; it is the fast feedback loop. `build.ps1` is the real check (also
creates the scene and material assets on first run through `ProjectSetup.Apply`).

## Architecture

Everything is created from code at runtime; there are no prefabs, no art, no audio assets. The scene is empty.

- `Bootstrap` (RuntimeInitializeOnLoadMethod) spawns `GameManager`, the single owner of the run: state machine
  (Title / Playing / Paused), score, combo, power level (1..5, thresholds in `PowerThresholds`), banner queue.
  All other systems are created by it in `Start` and reached through `GameManager.I`.
- `LevelBuilder` builds the house from primitives (rooms as Rects, walls with door gaps, furniture, scattered mess)
  and tracks cleanliness as `MessCleaned / MessTotal`. Rebuilt with a new seed on every `StartGame`.
- `PropFactory` is the catalogue: `DebrisKind` -> `DebrisSpec` (size class, points, bag volume, mass, counts as mess)
  and the primitive assembly for each kind. Anything the vacuum can interact with carries a `Debris` component on the
  rigidbody root. Furniture (size class 3+) also gets `TipOverTracker`; things blown out get `LaunchTracker`.
- `VacuumController` (rigidbody + sphere collider, camera-relative driving, hop, turbo, spin/speed tracking),
  `SuctionSystem` (cone of pull, absorb when size class <= power level, bag, blow-out respawns items from `BagItem`
  records), `VacuumVisuals` (body, eyes, squash).
- `ObjectiveSystem` is event-string driven: `Report("absorb:Sock")`, `"knock"`, `"launch"`, `"bagfull"`, `"trash"`,
  `"speed"`, `"spin"`, `"clean100"`. Completion is remembered in PlayerPrefs (`ach_<id>`), progress is per run.
- UI is uGUI built by `UIFactory` with the built-in `LegacyRuntime.ttf`; `HudController` caches values and only
  touches Text when something changed. `MenuController` handles title and pause with keyboard, gamepad and mouse.
- `GameAudio` synthesises every clip at startup (`AudioClip.Create`); `EffectsFactory` configures particle systems.
- `Palette` caches one material per colour on top of `Resources/Materials/Lit.mat` (Standard shader). That material
  asset exists so the shader is not stripped from builds; `ProjectSetup` creates it. Never enable shader keywords
  at runtime (the variant will be missing in the build).

Input: `GameInput` wraps the legacy Input Manager. Axes live in `ProjectSettings/InputManager.asset` (Horizontal,
Vertical, CamX, CamY, DPadX, DPadY, TriggerL, TriggerR); buttons are read with `KeyCode.JoystickButtonN`
(Xbox: A=0 B=1 X=2 Y=3 LB=4 RB=5 Back=6 Start=7). Do not add the Input System package: it needs
`activeInputHandler` in ProjectSettings.asset and an editor restart.

Unity 6 API names in use: `Rigidbody.linearVelocity`, `linearDamping`, `angularDamping`, `PhysicsMaterial`,
`Object.FindFirstObjectByType`.

## Environment facts

- Unity Hub is a portable extraction at `D:\Program Files\Unity Hub\Unity Hub.exe` (the installer demands UAC, which
  Claude cannot click; 7-Zip at `D:\DevTools\7-Zip` unpacks NSIS installers). Its editor install path is set to
  `D:\Program Files\Unity\Hub\Editor` via `"Unity Hub.exe" -- --headless install-path --set`.
- Editor 6000.3.23f1 (changeset 09d2ecc7fb28) at `D:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`.
- Licensing: Unity Personal, activated by signing into the Hub with the owner's Google account. Batch-mode builds fail
  with a licensing error until that is done once.
- No C++ toolchain on this machine: keep the Standalone scripting backend on Mono. IL2CPP and UWP need Visual Studio.
- Everything on D:, deliverables in English, commit and push on `main` as soon as something works.
