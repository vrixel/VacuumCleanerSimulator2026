# Google Play listing copy and steps

Everything below is ready to paste into Play Console (the owner's existing developer account; PC and Xbox are in
`STORE.md`). The upload is an AAB from `tools\build-android.ps1 -Aab`, signed with the upload key made by
`tools\make-keystore.ps1` (`D:\Cloclo\Keys`, outside the repo; Play App Signing keeps the app signing key).

## Status

- 2026-09-09 ("go testerscommunity"): THE 12-TESTER RUN IS BOOKED. On testerscommunity.com (his account,
  cosnuau@gmail.com, one Starter credit already paid) the app was submitted: name, plan Starter (15 testers), the
  join link https://play.google.com/apps/testing/com.cosnuau.vacuumcleanersimulator2026, the 256 px icon (fetched
  in-page from media.githubusercontent.com, which serves the real bytes of an LFS file with CORS, unlike
  raw.githubusercontent.com which returns the pointer text) and a note saying the game needs no login. Day 0 of 16,
  credit balance now zero. Their step 1 was done first in Play Console: the closed track "Alpha" now takes its
  testers from the Google Group testers-community@googlegroups.com. CAREFUL: Play's Testers tab is a radio between
  "Email lists" and "Google Groups", so switching to the group DROPPED the Owner and Testers 91rivers lists from
  the CLOSED track; the owner keeps his own access through the INTERNAL track, which still carries 404 and both
  lists. The change went to review from the Publishing overview (one change, "Set testers to be managed by Google
  Groups"). Countries were already at the maximum: the track filter reads "All countries/regions (177), Targeted
  (177), Not targeted (0)", so 177 IS every country Play offers here.

- 2026-09-08 20:00 ("Allez publie tout"): 404 (0.4.4) LIVE on the internal track (new release, AAB fetched in-page
  from a temporary branch, "Save and publish" + confirmation at screen coordinates), then promoted to the closed
  track "Alpha" (approved overnight: 404 reads "Available to selected testers", released 8 Sept 20:20) ("Promote release" > Closed testing > Alpha copies the bundle and the notes; the review page ends
  with "Save", the "Go to Publishing overview?" dialog, then "Submit 1 change for review" + "Send changes for
  review"): IN REVIEW. The 16 changes of 2026-09-07 had been approved the same night: 402 went live on the closed
  track at 22:34 on 2026-09-07 ("Available to selected testers"), so the 14-day clock can run as soon as 12 testers
  opt in. Tester links (Testers tab of the closed track): web
  https://play.google.com/apps/testing/com.cosnuau.vacuumcleanersimulator2026, Android
  https://play.google.com/store/apps/details?id=com.cosnuau.vacuumcleanersimulator2026 (the tester's address must
  be on the Owner or Testers 91rivers list). Version codes: 401 and 402 internal, 402 closed, 404 both.
  The 12 testers: the owner buys a run on https://www.testerscommunity.com (sign-in and payment are his,
  from 15 USD / 14 EUR, 12 testers for 14 days, testing starts within 6 hours). Their process, from their
  FAQ: submit the app on their dashboard with the opt-in link above, copy their testers Google Group address
  from the dashboard, then in Play Console > Testing > Closed testing > Alpha > Testers tab > Google Groups
  section, add that address (or hand it over and it gets added from here); their testers accept the invite
  and the 14-day clock runs from the day 12 are opted in. Then "Apply for production access" on the dashboard.

- 2026-09-07 late evening ("Fini la publication Android"): the app is fully set up in Play Console. Store
  listing saved (name, short and full description, icon, feature graphic, 8 phone screenshots from the touch smoke
  run at 1080p), store settings (Game > Simulation, contact email and website), every declaration done (privacy
  policy, app access: none restricted, ads: none, content rating IARC: PEGI 3 / Everyone, target audience 13+, data
  safety: nothing collected, advertising ID: none, government: no, financial: none, health: none). The closed
  testing track "Alpha" carries release 402 (0.4.2), 177 countries, testers = email lists Owner + Testers 91rivers,
  feedback cosnuau@gmail.com; everything submitted for review from the Publishing overview. PRODUCTION IS GATED:
  this developer account must run a closed test with at least 12 testers opted in for 14 continuous days, then
  "Apply for production access" on the dashboard (Google's rule for personal accounts created after November 2023).
  The two lists hold 4 addresses: 8 more testers are needed for the 14-day clock to count.

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
- Store listing images go through the asset library: click the slot's "Add assets", then set the file on the
  panel's `input[type=file]` (a `DataTransfer` + `change`): the upload lands in the library and is auto-selected
  for the slot; click the panel's "Add" (bottom right, real click) to attach it. Uploads made while another slot's
  panel was open only fill the library. Library rows do not respond to synthetic clicks, so re-uploading is the
  reliable way to select. Text fields of the listing accept the native value setter; "Save" works as a plain click.
- Every wizard (data safety, financial, health, content rating, target audience) has a last "Save" that opens
  "Go to Publishing overview? Your change has been saved": that dialog text is the only proof of a save. The word
  "saved" alone also appears in "If you save, changes will be saved" and misled a first pass; the IARC questionnaire
  needs its terms checkbox and the summary Save on every reopen.
- Store settings: category and contact details are two dialogs, each with its own "Save and publish" and a
  "Publish change on Google Play?" confirmation; the page-level Save only saves the dialog that is open.
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
