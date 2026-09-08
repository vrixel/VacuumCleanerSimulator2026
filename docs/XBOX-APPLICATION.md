# ID@Xbox application, ready to paste

The Xbox Store no longer accepts UWP games (November 2025), so the only console route is ID@Xbox: Microsoft
approves the studio, then hands out the GDK with Xbox extensions, the Unity Xbox platform module and dev-kit
access. Nothing can be built for the console before that. The form lives at
https://www.xbox.com/developers/id ("Apply now", signed in with the Microsoft account of the Partner Center
tenant that already publishes the PC version). Below is an answer for every field the form asks, in the order it
asks them; the three marked OWNER need a value only the owner has.

## Studio

- Studio / company name: Cosnuau
- Country: OWNER (the Partner Center account's country)
- Website: https://cosnuau.com
- Contact name, email, phone: OWNER (the Partner Center account holder; the store contact email is the one on the
  Partner Center account)
- Partner Center publisher: yes, publisher display name "Cosnuau", the PC game is already live on the Microsoft
  Store as product 9P9HVRJ09PK0 (https://apps.microsoft.com/detail/9P9HVRJ09PK0)
- Team size: 1
- Previous releases: Vacuum Cleaner Simulator 2026 on the Microsoft Store (PC, since 2026-09-05), Google Play
  (closed test) and TestFlight (iOS beta); Amityville: 112 Ocean Avenue (web and store builds) by the same studio.

## Game

- Title: Vacuum Cleaner Simulator 2026
- Genre: Simulation / physics sandbox / comedy
- Elevator pitch: You are the vacuum cleaner. The house is filthy. Eat crumbs, socks, chairs and eventually the
  toilet, then blow it all back out. A silly physics sandbox with a suspiciously serious cockpit.
- Description: Vacuum Cleaner Simulator 2026 is a physics sandbox in the spirit of Goat Simulator, except you are a
  vacuum cleaner and the mess is not going to clean itself. Drive through a six-room house, suck up crumbs, dust
  bunnies, socks, toy bricks and coins, then level up until chairs, the couch, the fridge and the toilet fit in the
  nozzle. When the bag is full, blow everything back out at high speed and start again. Nineteen vacuums with real
  handling differences, a simulated power cord that pulls tight and pops out of the wall, a cockpit with a suction
  gauge, a real dust bag that fills up, warning tiles and a dirt radar, and twenty silly achievements.
- Target audience and rating: family friendly, no violence, no language, no ads, no purchases; IARC PEGI 3 / ESRB
  Everyone already obtained for the Microsoft Store and Google Play listings.
- Engine: Unity 6 (6000.3), C#, everything generated at runtime (no prefabs, no art pipeline). Controller support
  is complete on the Xbox layout (A/B/X/Y/LB/RB/Start, colour-coded button hints) since the PC version targets Xbox
  controllers first.
- Platforms shipped: Windows PC (Microsoft Store, GitHub release). In test: Android (Google Play closed test),
  iOS (TestFlight).
- Platforms planned: Xbox Series X|S (this application), Xbox One if the GDK build fits.
- Development status: playable and released on PC (version 0.4.4); console port not started (needs the GDK).
- Release window on Xbox: 3 to 6 months after GDK access (port, certification requirements, achievements).
- Multiplayer: none. Online features: none. Cross-play: none. Xbox Live features planned: achievements mapped on
  the existing twenty objectives, Gamerscore, cloud saves.
- Business model: premium, low price (the PC version is free during the beta; the console price will be set with
  the PC one at 1.0).
- Publisher: self-published.
- Trailer / video: none yet. Screenshots: https://github.com/vrixel/VacuumCleanerSimulator2026 (README) and the
  Microsoft Store page. If the form insists on a video, record 60 s of the PC build with the Xbox Game Bar
  (Win+Alt+R) driving through the house, sucking the couch, blowing out and pulling the plug; upload as unlisted
  on YouTube and paste the link.
- Press kit / build: the PC build zip on
  https://github.com/vrixel/VacuumCleanerSimulator2026/releases/latest (Windows 64-bit, unzip and run).
- Source code: https://github.com/vrixel/VacuumCleanerSimulator2026 (public, all rights reserved, source for
  reference)

## After approval

1. Microsoft sends the GDK/GDKX download and the Unity Xbox platform module through the ID@Xbox portal; the
   Unity module installs on this editor by the same manifest trick as `tools/install-android.py` once its URL is
   known (the Hub cannot add it).
2. This machine has no C++ toolchain; the console build needs Visual Studio with the "Desktop development with
   C++" workload (IL2CPP is mandatory on console), about 8 GB, installable on D:.
3. Dev kit or the Xbox "developer mode" on a retail console for testing.
4. Certification requirements (XR) to plan for: suspend/resume, sign-in, storage, controller disconnect, all
   handled in `GameManager` and `GameInput`.
