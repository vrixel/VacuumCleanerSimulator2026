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
the title), **WN** for 0.4.9, and the review notes. What the version 1.0 page holds today is the 0.4.7 text ("New
in 0.4.7", "twenty silly achievements"): replace it from the matrix before "Add for Review". Never name another
game or another store in the metadata: App Review treats a competitor's trademark as a rejection.

## The steps left, in order

1. Sign in to App Store Connect (the session expires quickly and the sign-in is the owner's).
2. App Information: subtitle, category, age rating questionnaire, privacy policy URL.
3. Pricing and Availability: Free, all territories.
4. App Privacy: "Data Not Collected", publish.
5. Version 1.0: description, keywords, promotional text, support URL, screenshots from `marketing\appstore`,
   the build (pick 407), review notes, export compliance.
6. "Add for Review", then Submit. Review is usually a day or two; there is no tester requirement and no waiting
   period. Rejections at this stage are usually metadata, not code.
