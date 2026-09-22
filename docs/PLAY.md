# Google Play listing copy and steps

Everything below is ready to paste into Play Console (the owner's existing developer account; PC and Xbox are in
`STORE.md`). The upload is an AAB from `tools\build-android.ps1 -Aab`, signed with the upload key made by
`tools\make-keystore.ps1` (`D:\Cloclo\Keys`, outside the repo; Play App Signing keeps the app signing key).

## Status

- 2026-09-22 evening ("finalise", session unlocked): 409 (0.4.9) DONE ON PLAY. The AAB went up on the internal
  track, the release was promoted to the closed test Alpha ("start full rollout"), and the listing was refreshed
  from the matrix: **S2** as the short description (76 characters), **D** + **P-TOUCH** + the credits line as the
  full description (2438), the navy icon (`marketing/icon/icon_512.png`, his "N'oublie pas l'icône de l'app": the
  yellow one was still in the slot), the navy feature graphic, phone pictures = the six fused pictures 01-toilet,
  02-garage, 03-cat, 04-turbo, 05-cord, 06-blowout then phone-01-game and phone-08-tutorial, and the eight touch
  captures phone-1..8 in both tablet slots. "Send 8 changes for review" confirmed: the Publishing overview shows
  "Changes in review" (the Alpha rollout plus seven listing changes). Lessons: the order of a slot is the order of
  the individual Add clicks, a multi-file drop lands in processing order, so add one asset per Add; the footer
  Save button is laid out off screen but a DOM click on `button[debug-id="main-button"]` works and the "Go to
  Publishing overview?" dialog confirms the save. Temporary branch `aab-upload` deleted. Production access stays
  his click after the 14-day closed test (about 2026-09-25), questionnaire answers below.
- 2026-09-22 ("take their feedback in consideration and implement the recommendations"): THE TESTERS COMMUNITY
  REPORT IS IN (mail of 2026-09-15, two PDFs, copies in Drive under 91 Rivers Labs / Active Projects /
  VacuumCleanerSimulator: the feedback report and the "Production Access Questionnaire" answers). Verdict: no
  crash, no bug, stable; four recommendations, all built into 0.4.9 or into the store copy (see "Tester report"
  below). The paid run ends around 2026-09-25; "Apply for production access" (Play Console dashboard) is then the
  owner's click, with the questionnaire answers below ready to paste.

- 2026-09-09 21:15 ("Push everywhere"): 407 (0.4.7) live on the internal track and sent for review on the closed
  track "Alpha", where the paid testers are. Same routine as 404. The paid run keeps going: a new build on a closed
  track does not restart Google's 14-day clock, it is the track staying active with opted-in testers that counts.

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

## Tester report (Testers Community, 2026-09-15)

Twelve to fifteen paid testers on the closed track "Alpha" for 14 days (builds 404 then 407). Their report:

- Stability: no crashes, no bugs, no ANR; performance fine on the devices used.
- Recommendation 1, ASO: a keyword-rich store description with a fuller account of the gameplay. DONE in the copy
  below (2026-09-22), the same text on the three stores (`STORE.md` for Microsoft, `APPSTORE.md` for Apple).
- Recommendation 2, screenshots: show objects being vacuumed, several vacuum models, tricks, several rooms, with
  captions and annotations. DONE with `tools\store_shots.py` (captioned frames over the smoke shots), see the store
  matrix in `STORE-MATRIX.md`.
- Recommendation 3, an interactive first-launch tutorial with progressive disclosure and context hints. DONE in
  0.4.9: `Tutorial` (six steps on the hint line, each one a real action: drive, look, absorb, hop, boost, blow;
  runs once, PlayerPref `tutorial_done`; SKIP / REPLAY TUTORIAL in the pause menu). The bin prompt, the bag-full
  toast and the boost reminder were already context hints and stay.
- Recommendation 4, a "Rate your app" button linking to the store, plus an in-app review prompt after achievements
  with neutral timing and no sentiment filtering. DONE in 0.4.9: RATE THIS GAME on the title screen and in the
  pause menu (`StoreLinks`: the Play page on Android, the App Store write-review link on iOS, the Microsoft Store
  page on Windows), SEND FEEDBACK on the title screen (mailto cosnuau@gmail.com with the version in the subject),
  and one toast ever ("ENJOYING THE CHAOS?", after four minutes of play and three achievements, for everyone alike,
  PlayerPref `rate_nudged`). The Play In-App Review API was NOT used: it needs the Play Core package, and the store
  page is the same door on every platform.
- Extras they listed, not built: seasonal content, leaderboards and social sharing, accessibility settings,
  performance monitoring. Seasonal content and new levels are in `IDEAS.md` waiting for the owner's go.

### Production access questionnaire, answers ready to paste

Google asks these on "Apply for production access". The testers' PDF suggested answers; the ones below are
adapted to what is true of this app.

1. How did you recruit the testers? — "Through a paid closed-testing service (Testers Community, 12 to 15 testers,
   14 days) recruited to match the target audience: casual players and simulation fans, on a range of Android
   phones and tablets."
2. How easy was it to recruit them? — "Easy: the service supplied the testers within hours; the opt-in link and the
   Google Group were set up in Play Console the same day."
3. How engaged were they? — "Actively: they played through the closed track for the full 14 days, reported on
   stability, performance and usability, and sent a written report with recommendations."
4. What feedback did you get and how? — "Through the service's report and survey. Feedback: optimise the store
   description (ASO), redesign the screenshots to show the gameplay, add a first-launch walkthrough, add a
   rate-the-app button. No crashes or bugs were reported."
5. Who is the app for? — "Casual players of all ages who like physics sandboxes and silly simulations. Family
   friendly: no violence, no ads, no purchases, no account."
6. What makes it valuable to them? — "A unique comedy simulation where the player IS the vacuum cleaner: a house
   full of mess, nineteen machines with real handling differences, a physical power cord, a cat to chase. Offline,
   free, with nothing collected."
7. How many installs do you expect in the first year? — pick the honest bracket. "1,000 - 10,000" is the right
   one for a free game with no marketing budget (the testers' draft said 10k - 100k; do not overclaim).
8. What did you change after the closed test? — "We rewrote the store description around the gameplay and its
   keywords, redesigned the screenshots with captions showing the mess, the machines and the tricks, added a
   six-step interactive walkthrough on first launch (with skip and replay in the pause menu), added Rate This Game
   and Send Feedback buttons and one neutral rating invitation after a few achievements. Version 0.4.9."
9. Why is the app ready for production? — "It has been stable on the internal and closed tracks since 0.4.2 with
   no crash or ANR reported over the 14-day paid test; the testers' recommendations are shipped in 0.4.9; the
   listing, content rating, data safety and privacy policy are complete."
10. What did you learn from the test? — "The game itself held up; what testers wanted was onboarding and store
    presentation: a tutorial, clearer screenshots and a way to rate and send feedback. Those shaped 0.4.9."

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

The words live in `STORE-MATRIX.md` (2026-09-22, one source for the three stores): the Play short description
**S2** (80 max, 76), the description **D** with the touch paragraph **P-TOUCH** and the credits line, the release
notes **WN**. Play has no keyword field: the words of **D** are the keywords, which is why it names the mess, the
machines, the cat, the cord and the cockpit in plain words.

## Assets (from `tools\store_shots.py` over the touch smoke captures)

| Asset | Requirement | File |
|---|---|---|
| App icon | 512 x 512 PNG, no alpha needed | `marketing\icon\icon_512.png` (his pick pending, `STORE-MATRIX.md`) |
| Feature graphic | 1024 x 500 PNG or JPEG | `marketing\play\feature_1024x500.png` (wordmark over the game) |
| Phone screenshots | 2 to 8, 16:9 to 9:16, 320 to 3840 px | `marketing\play\phone-NN-slug.png` (8, 1920 x 1080, touch HUD, captioned) |
| 7-inch tablet screenshots | optional | same files |
| Video | YouTube link | `marketing\video\trailer_1920x1080.mp4`, uploaded unlisted |

Phone screenshots: `powershell -File tools\smoke-test.ps1 -Touch -Width 960 -Height 432 -Super 2` (the touch layer
on the PC build, captured at twice the window because the RDP display is 1366 x 768), then
`python tools\store_shots.py --only phone,feature` composes the captions and the gallery.

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
