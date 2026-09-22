# App Store listing, ready to paste

The iOS record exists in App Store Connect (app 6809662067, `com.cosnuau.vacuumcleanersimulator2026`, iOS 1.0
"Prepare for Submission"). TestFlight is already running builds 402 / 404 / 407 on the internal group. Nothing
below has to wait for a tester: Apple has no tester requirement, unlike Google. What gates the public release is
the listing, the privacy answers and App Review.

Screenshots are rendered from the game itself: `tools\smoke-test.ps1 -Touch -Width 717 -Height 330 -Super 4`
(iPhone) and `-Width 688 -Height 516 -Super 4` (iPad); the player cannot open a window bigger than the monitor
(1366 x 768 over RDP), so the shots are captured at four times the window size. `python tools\store_shots.py
--only iphone,ipad` then composes the captions and the gallery into `marketing\appstore`: iPhone 6.5" 2688 x 1242
(`iphone65-NN-slug.png`, the version page refuses the 6.9" 2868 x 1320, which is kept as `iphone69-*`) and
iPad 13" 2752 x 2064 (`ipad13-NN-slug.png`), eight each, the same eight subjects as the other stores.

## App information

- Name: `Vacuum Cleaner Simulator 2026` (30 characters max, 28 used)
- Subtitle (30 max): `Suck it up. All of it.`
- Category: Games, primary subcategory Simulation, secondary Family
- Age rating answers: no violence, no realistic violence, no sexual content, no profanity, no alcohol, tobacco or
  drugs, no horror, no gambling, no contests, no unrestricted web access, no user-generated content, no messaging,
  no location sharing. Expected 4+.
- Copyright: `2026 Cosnuau`
- Support URL: `https://cosnuau.com/vacuum.html`
- Marketing URL: `https://cosnuau.com/vacuum.html`
- Privacy Policy URL: `https://cosnuau.com/vacuum-privacy.html`
- Price: Free, all territories
- App Privacy: no data collected. No tracking, no third-party SDK, no analytics, no advertising identifier; the
  only stored data is the local save (best score, achievements, garage choice) in UserDefaults on the device.
- Export compliance: no encryption beyond what the OS provides (the workflow already stamps
  `ITSAppUsesNonExemptEncryption = false` in the Info.plist).
- Sign-in: none. No demo account needed for review.

## Promotional text, description, keywords, What's New, review notes

All in `STORE-MATRIX.md` (2026-09-22, one source for the three stores): **S3** the subtitle (30 max, 22),
**PROMO** (170 max, 162), **D** with the touch paragraph **P-TOUCH** and no URL, **KW** (100 max, 97, no word of
the title), **WN** for 0.4.9, and the review notes. The version 1.0 page holds the matrix text since 2026-09-22 (promo 162,
description 2224 without the credits line, keywords 97, review notes 389; the React fields take the native value
setter plus input and change events, and Save turns on). Never name another game or another store in the
metadata: App Review treats a competitor's trademark as a rejection.

## The steps left, in order

1. Sign in to App Store Connect (the session expires quickly and the sign-in is the owner's).
2. App Information: subtitle, category, age rating questionnaire, privacy policy URL.
3. Pricing and Availability: Free, all territories.
4. App Privacy: "Data Not Collected", publish.
5. Version 1.0: description, keywords, promotional text, support URL, screenshots from `marketing\appstore`,
   the build (409), review notes, export compliance. DONE 2026-09-22 (screenshots: "Delete All" per device, then
   the eight files on the media input, fetched in-page from the temporary branch `ios-shots`; build: "Delete" on
   the 407 row, "Add Build", the 409 radio, Done, Save).
6. "Add for Review", then Submit. Review is usually a day or two; there is no tester requirement and no waiting
   period. Rejections at this stage are usually metadata, not code. The first "Add for Review" of 2026-09-22
   refused for one missing item, Content Rights in App Information: set to "contains third-party content, rights
   held" (CC-BY meshes, OFL fonts). Hidden-tab lesson (the Windows session was
   locked): the SPA's localisation request `/WebObjects/iTunesConnect.woa/ra/l10n-managed` times out at 10 s in a
   throttled hidden tab, so the header primary button and every dialog render with empty labels and nothing can be
   clicked with meaning. The console's own JSON API works from the page instead (`fetch` with `credentials:
   'include'`, Accept/Content-Type application/json, no CSRF header): `POST /iris/v1/reviewSubmissions`
   (platform IOS, relationship app) + `POST /iris/v1/reviewSubmissionItems` (relationships reviewSubmission and
   appStoreVersion) = "Add for Review" (done 2026-09-22, submission d0bffbf7-6a99-42d9-8155-519529054c1b,
   version READY_FOR_REVIEW); `PATCH /iris/v1/reviewSubmissions/{id}` with `attributes.submitted=true` = "Submit
   to App Review", which is exactly what the button sends (read in the bundle's `PatchReviewSubmissionAPI`) and
   which returned HTTP 504 from Apple's edge four times on 2026-09-22 (about 21 s each, state unchanged). SUBMITTED by him on
   2026-09-22 at 14:03 (App Review page, the draft row, "Submit to App Review"): status "Waiting for Review". Review takes a day or two, release is automatic after approval. The button lives on the App Review page (General, left column) inside the draft submission row, not on the version page, which only says "This app version has been added for review". Screenshot
   order (2026-09-22 afternoon, his "the graphic assets of marketplace are not the ones we worked on"): a
   multi-file drop uploads in arbitrary order, so both slots came out scrambled. Reorder = `PATCH
   /iris/v1/appScreenshotSets/{setId}/relationships/appScreenshots` with the ids sorted by
   `included[].attributes.fileName` (from `GET /iris/v1/appStoreVersionLocalizations/{loc}/appScreenshotSets?include=appScreenshots`),
   but it answers 409 STATE_ERROR "Can't Reorder Assets while Ready For Review" as long as the version sits in a
   review submission: `DELETE /iris/v1/reviewSubmissionItems/{itemId}` first, reorder, then `POST
   /iris/v1/reviewSubmissionItems` again (same item id comes back, submission still READY_FOR_REVIEW). Both slots
   now read 01-game ... 08-tutorial. The fused and action pictures are NOT in the App Store on purpose: guideline
   2.3.3 wants screenshots of the app in use, so only the captioned captures go there.
