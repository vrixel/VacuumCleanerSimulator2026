# Google Play listing copy and steps

Everything below is ready to paste into Play Console (the owner's existing developer account; PC and Xbox are in
`STORE.md`). The upload is an AAB from `tools\build-android.ps1 -Aab`, signed with the upload key made by
`tools\make-keystore.ps1` (`D:\Cloclo\Keys`, outside the repo; Play App Signing keeps the app signing key).

## Status

- 2026-09-07 evening: 402 (0.4.2) = the phone HUD after his first phone session (domed buttons, no plates, no
  radar or log, container icon, bigger garage arrows), uploaded to the internal track as a new release.
- 2026-09-07 19:06: INTERNAL TESTING LIVE with 401 (0.4.1): see-through walls, dust bag gauge, bin marker, touch
  wording of the empty prompt. Testers: email list "Owner" (the owner's address); opt-in link
  https://play.google.com/apps/internaltest/4701366885524013287 (open it with the tester's Google account, accept,
  then install from the Play Store; a fresh track can take up to an hour to show). The listing is not reviewed
  yet, so testers see the package name as a temporary app name. Next upload: bump `bundleVersion` (402+), build
  the AAB, "Create new release" on the internal track, or promote to production once the listing is complete.
- 2026-09-07: the app exists in Play Console (developer account 91Rivers, app id 4972315579361767663, package
  `com.cosnuau.vacuumcleanersimulator2026`, Game, Free, default language en-GB: change it in Store settings if
  wanted; the two creation declarations were ticked on the owner's "coche"). Internal testing holds a draft release
  "400 (0.4.0)" with the AAB uploaded and release notes written. Still to do: the testers email list (the owner's
  address) and the publish confirmation; both open dialogs, which need a visible tab (see the lessons below).
  Play App Signing needed no prompt: new apps are enrolled with a Google-generated key by default.

## App

- Name: `Vacuum Cleaner Simulator 2026` (30 characters max: 28)
- Package: `com.cosnuau.vacuumcleanersimulator2026`
- Category: Game > Simulation. Free, no ads, no in-app purchases, no account, no data collected.
- Privacy policy: https://cosnuau.com/vacuum-privacy.html
- Target audience: 13 and over is the simplest path (a "designed for families" declaration adds the Teacher
  Approved review). Content rating: IARC questionnaire, answer no to everything (cartoon appliance chaos, no
  violence, no language): expect Everyone / PEGI 3.
- Data safety: no data collected, no data shared, no encryption needed, no deletion request path needed.

## Store listing

Short description (80 max, 79):

    You are the vacuum. The house is filthy. Eat socks, chairs, the cat's dignity.

Full description:

    Vacuum Cleaner Simulator 2026 is a physics sandbox in the spirit of Goat Simulator, except you are a vacuum
    cleaner. Drive through a messy house, suck up crumbs, socks, toy bricks and coins, grow your power until you
    can eat chairs, lamps and eventually the toilet, then blow it all back out. Chase the cat. Leave a clean trail
    through the cocoa powder. Rewind your cord from across the house and watch the plug whip through the furniture.

    Nineteen vacuums in the garage: eight built from scratch and eleven real machines on loan, each with its own
    handling, bag and personality, from a smiling red canister to a workshop drum and a French sled that glides.

    A cockpit-grade HUD on an arcade cabinet: suction gauges, motor readouts, annunciator lamps, a dirt radar,
    a mission log with 22 achievements. Boost with sparks and speed lines. Bonus splashes that never cover your
    vacuum.

    Touch controls: left stick to drive, right pads to hop, boost, blow, empty and rewind, drag the free part of
    the screen to look around. Family friendly, no ads, no accounts, nothing collected. Saves your best score,
    achievements and garage choice on the phone.

    All vacuum names are parodies; no real product names or logos are used. The eleven real-machine meshes are
    Creative Commons models, credited at github.com/vrixel/VacuumCleanerSimulator2026/blob/main/docs/CREDITS.md.

## Assets (from `tools\marketing.py` and the touch smoke run)

| Asset | Requirement | File |
|---|---|---|
| App icon | 512 x 512 PNG, no alpha needed | `marketing\icon\icon_512.png` |
| Feature graphic | 1024 x 500 PNG or JPEG | `marketing\play\feature_1024x500.png` |
| Phone screenshots | 2 to 8, 16:9 to 9:16, 320 to 3840 px | `marketing\play\phone-*.png` (1920 x 1080, touch layer on) |
| 7-inch tablet screenshots | optional | same files |

Phone screenshots come from `powershell -File tools\smoke-test.ps1 -Touch -Width 1920 -Height 1080` (the touch
layer on the PC build; the picture is the same renderer as the phone, minus the effects the phone drops).

## Steps

1. Play Console > Create app: name, default language English (United States), Game, Free, declarations.
2. App content: privacy policy URL, ads (no), app access (all features available without special access),
   content rating questionnaire, target audience, news app (no), COVID (no), data safety (nothing collected),
   government apps (no), financial features (no), health (no).
3. Store listing: texts above, icon, feature graphic, phone screenshots.
4. Release > Production (or Internal testing first) > Create release: Play App Signing (default), upload the AAB,
   release name = the version, release notes = the "What's new" of the matching Windows release. Review and roll out.
5. Review takes from a few hours to a few days for a new app.

## Console lessons (2026-09-07)

- The AAB goes through the page, not through a file picker: fetch it from a CORS-enabled URL, wrap the blob in a
  `File`, set it on `input[type=file][accept=".aab"]` with a `DataTransfer` and dispatch `change`. The fetch runs
  asynchronously (store the promise on `window`, poll it): 45 MB took about 100 s. `raw.githubusercontent.com`
  sends `Access-Control-Allow-Origin: *`; release assets (`release-assets.githubusercontent.com`) do not. The raw
  URL came from a temporary branch made with git plumbing (`hash-object --no-filters`, `mktree`, `commit-tree`,
  `push origin <sha>:refs/heads/aab-upload`) and deleted right after the upload, so nothing touched `main`.
- Play Console's material buttons ignore synthetic clicks for anything that opens a dialog (Create email list, the
  publish confirmation) and a hidden tab never renders a dialog at all: a locked Windows session makes Chrome treat
  every window as occluded, `requestAnimationFrame` stalls, hit-testing stops (a `MessageChannel` rAF polyfill is not
  enough). Unlock first, then bring the tab in front without stealing focus (`win.ps1` in the session scratchpad:
  `place` = restore + topmost + no-activate, `untop` + `max` afterwards).
- Text fields and radios accept DOM writes (`form_input`, the native value setter plus an `input` event); "Check
  availability" and "Next" work as plain element clicks; release notes take `<en-GB>...</en-GB>` tags (500 chars).

## Versioning

`ProjectSetup` derives the Android version code from `bundleVersion` (0.4.0 -> 400); Play refuses a code that does
not grow, so every upload needs a higher `bundleVersion`.
