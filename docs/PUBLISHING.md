# Publishing

## Steam (PC)

- Build: `tools\build.ps1` produces a Windows 64-bit Mono player in `Builds\Win64`. That folder is what gets uploaded.
- Account: Steamworks partner (https://partner.steamgames.com), one-time app fee, identity and tax forms. Owner action.
- Integration: add Steamworks.NET later for achievements and the overlay. Achievements map one-to-one on `ObjectiveSystem` ids.
- Upload: SteamPipe (`steamcmd` + `app_build` script), run locally like everything else.
- Rating: Steam has no rating gate, but the store questionnaire should describe cartoon chaos only.

## Xbox

"Play Store" is Google's Android store. Xbox uses the Microsoft Store. There used to be two routes; one is closed:

1. **Xbox Live Creators Program / UWP** (self-service): CLOSED. Since November 2025 the Xbox Store no longer accepts
   UWP games (Microsoft Q&A), and the PC MSIX of this game is a Win32 desktop package (`runFullTrust`), which the
   Partner Center package page cannot offer to the "Windows 10/11 Xbox" device family. Do not install the UWP
   toolchain for it.
2. **ID@Xbox + GDK** (Microsoft approval, free): the only route. The owner applies at
   https://www.xbox.com/developers/id with the Store page and a trailer; once approved, Microsoft hands out the
   GDK with Xbox extensions (GDKX), the Unity "Xbox Series X|S" platform module (not in the public release
   manifest: `tools/install-android.py` cannot fetch it) and dev-kit access. Building then also needs Visual Studio
   with the C++ workload (IL2CPP is mandatory on console), which this machine does not have.

Status 2026-09-08 (his "construis la version Xbox"): no Xbox build is possible here. Everything that can be prepared
without the GDK is done: the game runs on the Xbox controller layout (`GameInput`, coloured button hints), the
Store listing exists (PC), and the code has no platform-specific dependency beyond uGUI and Post Processing v2.
Next human step: the ID@Xbox application.

## Ratings

Target ESRB E / PEGI 7. Keep: cartoon physics, no blood, no language, no ads, no in-app purchases in v1.
