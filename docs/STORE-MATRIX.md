# The three stores, side by side

One source for the words and the pictures of the Microsoft Store, Google Play and App Store listings (2026-09-22,
his "a matrix of the 3 stores to make sure titles, marketing copy and graphics are consistent and excellent").
The console mechanics, the statuses and the submission steps stay in `STORE.md`, `PLAY.md` and `APPSTORE.md`;
those three now point here for the copy. When a word changes, change it here, then paste into the three consoles.

The rule behind every field: one title, one voice (the game's own arcade voice, short sentences, the joke played
straight), the same facts everywhere (nineteen machines, 22 achievements, one cat, a cord that fights back, no
ads, no account, nothing collected), and no other game, brand or store named in any metadata (App Review rejects
a competitor's trademark, and the other two consoles gain nothing from it).

## The matrix

| Field | Microsoft Store (PC) | Google Play | App Store (iPhone, iPad) |
|---|---|---|---|
| Title | Vacuum Cleaner Simulator 2026 | same (30 max, 29 used) | same (30 max, 29 used) |
| Tagline | Short description, 200 max: **S1** below (188) | Short description, 80 max: **S2** (76) | Subtitle, 30 max: **S3** (22) |
| Description | **D** with the PC paragraph **P-PC** | **D** with the touch paragraph **P-TOUCH** and the credits line | **D** with **P-TOUCH**, no URL in the text (the credits live on the support page) |
| Promotional text | none | none | 170 max: **PROMO** (162), editable without a new build |
| Keywords | Store tags: Simulation, Physics, Casual, Comedy, Sandbox, Family Friendly, Singleplayer, Controller Support | none (Play indexes the description: the words of **D** are the keywords) | 100 max: **KW** (97), no word already in the title |
| What's new | **WN** as release notes | **WN** as release notes | **WN** as "What's New" |
| Category | Games > Simulation, plus Family and kids | Game > Simulation | Games, primary Simulation, secondary Family |
| Age rating | IARC: ESRB E, PEGI 3 | IARC: Everyone, PEGI 3, target audience 13+ | questionnaire, 4+ |
| Price and reach | free, all 240 markets | free, 177 countries (the maximum for this account) | free, 175 territories |
| Icon | `marketing/store/ms_icon_300.png` (300) | `marketing/icon/icon_512.png` (512) | `Assets/Icon/icon.png` through the build (1024 in Xcode) |
| Screenshots | 10 x 1920 x 1080, captioned: `marketing/store/screens/NN-slug.png` | 8 x 1920 x 1080 (phone, 16:9, touch HUD, captioned): `marketing/play/phone-NN-slug.png` | iPhone 6.5" 2688 x 1242 (the version page refuses 6.9"): `marketing/appstore/iphone65-NN-slug.png`; iPad 13" 2752 x 2064: `ipad13-NN-slug.png`; 8 each |
| Gallery of the machines | `marketing/store/gallery_1920x1080.png` as screenshot 02 | `marketing/play/phone-02-gallery.png` | `iphone65-02-gallery.png`, `ipad13-02-gallery.png` |
| Big picture | super hero 1920 x 1080 `ms_superhero_1920x1080.png`, poster 720 x 1080, box art 1080 | feature graphic 1024 x 500 `marketing/play/feature_1024x500.png` | none (the first screenshots are the big picture) |
| Video | trailer MP4 1920 x 1080, under 60 s: `marketing/video/trailer_1920x1080.mp4`, poster `trailer_poster.png` | the same trailer on YouTube (unlisted is enough), link in the listing | App Preview: device-size footage only, no other platform named; NOT this trailer, a later job |
| Privacy | policy URL, mandatory for a runFullTrust package | policy URL, data safety "nothing collected" | policy URL, App Privacy "Data Not Collected" |
| Live version | 0.4.7.0 (submission 5) | 407 internal + closed "Alpha" | TestFlight 407 internal; version 1.0 waits for "Add for Review" |
| Next click (his) | new submission with 0.4.9 (`tools\msix.py`) | "Apply for production access" after 2026-09-25, then a 0.4.9 release | "Add for Review" |

Where a store has no field (Play has no keywords, the Microsoft Store has no promo text), nothing is lost: the
facts of that field are already inside **D**.

## The copy

### Title (all three)

    Vacuum Cleaner Simulator 2026

### S1, Microsoft Store short description (200 max, 188)

    You are the vacuum cleaner. The house is filthy. Eat crumbs, socks, chairs and eventually the toilet, then blow it all back out. Nineteen machines, one cat, a suspiciously serious cockpit.

### S2, Google Play short description (80 max, 76)

    You are the vacuum. The house is filthy. Eat socks, chairs, even the toilet.

### S3, App Store subtitle (30 max, 22)

    Suck it up. All of it.

### D, the description (all three; swap the platform paragraph)

    You are the vacuum cleaner. The house is filthy. Eat crumbs, socks, chairs and eventually the toilet, then blow it all back out.

    Vacuum Cleaner Simulator 2026 is a comedy physics sandbox where you are the machine and the mess is not going to clean itself. Drive through a six-room house, suck up crumbs, dust bunnies, socks, toy bricks and coins, then level up until chairs, the couch, the fridge and the toilet fit in the nozzle. When the bag is full, empty it into the bin, or blow everything back out at high speed and start again.

    NINETEEN MACHINES, ALL DIFFERENT
    Eight built from scratch and eleven realistic machines on loan: a robot disc, a bagless upright on a ball, a smiling red canister, a cordless stick, a 1978 upright with a headlight, a French sled that glides, a wet-and-dry drum that eats furniture early, a workshop trolley, and the cardboard prototype that started it all. Each one drives, sucks and hops differently.

    A CORD THAT FIGHTS BACK
    Corded models drag a real simulated cable. It straightens as you go, pulls tight around corners, and at the end it holds you like a leash. Keep pulling and the plug pops out of the wall. Rewind it from across the house and watch the plug whip through the furniture. Cordless models worry about their battery instead.

    A CAT WITH OPINIONS
    Chase it through the living room. It steers around the furniture, panics, hops sideways and yowls. It can never be vacuumed, but it does not know that.

    A COCKPIT THAT TAKES ITSELF VERY SERIOUSLY
    Every vacuum has its own suction gauge, a real dust bag or bin that fills up, motor readouts, warning lamps, a dirt radar and a mission log of 22 silly achievements. None of it will help you. All of it looks great.

    <platform paragraph, P-PC or P-TOUCH>

    - Family friendly: cartoon chaos, no violence, no language, no ads, no purchases.
    - No account, no sign-in, nothing collected. Your best score, achievements and garage choice stay on your device.
    - A one-minute tutorial on the first run, replayable from the pause menu.

**P-PC** (Microsoft Store):

    BUILT FOR THE COUCH
    Full controller support (Xbox layout) and keyboard and mouse. Boost with the trigger, hop over the mess, rewind the cord from across the house.

**P-TOUCH** (Google Play, App Store):

    BUILT FOR TOUCH
    Big domed buttons like the ones on a real vacuum, a thumb stick that drives, drag anywhere to look around. Walls turn to frosted glass when they hide your machine, and a marker points at the bin when the bag needs emptying.

**Credits line** (Google Play and Microsoft Store only; the App Store text carries no URL):

    All vacuum names are parodies; no real product names or logos are used. The eleven realistic meshes are Creative Commons models, credited at github.com/vrixel/VacuumCleanerSimulator2026/blob/main/docs/CREDITS.md.

### PROMO, App Store promotional text (170 max, 162)

    Nineteen vacuums, one filthy house, one cat with opinions. Drive, boost, suck it all up, then blow it back out. New: a one-minute tutorial, and the cat now yowls.

### KW, App Store keywords (100 max, 97; never a word of the title, Apple indexes the title already)

    physics,sandbox,funny,silly,cleaning,house,chaos,family,offline,cat,hoover,robot,arcade,kids,mess

### WN, What's new (0.4.9, the same three stores; the stores are on 0.4.7, so it covers 0.4.8 too)

    0.4.9: a one-minute tutorial on the first run (driving, boost, the bag, the bin; skip any time, replay from the pause menu), Rate This Game and Send Feedback on the title screen, and a second generation of sound: the cat cries when you chase it, things clatter into the hose, the motor labours and the intake wheezes as the bag fills, the boost has its own whine. The garage is one size family at last.

### Review notes (App Store, and the Microsoft Store certification notes)

    No account and no sign-in. The game opens on the title screen: START, pick a vacuum with the arrows, then drive with the left stick (or WASD) and use the round buttons (turbo, hop, blow, empty the bag at the bin, rewind the cord). A one-minute tutorial runs on the first launch and can be skipped. All vacuum names are parodies; no real product name, logo or trademark appears in the game.

## The pictures

All rendered by the game itself (`tools/store_shots.py` over the smoke captures, one script for the four sizes)
in the HUD's own arcade typography (`tools/brand.py`: Russo One italic headlines with a hard edge and a block
shadow, safety yellow, electric blue, no glow), so the store pages speak like the game. Eight subjects, the same
order on every store, so someone who sees the PC page and then the phone page sees one game:

| # | Subject | Headline | What it shows |
|---|---|---|---|
| 01 | Gameplay | YOU ARE THE VACUUM | the run, the cockpit (PC) or the domed buttons (touch) |
| 02 | Gallery | NINETEEN MACHINES | the nineteen models side by side, the wordmark |
| 03 | The cat | CHASE THE CAT | the cat fleeing across the living room |
| 04 | The boost | HOLD THE BOOST | dust plume, sparks, speed lines |
| 05 | The cord | A CORD THAT FIGHTS BACK | the cable taut to the socket, the plug about to pop |
| 06 | The bag | BAG FULL? FIND THE BIN | the bag at 86 %, the BIN marker, the bin |
| 07 | The powder | LEAVE YOUR MARK | the cocoa trail from above |
| 08 | The garage / tutorial | PICK YOUR MACHINE / LEARN IT IN SIX STEPS | the garage (PC), the first-run tutorial (touch) |

PC adds 09 the plug yanked and 10 the rewind. The captions sit above the cockpit on PC, and on touch they sit low
in a zone that never covers the REWIND button (`CAPTION_ZONE` in `store_shots.py`).

The wordmark (`marketing/logo/wordmark.png`, transparent, and `wordmark-line.png`) is the same on the gallery,
the feature graphic, the trailer cards and the site. The icon is his open pick: four kie candidates next to the
shipped icon at 512 / 128 / 64 with the store corner mask in `marketing/icon-candidates/sheet.png` (second round after his first look: three red-sled
vortex candidates on blue, cyan and navy backgrounds)
(`tools/assets/icon_candidates.py` generates, `tools/wordmark.py --sheet` composes); nothing replaces
`marketing/source/icon.png` until he chooses, then `python tools\marketing.py` cuts every size.

The trailer: `tools\record.ps1` makes the game record itself (30 steps per second, every frame a JPEG, chapter
marks), `python tools\store_video.py` assembles it (title card, chapter captions, end card, the game's music over
a low motor hum) into `marketing/video/trailer_1920x1080.mp4` with a poster frame and a contact sheet to check.

## Gaps and next clicks

- Microsoft Store: the live listing has four uncaptioned 0.4.0-era screenshots and the racing-style key art. Next
  submission (0.4.9): the ten captioned screens, the trailer, **S1** and **D** + **P-PC**, **WN**.
- Google Play: the live listing has the eight 0.4.2 phone shots and the old feature graphic. On the production
  release: the eight captioned phone shots, the new feature graphic, the YouTube link to the trailer, **S2** and
  **D** + **P-TOUCH**, **WN**. The Play name and the App Store name are the same 29 characters.
- App Store: the version 1.0 page carries the 0.4.7 shots, the 0.4.7 promo text and "twenty achievements". Before
  "Add for Review": the eight iPhone 6.5" and eight iPad 13" captioned shots, **PROMO**, **D** + **P-TOUCH**, **KW**,
  **WN**, and build 409 once it is on TestFlight.
- The icon: his pick from the sheet, then `tools\marketing.py`, a rebuild (exe icon), a new MSIX and AAB and an
  iOS export (the icon travels inside the builds).
- App Preview video for the App Store: needs device-size footage (the record mode can run the touch layer at the
  iPhone window size); not started.
