# Store listing copy and submission checklists

Everything below is ready to paste. Submissions go through the owner's accounts; nothing here can be submitted by
Claude. The owner already has a Microsoft Partner Center developer account (an app is published): that one
registration covers every Windows app and game, PC included, so the Store costs nothing more. Steam is different:
Steamworks charges 100 USD per game (Steam Direct fee, refunded once the game earns 1,000 USD).

## Listing copy

**Title:** Vacuum Cleaner Simulator 2026

**Short description (under 200 characters):**
You are the vacuum cleaner. The house is filthy. Eat crumbs, socks, chairs and eventually the toilet, then blow it
all back out. A silly physics sandbox with a suspiciously serious cockpit.

**Long description:**
Vacuum Cleaner Simulator 2026 is a physics sandbox in the spirit of Goat Simulator, except you are a vacuum
cleaner and the mess is not going to clean itself. Drive through a six-room house, suck up crumbs, dust bunnies,
socks, toy bricks and coins, then level up until chairs, the couch, the fridge and the toilet fit in the nozzle.
When the bag is full, blow everything back out at high speed and start again.

Eight vacuums with real handling differences: a robot disc, a bagless upright on a ball, a smiling red canister,
a cordless stick, a 1978 upright with a headlight, a French canister that whispers, a wet-and-dry drum that eats
furniture early, and the cardboard prototype that started it all. Corded models drag a real cord behind them: it
straightens as you go, pulls tight around corners, and at the end it holds you like a leash. Keep pulling and the
plug pops out of the wall. Cordless models worry about their battery instead.

The cockpit takes itself very seriously. Every vacuum has its own suction gauge, a real dust bag or bin that fills
up, motor readouts, warning tiles, a MASTER CAUTION lamp, a dirt radar and a mission log of twenty silly
achievements. None of it will help you. All of it looks great.

- Family friendly: cartoon chaos, no violence, no language, no ads, no purchases.
- Full controller support (Xbox layout) and keyboard and mouse.
- Local achievements, best score and garage choice saved between sessions.

**Tags:** Simulation, Physics, Casual, Comedy, Sandbox, Family Friendly, Singleplayer, Controller Support

**Age rating answers (IARC / ESRB / PEGI questionnaire):** no violence, no blood, no sexual content, no language,
no drugs, no gambling, no user interaction, no data collection beyond local save files, no in-app purchases.
Expected: ESRB E, PEGI 3.

## Marketing assets (marketing/)

Generated with kie.ai from `tools/assets/kie_assets.py` and cut to store sizes by `tools/marketing.py`.

| File | Size | Used for |
|------|------|----------|
| `marketing/store/steam_header.png` | 920 x 430 | Steam header capsule |
| `marketing/store/steam_small.png` | 462 x 174 | Steam small capsule |
| `marketing/store/steam_main.png` | 1232 x 706 | Steam main capsule |
| `marketing/store/steam_library.png` | 600 x 900 | Steam library capsule |
| `marketing/store/steam_hero.png` | 3840 x 1240 | Steam library hero |
| `marketing/store/ms_icon_300.png` | 300 x 300 | Microsoft Store icon |
| `marketing/store/key_art.png` | source | press, GitHub, cosnuau.com |
| `marketing/icon/icon_*.png`, `marketing/icon/icon.ico` | 16 to 1024 | app icon, exe icon, site favicon |
| `docs/screenshots/*.png` | 1920 x 1080 | store screenshots |

## GitHub release

`tools\release.ps1 -Version 0.1.0` builds, zips `Builds\Win64` and publishes a GitHub release with the zip.

## Microsoft Store (PC first, Xbox later)

**Status 2026-09-05: submission 1 (v0.1.1.0) certified and live the same day (Partner Center mail 12:09 UTC, IARC live
rating notice 12:16 UTC); submission 2 (v0.2.0.0) drafted with new screenshots and release notes.** Submission 1 was, free, all 240 markets, public, publish
as soon as it passes. Filled: pricing, properties (Games / Simulation + Family + kids, no personal data, single player
PC, support site), IARC questionnaire (ESRB Everyone, PEGI 3), package, English (US) listing with 4 screenshots
(1920 x 1080, from `tools\smoke-test.ps1 -Width 1920 -Height 1080`), poster 720 x 1080, box art 1080, super hero
1920 x 1080, tile icon 300, keywords, notes for certification. Lessons: a `runFullTrust` package makes a privacy
policy URL mandatory (https://cosnuau.com/vacuum-privacy.html); Partner Center sessions expire after a few minutes
of inactivity and the sign-in popup is the owner's; every screenshot file input takes one file, then a new input
appears. Next version: bump `bundleVersion`, `toolsuild.ps1`, smoke, `tools\msix.py`, new submission, drag the
MSIX, fill "What's new".

Reserved on 2026-09-05 in the owner's Partner Center: Store ID `9P9HVRJ09PK0`, page
https://apps.microsoft.com/detail/9P9HVRJ09PK0. Identity for the manifest (public values, not secrets):

```
python tools\msix.py --identity-name Cosnuau.VacuumCleanerSimulator2026 --publisher "CN=613E5688-3351-4C6B-BFCB-CFFE282F1F0A" --publisher-display Cosnuau
```

1. Owner: sign in to the existing Partner Center developer account (no new fee; PC games use the same account
   and the same dashboard as apps, category Games).
2. Owner: reserve the name "Vacuum Cleaner Simulator 2026" in Partner Center.
3. Package the Win64 build as MSIX with the identity from Partner Center (Product management > Product identity):
   `python tools\msix.py --identity-name <Package/Identity/Name> --publisher "<Package/Identity/Publisher>" --publisher-display <PublisherDisplayName>`. Uses makeappx.exe from the Microsoft.Windows.SDK.BuildTools
   NuGet package under `D:\DevTools\WindowsSDK-BuildTools`; the package is unsigned, the Store signs it.
4. Upload the MSIX, fill the listing with the copy above, the 300 x 300 icon and the 1920 x 1080 screenshots.
5. Xbox consoles need a UWP build (IL2CPP, Visual Studio with C++ UWP tools) through the Xbox Live Creators
   Program, or ID@Xbox with the GDK. Not part of the first release.

## Steam

1. Owner: open a Steamworks partner account (app fee, tax and identity forms).
2. Create the app, note the App ID, add it as `steam_appid.txt` next to the exe for local testing.
3. Integrate Steamworks.NET (achievements map one to one on `ObjectiveSystem` ids), rebuild.
4. Upload with SteamPipe from this machine (`steamcmd` + `app_build` script, kept in `tools/steam/`).
5. Store page: copy above, capsules from `marketing/store/`, screenshots from `docs/screenshots/`.
