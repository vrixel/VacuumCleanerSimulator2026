# Publishing

## Steam (PC)

- Build: `tools\build.ps1` produces a Windows 64-bit Mono player in `Builds\Win64`. That folder is what gets uploaded.
- Account: Steamworks partner (https://partner.steamgames.com), one-time app fee, identity and tax forms. Owner action.
- Integration: add Steamworks.NET later for achievements and the overlay. Achievements map one-to-one on `ObjectiveSystem` ids.
- Upload: SteamPipe (`steamcmd` + `app_build` script), run locally like everything else.
- Rating: Steam has no rating gate, but the store questionnaire should describe cartoon chaos only.

## Xbox

"Play Store" is Google's Android store. Xbox uses the Microsoft Store. Two routes:

1. **Xbox Live Creators Program** (self-service, small fee, no approval process).
   Build the game as a UWP app, publish on the Microsoft Store, it runs on Xbox consoles in the Creators section.
   Needs: the "Universal Windows Platform Build Support" Unity module, Visual Studio with the C++ UWP workload and a
   Windows SDK (IL2CPP is mandatory for UWP), a Partner Center account. No achievements or Gamerscore on this route.
2. **ID@Xbox** (Microsoft approval, free). Full Xbox features, uses the GDK and Unity's Xbox platform add-on,
   which Microsoft only hands to approved developers.

Plan: ship Steam first, apply to ID@Xbox with a Steam page and a trailer in hand, and use the Creators Program only if
the application stalls.

## Ratings

Target ESRB E / PEGI 7. Keep: cartoon physics, no blood, no language, no ads, no in-app purchases in v1.
