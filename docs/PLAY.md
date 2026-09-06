# Google Play listing copy and steps

Everything below is ready to paste into Play Console (the owner's existing developer account; PC and Xbox are in
`STORE.md`). The upload is an AAB from `tools\build-android.ps1 -Aab`, signed with the upload key made by
`tools\make-keystore.ps1` (`D:\Cloclo\Keys`, outside the repo; Play App Signing keeps the app signing key).

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

## Versioning

`ProjectSetup` derives the Android version code from `bundleVersion` (0.4.0 -> 400); Play refuses a code that does
not grow, so every upload needs a higher `bundleVersion`.
