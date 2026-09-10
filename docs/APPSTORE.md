# App Store listing, ready to paste

The iOS record exists in App Store Connect (app 6809662067, `com.cosnuau.vacuumcleanersimulator2026`, iOS 1.0
"Prepare for Submission"). TestFlight is already running builds 402 / 404 / 407 on the internal group. Nothing
below has to wait for a tester: Apple has no tester requirement, unlike Google. What gates the public release is
the listing, the privacy answers and App Review.

Screenshots are rendered from the game itself by `tools\smoke-test.ps1 -Touch -Width 1434 -Height 660 -Super 2`
(iPhone) and `-Width 1376 -Height 1032 -Super 2` (iPad); the player cannot open a window bigger than the monitor,
so the shots are captured at double the window size. They live in `marketing\appstore`:
iPhone 6.5" 2688 x 1242 and iPad 13" 2752 x 2064, six each (title, game, turbo, bin, cat, powder).
Apple wants at least one iPhone 6.9" set, and an iPad 13" set because the build ships as iPhone + iPad.

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

## Promotional text (170 max, editable without a new version)

Nineteen vacuums, one filthy house, and a bag that fills up. Drive, suck, blow it all back out. New in 0.4.7: the
whole garage is finally the same scale.

## Description

You are the vacuum cleaner. The house is filthy. Eat crumbs, socks, chairs and eventually the toilet, then blow it
all back out.

Vacuum Cleaner Simulator 2026 is a physics sandbox in the spirit of Goat Simulator, except you are the machine and
the mess is not going to clean itself. Drive through a six-room house, suck up crumbs, dust bunnies, socks, toy
bricks and coins, then level up until chairs, the couch, the fridge and the toilet fit in the nozzle. When the bag
is full, empty it into the bin, or blow everything back out at high speed and start again.

NINETEEN MACHINES, ALL DIFFERENT
A robot disc, a bagless upright on a ball, a smiling red canister, a cordless stick, a 1978 upright with a
headlight, a French canister that whispers, a wet-and-dry drum that eats furniture early, a workshop trolley that
eats screws for breakfast, and the cardboard prototype that started it all. Each one drives, sucks and hops
differently.

A CORD THAT FIGHTS BACK
Corded models drag a real simulated cable. It straightens as you go, pulls tight around corners, and at the end it
holds you like a leash. Keep pulling and the plug pops out of the wall. Cordless models worry about their battery
instead.

A COCKPIT THAT TAKES ITSELF VERY SERIOUSLY
Every vacuum has its own suction gauge, a real dust bag or bin that fills up, motor readouts, warning lamps and a
mission log of twenty silly achievements. None of it will help you. All of it looks great.

BUILT FOR TOUCH
Big domed buttons like the ones on a real vacuum, a thumb stick that drives, drag anywhere to look around. Walls
turn to frosted glass when they stand between the camera and your machine, and a marker points at the bin when the
bag needs emptying.

- Family friendly: cartoon chaos, no violence, no language, no ads, no purchases.
- No account, no sign-in, nothing collected. Your score stays on your device.
- Also on Windows through the Microsoft Store.

## Keywords (100 characters max, comma separated, no spaces after commas)

`vacuum,cleaner,simulator,physics,sandbox,funny,silly,cleaning,house,goat,chaos,family,offline,no ads`

## What's New in this version

The whole garage is one size family: the realistic machines used to be measured on whatever stuck out of them, a
trailing hose or a push handle, so their bodies came out small. The boost now teaches itself until you have held it
once, and the garage no longer starts the run with the wrong vacuum.

## Review notes

No account and no sign-in. The game opens on the title screen: tap START, pick a vacuum with the arrows, then
drive with the left stick and use the round buttons on the right (turbo, hop, blow, empty the bag at the bin,
rewind the cord). Landscape only, iPhone and iPad. All vacuum names are parodies; no real product name, logo or
trademark appears in the game.

## The steps left, in order

1. Sign in to App Store Connect (the session expires quickly and the sign-in is the owner's).
2. App Information: subtitle, category, age rating questionnaire, privacy policy URL.
3. Pricing and Availability: Free, all territories.
4. App Privacy: "Data Not Collected", publish.
5. Version 1.0: description, keywords, promotional text, support URL, screenshots from `marketing\appstore`,
   the build (pick 407), review notes, export compliance.
6. "Add for Review", then Submit. Review is usually a day or two; there is no tester requirement and no waiting
   period. Rejections at this stage are usually metadata, not code.
