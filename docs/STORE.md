# Store listing copy and submission checklists

Everything below is ready to paste. Submissions go through the owner's accounts; nothing here can be submitted by
Claude. The owner already has a Microsoft Partner Center developer account (an app is published): that one
registration covers every Windows app and game, PC included, so the Store costs nothing more. Steam is different:
Steamworks charges 100 USD per game (Steam Direct fee, refunded once the game earns 1,000 USD).

## Listing copy

The words live in `STORE-MATRIX.md`, one source for the three stores (2026-09-22): title, the Microsoft Store
short description **S1**, the description **D** with the PC paragraph **P-PC** and the credits line, the release
notes **WN**, the certification notes. Paste from there. Store-specific fields:

**Tags:** Simulation, Physics, Casual, Comedy, Sandbox, Family Friendly, Singleplayer, Controller Support

**Age rating answers (IARC / ESRB / PEGI questionnaire):** no violence, no blood, no sexual content, no language,
no drugs, no gambling, no user interaction, no data collection beyond local save files, no in-app purchases.
Expected: ESRB E, PEGI 3.

## Marketing assets (marketing/)

Key art and icon generated with kie.ai (`tools/assets/kie_assets.py`, `tools/assets/marketing_real.py`) and cut to
store sizes by `tools/marketing.py`; screenshots, gallery, wordmark and trailer rendered by the game itself
(`tools/store_shots.py`, `tools/wordmark.py`, `tools/record.ps1` + `tools/store_video.py`, all on `tools/brand.py`).

| File | Size | Used for |
|------|------|----------|
| `marketing/store/screens/NN-slug.png` (10) | 1920 x 1080 | Store screenshots, captioned in the HUD's typography |
| `marketing/store/gallery_1920x1080.png` | 1920 x 1080 | the nineteen machines side by side (screenshot 02) |
| `marketing/video/trailer_1920x1080.mp4` | 1920 x 1080, under 60 s | Store trailer (also YouTube for Play) |
| `marketing/logo/wordmark.png`, `wordmark-line.png` | transparent | the title on gallery, feature graphic, video cards, site |
| `marketing/icon-candidates/sheet.png` | contact sheet | the icon choice (his), see `STORE-MATRIX.md` |
| `marketing/store/ms_superhero_1920x1080.png`, `ms_poster_720x1080.png`, `ms_boxart_1080.png` | as named | Store super hero, poster, box art |
| `marketing/store/ms_icon_300.png` | 300 x 300 | Microsoft Store icon |
| `marketing/store/key_art.png` | source | press, GitHub, cosnuau.com |
| `marketing/icon/icon_*.png`, `marketing/icon/icon.ico` | 16 to 1024 | app icon, exe icon, site favicon |
| `marketing/store/steam_*.png` | Steam sizes | parked with Steam |

## GitHub release

`tools\release.ps1 -Version 0.1.0` builds, zips `Builds\Win64` and publishes a GitHub release with the zip.

## Microsoft Store (PC first, Xbox later)

**Status 2026-09-22 evening: submission 7 (id 1152921505701954365) adds THE TRAILER to the live 0.4.9 listing,
everything else unchanged (package v0.4.9.0 validated). SUBMITTED for certification about 16:40 UTC on his
"finalise", status "In certification" (pre-processing). How the trailer pane took the files, in a VISIBLE tab:
the mp4 (fetched in page from the temporary branch, wrapped in a `File`) goes on the hidden
`#video-upload-input` with a dispatched `change` AND a `DragEvent('drop')` carrying the same `DataTransfer` on the
pane's drop zone; each of the two fires once, so the table showed the trailer twice and one row was deleted
(row checkbox, Delete, "From this Store listing only"). The thumbnail needs the SAME pair on the pane's own
`input#image-upload-input` (set the files, dispatch `change`, then the drop on its `.asset-card`): the drop alone
did nothing. Then "Add image caption" (required; caption "Vacuum Cleaner Simulator 2026 trailer"), OK by a DOM
click on the dialog's button (a screen click on it closed the whole pane and lost the thumbnail), close the pane
with its X (in the `he-fly-in-panel` shadow root, a screen click at its top-right corner), choose the trailer in
"Choose a trailer to play at the top of your Store listing", Save at screen coordinates (lands on the Game
overview). The trailer and the choice survive a reload. "Submit for certification" needed a real screen click this
time (the DOM click on the freshly rendered he-button did nothing). Temporary branch `msix-upload` deleted.**

**Status 2026-09-22: submission 6 (id 1152921505701949322) carries v0.4.9.0 (tutorial, store links, rating
nudge, the navy icon) with the matrix texts (`STORE-MATRIX.md`: **S1** as the short title, **D** + **P-PC** and the
credits line, 2362 characters, **WN** as the release notes; LIVE in the public catalogue at 10:27 UTC the same
morning, certification took about an hour), ten desktop screenshots (the eight fused pictures
01-08, then 01-game and 08-tutorial of `marketing/store/screens`), poster, box art, 300 px icon and super hero from
the navy set; 0.4.7.0 removed. SUBMITTED for certification about 09:30 UTC with the Windows session LOCKED: every
click was a DOM click, every upload an in-page fetch from the temporary branch `msix-upload`. Lessons: (1) the
desktop screenshot slot caps at TEN, the overflow of a twelve-file drop lands in the Xbox tab, which then has to
be emptied (Xbox screenshots on a PC-only listing are a certification warning); (2) the trailer pane
(`#video-upload-input` after the visible "Upload" button of "Trailers and additional assets") never processes a
video in a hidden tab, so the trailer waits for submission 7 in a VISIBLE tab after his unlock
(`img6_video_trailer_1920x1080.mp4` + `img6_video_trailer_poster.png` are still on `msix-upload`, keep the branch
until then); (3) "Submission options" is a span router link with no href and the direct URL redirects to the
overview: not needed, the certification notes carry over from earlier submissions.**

**Status 2026-09-09 evening: submission 5 (id 1152921505701850054) carries v0.4.7.0 (one size family in the garage,
the turbo reminder, the garage-selector fix, the brand names painted out of two meshes), notes in `#releaseNotes`,
0.4.4.0 removed; SUBMITTED for certification at about 19:10 UTC on his "Push everywhere". Same routine as
submission 4 and no surprises: MSIX on a temporary branch, in-page fetch, one screen-coordinate click per Save,
"Submit for certification" as a plain element click.**

**Status 2026-09-08 evening: submission 4 (id 1152921505701838982) carries v0.4.4.0 (Win64 build 0.4.4: coloured
gamepad buttons in the hints, Henry scale, museum nozzles and yaw, plus the 0.4.1-0.4.2 see-through walls, bag
gauge and bin marker that had only shipped on GitHub), release notes "0.4.4: ..." in `#releaseNotes`, the
0.4.1.0 package removed (Save on the Packages page confirms the removal), everything else unchanged; SUBMITTED
for certification at about 18:00 UTC on the owner's "Allez publie tout" (the "Submit for certification" he-button
took a plain element click; a hidden "Sign in required" dialog sits in the DOM at all times and means nothing
while it is not displayed). Both Save buttons (Packages, Store listing) again needed a click at screen coordinates
and each one lands on the Game overview page when it succeeds. LIVE: the public catalog listed
`Cosnuau.VacuumCleanerSimulator2026_0.4.4.0_x64__be04n9vkbk9wc` at 18:48 UTC, about 50 minutes after the submission.**

**Status 2026-09-06 night: submission 1 (v0.1.1.0) live on 2026-09-05; submission 2 (v0.2.0.0) live on 2026-09-06
(mail 14:36 UTC, about 19 h in certification; the public catalog
`https://displaycatalog.mp.microsoft.com/v7.0/products/9P9HVRJ09PK0?market=US&languages=en-us` lists the package
full names, a clean signal to poll); submission 3 (id 1152921505701822812) carries v0.4.1.0 (the 0.4.0 build packaged again with the final icon: two
packages with the same full name and different contents are refused even when one is marked for removal, so the
MSIX version was incremented),
the 0.4.0 release notes and the racing-style poster, box art, super hero and tile icon (uploaded by dropping the
file on each slot's input, no trash needed); everything else unchanged; SUBMITTED for certification on 2026-09-06 about 18:30 UTC on the owner's go (the
"Submit for certification" he-button responds to a plain element click); LIVE in the public catalog at 19:58 UTC, about
90 minutes later.**
Lessons of submission 3: raw.githubusercontent.com caches a path for minutes, so a replaced file needs a new name
(`_v5`, `_b`); the in-page `fetch` of the MSIX keeps running after the browser tool times out at 45 s, so
never launch it twice (the second run made a duplicate package that had to be removed); the Save buttons of the
Packages and Store listing pages ignore clicks by accessibility reference and need a click at screen coordinates
(take a screenshot, click the button); the English listing lives at
`.../submissions/<id>/listings?languageid=4&languagecode=en-us` and its release notes are `#releaseNotes` (1500 chars). Package upload without a drag: push the
MSIX to a temporary branch, then in the Packages page run a script that fetches it from raw.githubusercontent.com
(CORS `*`, no CSP on Partner Center), wraps it in a `File`, sets it on the `input[type=file]` and dispatches
`change`; delete the branch afterwards. GitHub release assets and localhost do not work (no CORS header; local
network access prompt). Submission 1 was, free, all 240 markets, public, publish
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
5. Xbox consoles: the Creators Program / UWP route is closed (see `PUBLISHING.md`); the only route is ID@Xbox
   with the GDK, which needs Microsoft's approval, their Unity Xbox module and a C++ toolchain. The Store package
   page's "Windows 10/11 Xbox" column cannot be ticked for a Win32 MSIX. Not buildable on this machine.

## Steam

1. Owner: open a Steamworks partner account (app fee, tax and identity forms).
2. Create the app, note the App ID, add it as `steam_appid.txt` next to the exe for local testing.
3. Integrate Steamworks.NET (achievements map one to one on `ObjectiveSystem` ids), rebuild.
4. Upload with SteamPipe from this machine (`steamcmd` + `app_build` script, kept in `tools/steam/`).
5. Store page: copy above, capsules from `marketing/store/`, screenshots from `docs/screenshots/`.
