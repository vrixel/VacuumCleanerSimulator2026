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
powershell -File tools\smoke-test.ps1        # automated run of the build: self-screenshots (Builds\smoke-*.png), drives, logs "[VCS] Smoke result", quits
powershell -File tools\release.ps1 -Version 0.2.0   # build, zip Builds\Win64, publish GitHub release v0.2.0 with the zip (-SkipBuild to reuse a smoked build, -Draft)
python tools\marketing.py                   # cut marketing\source\*.png into store sizes (marketing\store), icon sizes + icon.ico (marketing\icon), Assets\Icon\icon.png
```

```powershell
python tools\assets\kie_assets.py --list                 # generated-asset campaign: what exists, what is missing
python tools\assets\kie_assets.py --dry-run              # what would be generated and with which model, no spend
python tools\assets\kie_assets.py                        # generate missing gauges, containers, panel, music, sounds (kie.ai credits)
python tools\assets\kie_assets.py --reprocess --images-only   # rebuild Assets/Resources from tools/assets/raw, no API calls
python tools\assets\kie_assets.py --sheet tools\assets\raw\contact.png   # contact sheet of every processed image
python tools\assets\kie_assets.py --only bag_full --force # redo one asset
python tools\assets\kie_assets.py --images-only --only mk_key_art --force   # marketing images (mk_* items -> marketing\source), 5 credits each
```

Generated assets follow the kie-ai skill discipline (idempotent, raw downloads kept in `tools/assets/raw`, never
overwritten without `--force`, balance printed before and after). Look at every image before shipping it: a
`success` proves an image came back, not that the prompt was honoured. Binary assets are tracked with git LFS.

The smoke test runs the player with `-smoke <dir>` (see `SmokeRunner`, `-Width/-Height` for 1080p listing shots): it
never injects keystrokes or steals focus, so it is safe while the owner is using the PC. It drives, chases the cat
(`smoke-cat.png`, cat state in the log), photographs the powder trail from above (`smoke-powder.png`), shortens the
cord to 4 m and pulls the plug out (`smoke-taut.png`, `smoke-rewind.png`). Look at the PNGs; the log scan alone only
proves the game did not throw. The player log line `d3d12: failed to query info queue interface` is benign.

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
  `SuctionSystem` (cone of pull, absorb when size class <= power level + `SizeBonus`, bag, blow-out respawns items
  from `BagItem` records), `VacuumVisuals` (body from the catalogue, eyes, squash).
- The garage: `VacuumCatalog` lists the selectable vacuums as `VacuumSpec` (handling stats, eye and nozzle anchors,
  a `Build` delegate); `VacuumModels` holds one builder per vacuum, made of `MeshKit` solids (revolve profiles,
  spline tubes with optional corrugation, rounded boxes) and `Palette.Mat` materials (plastic, glossy, rubber, fabric,
  chrome). All models are brand-inspired lookalikes with parody names: never use real product names or logos.
  `VacuumPreview` renders the selected model on a hidden stage at y = -500 into a RenderTexture shown by the title
  screen (`MenuController`); the choice persists in PlayerPrefs `vacuum_id`. To add a vacuum: one builder in
  `VacuumModels`, one entry in `VacuumCatalog.All`. Local space is origin on the floor, +z forward, metres.
- `ObjectiveSystem` is event-string driven: `Report("absorb:Sock")`, `"knock"`, `"launch"`, `"bagfull"`, `"trash"`,
  `"speed"`, `"spin"`, `"clean100"`. Completion is remembered in PlayerPrefs (`ach_<id>`), progress is per run.
- UI is uGUI built by `UIFactory` with the built-in `LegacyRuntime.ttf`; `HudController` caches values and only
  touches Text when something changed. `MenuController` handles title and pause with keyboard, gamepad and mouse.
- The cockpit (`Cockpit`, 250 px strip at the bottom) is the simulator joke: a suction gauge styled per vacuum
  (generated face in `Resources/UI/Gauges/gauge_<id>`, needle or LED ring per `VacuumSpec.Gauge`), the real
  container filling up (`Resources/UI/Containers/<kind>_empty` under `<kind>_full` with a vertical fill), motor
  readouts, warning lamps, odometer, a system-status box. Numbers come from `Telemetry`, a pretend model updated
  each frame by `GameManager` that never touches physics. Every sprite has a procedural fallback in `UISprites`,
  so the game runs without the generated art. `AssetImportRules` makes everything under `Resources/UI` a sprite
  and streams `Resources/Audio/Music`.
- Music: `GameAudio.PlayMusic("title" | "game")` loads `Resources/Audio/Music/<name>` and crossfades; a generated
  motor recording (`Resources/Audio/Sfx/motor_loop`) replaces the synthesised hum when present.
- Art direction of the HUD (settled 2026-09-05 after two rejected passes, one blurry-dull and one Vegas): bold
  arcade cabinet on serious instruments. `UIStyle` holds it: Russo One italic headlines with a hard black edge and
  a block shadow (`ArcadeText`), safety yellow / electric blue / red / white, generated instrument frames from
  `Resources/UI/Hud` nine-sliced under live content (`Frame`, `PlateSprite`), yellow `Tab` labels, `Tile`
  annunciators, `Tape` scrolling scales, `Digital` seven-segment readouts (DSEG7 with ghost digits), Share Tech
  Mono for values, Exo 2 for body. No soft glow anywhere: blur was the first complaint. Fonts are OFL in
  `Resources/Fonts` with `OFL-1.1.txt` and `FONTS-README.txt`: keep them when adding fonts.
- The HUD (`HudController`) is spread around the screen: score block top-left, power strip top-centre, timer and
  `RadarView` top-right (a top-down camera into a masked RawImage; markers are quads on layer 8 that the main and
  preview cameras cull), vertical meters left, mission log right, `Cockpit` at the bottom.
- The cord (`PowerCord`, corded vacuums only) is a simulated cable since 2026-09-05 (evening): a chain of points
  (0.22 m segments, Verlet integration, 6 constraint passes, gravity, floor friction) pinned to the `WallSocket`
  and to the reel outlet on the back of the vacuum. Only walls stop it: static colliders taller than 0.3 m, pushed
  out with `Physics.ComputePenetration`; anything with a rigidbody (furniture, mess, cat, vacuum) is ignored so the
  cord slides over it. The reel pays out one segment per step when the whole chain is overstretched by more than
  0.7 segment (a reel brake), up to `MaxLength` (22 m, static so the smoke test can shorten it); `Length` is the
  paid-out cable, slack lies on the floor. At the end it is a leash (outward velocity removed, overshoot taken
  back); pushing outward for 0.9 s more pops the plug (`YankPlug`, event `yank`, logged) and the cord reels itself
  in. `Rewind()` (R / Y, and automatically on Spotless) consumes points at the vacuum, so the free end whips across
  the floor and knocks light debris. Drawn with a LineRenderer on `Palette.Mat` (no tangents).
- `GameAudio` synthesises every clip at startup (`AudioClip.Create`); `EffectsFactory` configures particle systems.
- Look (2026-09-05, "realistic, not cartoon"): `VacuumVisuals.RealisticLook` drops the googly eyes; `VacuumDetails`
  adds vent slots, nameless badges, hubcaps, screws and seams per model id on top of the `VacuumModels` builders;
  `Palette.Plastic/Glossy/Rubber/Fabric/Chrome` are normal-mapped (`ProceduralTextures`: tileable grain, brushed,
  weave, wood) on `Resources/Materials/LitBump.mat` (carries `_NORMALMAP` so the variant ships); MeshKit meshes
  get tangents. Lighting: trilight ambient, soft shadows at VeryHigh, MSAA 4x, and a realtime `ReflectionProbe`
  rendered once per level build (`BuildLighting`). LineRenderers keep `Palette.Mat` (no tangents).
- Vacuum shells v2 (2026-09-05, direction validated on `docs/concepts/sheet.png`, generated by
  `tools/assets/concepts.py`): `VacuumModelsV2` keeps signature surfaces round (discs, drums, bins, cowls: smooth revolves) and uses
  flat-shaded facets (`MeshKit.Flat`) only for hex hubs; grooves and seams in the profiles, bolted panels, translucent bins (`Palette.Glass`, Fade), emissive LEDs and tiny displays
  (`Palette.Led` on `Resources/Materials/LitEmissive.mat`), desaturated real-product colours, wheels with tyres and
  hubs. Same anchors as `VacuumModels`, which now dispatches to V2 unless `VacuumModels.UseV2` is false (the gallery
  renders both: `tools/gallery.ps1` -> `docs/screenshots/models-before-after.png`). `VacuumDetails` only applies
  to the 0.2 shells. Add a vacuum: builder in `VacuumModelsV2`, dispatch line in `VacuumModels`, entry in the catalogue.
- Cocoa powder (`PowderSystem`, one `PowderLayer` quad per room at y = 0.012): generated RGBA texture at 36 px/m
  (splats, streaks, dusting), `Resources/Materials/Fade.mat` (Standard Fade, keyword in the asset). `SuctionSystem.Suck`
  calls `Powder.Vacuum(nozzle, radius)` every physics step while grounded and the bag has room: it zeroes alpha in a
  disc, so the vacuum's path stays visible; cleared m² go to `GameManager.OnPowderCleaned` (40 pts/m², event
  `powder`, bag fill 1.5 L/m²) and every 1.5 m² counts as one piece of mess in the cleanliness ratio.
- The cat (`Cat`, spawned by `LevelBuilder.BuildCat` in the living room): idle / wander / flee state machine, three
  raycast feelers to steer around walls and furniture (small mess is ignored), 8.5 m/s flee with a sideways panic hop
  when nearly caught, knocks light debris; scares and bumps go to `GameManager.OnCatScared` (event `cat`, synthesised
  meow / yowl in `GameAudio`). It has no `Debris`, so it can never be absorbed.
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

- Unity Hub 3.21.1 is a portable extraction at `D:\Program Files\Unity Hub\Unity Hub.exe` (its installer demands UAC,
  which Claude cannot click; 7-Zip at `D:\DevTools\7-Zip` unpacks the Hub's NSIS installer but NOT the 4 GB editor
  installer). Headless CLI works: `"Unity Hub.exe" -- --headless install-path --set <dir>`, `editors --add <Unity.exe>`,
  `editors -i`.
- Editor 6000.3.23f1 (changeset 09d2ecc7fb28) at `D:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`,
  registered in the Hub. Installed by `tools\install-unity.ps1`: the installer manifest wants elevation, but running it
  with `__COMPAT_LAYER=RunAsInvoker` skips UAC and it writes fine to D:. It idles forever after copying its 8 GB
  (no window, no children): once `Editor\Unity.exe` exists and the folder stops growing, kill the process.
- Licensing: Unity Personal, activated by signing into the Hub with the owner's Google account and taking the free
  personal license. Until then `Unity.exe -batchmode` exits with code 198 ("No valid Unity Editor license found")
  in about one second; `compile-check.ps1` still works because it never starts the editor.
- Compile check details: engine modules target .NET Standard 2.1 (runtime project is `netstandard2.1`), editor modules
  are .NET Framework (`net48`); both module sets live in `Editor\Data\Managed\UnityEngine`. uGUI is a source package,
  so the check references the precompiled `UnityEngine.UI.dll` from `Data\Resources\PackageManager\ProjectTemplates\libcache`.
- No C++ toolchain on this machine: keep the Standalone scripting backend on Mono. IL2CPP and UWP need Visual Studio.
  The IL2CPP module is not installed either, and `Editor\Data\il2cpp` (which also holds UnityLinker) does not exist,
  so managed stripping is set to Disabled in `ProjectSetup`; with stripping on, the build dies in the
  "ManagedStripped" Bee step with "The system cannot find the path specified".
- `Unity.exe` is a GUI-subsystem executable: PowerShell's `&` returns immediately. `build.ps1` uses
  `Start-Process -PassThru` then `WaitForExit()` on that process only. `Start-Process -Wait` would also wait for
  Unity's descendants (shader compiler, package manager, import workers), which idle for ~10 minutes after the
  editor has exited: that turned a 15-second batch build into an 11-minute one.
- A warm batch build is about 15 s of editor time (Unity's `-timestamps` log shows it); the player build step is 6 s.
- Large heredocs (over roughly 10 KB) fail in the Bash tool on this machine; write source files with the Write tool.
- Everything on D:, deliverables in English, commit and push on `main` as soon as something works.
- Xbox consoles: UWP games are no longer accepted on the Xbox Store (Microsoft Q&A, Nov 2025) and the Hub refuses to
  add modules to this editor (installed outside the Hub); the only path is ID@Xbox (owner application) + GDK. Do not
  install the UWP toolchain for it. Blender 4.5 portable lives in `D:\Program Files\Blender` (not used by the build).
- The repo is PUBLIC (vrixel/VacuumCleanerSimulator2026) since 2026-09-05, downloads at `releases/latest`; never
  commit kie keys or anything from `D:\Cloclo\Projects\Nazisme`. `LICENSE.md` is all rights reserved (source for
  reference), fonts are OFL. Bump `bundleVersion` in `ProjectSetup` with every release.
- App icon: `Assets/Icon/icon.png` (512, cut by `tools/marketing.py` from the kie icon) is applied to the Standalone
  and default icon slots by `ProjectSetup.ApplyIcon` on every batch build; the exe shows it.
- The game page on cosnuau.com is `public/vacuum.html` (a flat file: CloudFront serves the home page for folder URLs, assets in `public/vacuum/`) in `D:\Cloclo\Projects\cosnuau.com` (web-sized JPEGs
  and icons next to it, produced from `marketing/store` and `docs/screenshots`), plus the engraved register emblem
  `public/assets/emblems/d8.png` (from `mk_emblem`, 192 px pure black on transparency). The register only shows
  mysterious black engravings; the game keeps its own cartoon style. Deploy = push to that repo's `main` touching
  `public/**` (CI syncs to S3 and invalidates CloudFront).
- Microsoft Store: the owner's Partner Center account (already used for another game) covers PC games at no extra
  cost; `docs/STORE.md` has the listing copy, the asset table and the step lists. `python tools\msix.py` packages
  `Builds\Win64` as an unsigned MSIX (full-trust desktop app) with `makeappx.exe` from the
  Microsoft.Windows.SDK.BuildTools NuGet package extracted under `D:\DevTools\WindowsSDK-BuildTools` (no admin);
  the identity values come from Partner Center > Product identity. Steam is parked (100 USD per game).
