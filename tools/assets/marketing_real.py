#!/usr/bin/env python3
"""Swaps the cartoon vacuum of the marketing art for the realistic French canister (Monsieur Traineau).

2026-09-06, his direction: the game is about realistic vacuums now, no faces; the compositions of the existing
marketing pictures stay ("les photos actuelles sont tres bien"), only the vacuum changes. Each picture is an
image-to-image edit that receives TWO images: the current picture and a studio render of the canister as the
reference. The cartoon originals are kept in marketing/source/cartoon.

    python tools/assets/marketing_real.py --list
    python tools/assets/marketing_real.py [--only key_art,icon] [--model bytedance/seedream-v4-edit] [--force] [--dry-run]

Then `python tools/marketing.py` recuts the store sizes and the icons. Look at every picture before shipping it.
"""
import argparse
import json
import os
import shutil
import sys

SKILL = os.path.join(os.path.expanduser("~"), ".claude", "skills", "kie-ai", "scripts")
sys.path.insert(0, SKILL)
import kie as kiemod  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "tools", "assets", "raw")
SRC = os.path.join(ROOT, "marketing", "source")
CARTOON = os.path.join(SRC, "cartoon")
REFERENCE = os.path.join(ROOT, "docs", "research", "traineau-reference.png")

# the wording that worked on the key art (seedream-v4-edit, 2026-09-06): describe the reference body part by part,
# say "no eyes" several times and point at the second image "which has no eyes"; nano-banana-edit kept the eyes twice
VACUUM = ("a photorealistic copy of the vacuum cleaner in the SECOND image: a low, long canister vacuum with a red top "
          "shell and a dark-grey lower shell, two large spoked rear wheels, a small front caster, a ribbed black hose "
          "rising from its front to a metal wand ending in a flat floor head. The body is a plain appliance with NO EYES "
          "AT ALL, NO FACE, NO MOUTH, no decoration on the shell, exactly like the second image which has no eyes")
KEEP = ("Keep everything else of the first image exactly the same: the room, the furniture, the flying debris, the cat, "
        "the colours, the camera angle and the framing. ABSOLUTELY NO TEXT of any kind.")

ITEMS = {
    "key_art": f"Replace the cartoon vacuum cleaner that has googly eyes by {VACUUM}. Place it large on the floor in the middle of the room, tearing through the mess, crumbs and socks flying into its floor head. {KEEP}",
    "hero_wide": f"Replace the cartoon vacuum cleaner that has googly eyes by {VACUUM}. Place it large on the kitchen floor, charging across the room, cereal and crumbs swirling into its floor head. {KEEP}",
    "library_portrait": f"Replace the cartoon vacuum cleaner that has googly eyes by {VACUUM}. Place it standing proudly on top of the mountain of dust, socks and toys, seen from the same low angle. {KEEP}",
    "garage_lineup": ("Replace the eight cartoon vacuum cleaners that have googly eyes by realistic household vacuum cleaners of the "
                      "same colours and kinds (a robot disc, a purple bagless upright, a red canister, a cordless stick, a green "
                      "upright, a blue canister, a yellow drum, an orange box), plain appliances with NO EYES AT ALL and NO FACES, "
                      "and put " + VACUUM + " in the centre of the line-up, slightly in front of the others. Keep the studio "
                      "backdrop, the team-photo composition, the colours and the framing. ABSOLUTELY NO TEXT of any kind."),
    "icon": ("App icon: replace the cartoon vacuum face of the FIRST image by " + VACUUM + ", seen from a three-quarter front "
             "view with the hose and wand raised behind it, big, filling the frame, centred on the same bright safety-yellow "
             "rounded-square background as the first image. Bold, clean, glossy, square. ABSOLUTELY NO TEXT of any kind."),
}


# 2026-09-06 evening, his second direction: "plus style et impressionnant, artwork Need for Speed ou jeux de voiture,
# mais avec un aspi bien detaille et technique en mouvement". These start from the reference render alone.
RACE = ("this exact vacuum cleaner from the image, a red and dark-grey canister vacuum on wheels with a ribbed black hose "
        "and metal wand, rendered as a hyper-detailed technical hero machine: crisp mechanical parts, panel gaps, vents, "
        "spoked wheels with rubber tyres, glossy red paint with reflections, carbon-fibre and brushed-metal details, "
        "NO eyes, NO face, NO mouth")
RACE_STYLE = ("Key art in the style of a AAA racing video game: low dramatic camera angle close to the floor, the machine "
              "caught at full speed, motion blur streaks, dust clouds and debris trailing behind it, sparks from the wheels, "
              "strong rim lighting, wet-floor reflections, cinematic lens flare, moody dark colour grade with hot orange and "
              "electric blue accents, volumetric light, photoreal 3D render, family friendly, no violence. ABSOLUTELY NO TEXT "
              "of any kind: no title, no letters, no logos, no watermark.")
RACE_ITEMS = {
    "key_art": (f"{RACE}, drifting sideways through a dark messy living room at night, its hose and wand whipping behind it, "
                f"crumbs, socks and toy bricks being sucked into its floor head, a sofa and a lamp blurred in the background. "
                f"Square composition with room at the top for a title. {RACE_STYLE}"),
    "hero_wide": (f"{RACE}, charging straight at the camera down a long dark kitchen at night, chairs toppling in its wake, "
                  f"cereal and crumbs swirling into its floor head. Very wide landscape composition, machine centred. {RACE_STYLE}"),
    "library_portrait": (f"{RACE}, launching off a mountain of dust, socks and toys like a rally car over a crest, seen from a "
                         f"low angle against a dramatic dark sky of dust, headlight-like glow from its front. Tall portrait "
                         f"composition with empty space at the top for a title. {RACE_STYLE}"),
    "icon": (f"App icon on a FLAT bright safety-yellow background that fills the entire square edge to edge (no floor, no "
             f"room, no scene, no panel, no vignette): {RACE}, three-quarter front view, slightly tilted as if cornering at "
             f"speed, big and centred, filling most of the frame, with a small dust puff and a few orange sparks behind it "
             f"and a soft drop shadow under it. Bold, punchy, readable when small, square. Photoreal 3D render, family "
             f"friendly. ABSOLUTELY NO TEXT of any kind: no title, no letters, no logos, no watermark."),
}


# 2026-09-22, his icon pick ("go pour sled navy clean, ensure other marketplace graphic assets follow this"): every
# marketing picture is now the same machine in the same studio as the icon, edited from the icon itself (the committed
# copy in marketing/icon-candidates) so the body, the navy and the blue glow match. No debris, no sparks, no room.
NAVY_REFERENCE = os.path.join(ROOT, "marketing", "icon-candidates", "sled_navy_clean.png")
NAVY = ("this exact vacuum cleaner from the image: a glossy candy-red canister vacuum with a dark-grey lower shell, "
        "two silver caps on top, a round blue-lit intake ring on its front, a ribbed black hose, a chrome telescopic "
        "wand and a flat black floor head, photoreal, NO eyes, NO face, NO mouth")
NAVY_STYLE = ("Exactly the same look as the image: a deep navy blue studio background with an electric-blue radial light "
              "burst behind the machine, a glossy wet dark-blue floor with a mirror reflection of the machine, one soft "
              "thin wisp of grey dust drifting into the floor head and a light dusting of grey dust on the floor. "
              "Nothing else: no debris, no socks, no toys, no sparks, no tornado, no furniture, no room, no people. "
              "Punchy, cinematic, family friendly. ABSOLUTELY NO TEXT of any kind: no title, no letters, no logos, "
              "no watermark.")
NAVY_ITEMS = {
    "key_art": (f"Square key art: {NAVY}, large, three-quarter front view from a low camera, the body in the lower right "
                f"and the wand and floor head reaching to the lower left, leaving the upper third of the frame as empty "
                f"navy background for a title. {NAVY_STYLE}"),
    "hero_wide": (f"Very wide landscape banner (16:9): {NAVY}, centred, seen from a low front three-quarter camera as if "
                  f"charging towards the viewer, the wand and floor head sweeping to the left, a long wisp of dust "
                  f"trailing behind it to the right, wide empty navy background on both sides. {NAVY_STYLE}"),
    "library_portrait": (f"Tall portrait poster (2:3): {NAVY}, in the lower half of the frame, seen from a low camera, the "
                         f"chrome wand rising diagonally through the middle of the picture, the upper third empty navy "
                         f"background with the blue light burst, for a title. {NAVY_STYLE}"),
}
NAVY_SIZE = {"key_art": "square_hd", "hero_wide": "landscape_16_9", "library_portrait": "portrait_3_2"}

# 2026-09-22 night, his "you need AI gen images to show better images of the actions no? like a hoover swallowing a
# toilet": one generated action picture per store subject, the icon's machine dropped into the house, edited from
# the icon (seedream-v4-edit, 5 credits each, 16:9). They land in marketing/source/action/<name>.png; store_shots.py
# captions them like the captures. Apple only accepts pictures of the app in use (guideline 2.3.3), so these are for
# the Microsoft Store gallery, the Play listing and the site, never the App Store slots.
ACTION_SCENE = ("The scene is the inside of a bright, colourful family house rendered like a 3D animated film: clean "
                "geometry, warm daylight, saturated pastel walls and floors, simple cartoon furniture, family friendly, "
                "no people. Dynamic action, motion blur on the flying things, a punchy cinematic camera. The machine "
                "stays exactly as in the image, photoreal and glossy, with NO eyes, NO face, NO mouth. ABSOLUTELY NO "
                "TEXT of any kind: no title, no letters, no logos, no watermark, no signs.")
ACTION_ITEMS = {
    "toilet": (f"Wide 16:9 game key art: {NAVY}, in a bathroom, its ribbed black hose stretched impossibly wide around a "
               f"white ceramic toilet that is halfway swallowed into the hose, the toilet bent and squeezed like rubber, "
               f"water and a toilet roll flying, the machine braced on its wheels. Absurd and funny. {ACTION_SCENE}"),
    "couch": (f"Wide 16:9 game key art: {NAVY}, in a living room, its floor head lifted, a whole sofa with its cushions "
              f"being sucked into the hose end, the sofa stretching and folding into the nozzle, cushions and a lamp "
              f"flying towards it, a bookshelf leaning. Absurd and funny. {ACTION_SCENE}"),
    "cat": (f"Wide 16:9 game key art: {NAVY}, charging across a living-room floor at full speed after a fluffy ginger "
            f"cat that leaps away in panic with its fur puffed up, a rug lifting, crumbs and toy bricks scattering. "
            f"Funny, nobody gets hurt. {ACTION_SCENE}"),
    "turbo": (f"Wide 16:9 game key art: {NAVY}, seen from a low rear three-quarter camera, racing down a long hallway "
              f"with orange sparks flying from its wheels, a thick plume of grey dust behind it and radial speed lines, "
              f"socks and cereal sucked into its floor head. {ACTION_SCENE}"),
    "cord": (f"Wide 16:9 game key art: {NAVY}, straining at the end of a black power cord stretched taut across a "
             f"kitchen, the plug tearing out of a wall socket with a little puff of smoke and a spark, the cord whipping. "
             f"Funny. {ACTION_SCENE}"),
    "blowout": (f"Wide 16:9 game key art: {NAVY}, its bag bulging to bursting, blowing a huge fountain of socks, toy "
                f"bricks, cereal, coins, dust bunnies and a rubber duck out of its front intake across a bedroom, the "
                f"machine rearing up on its rear wheels. Absurd and funny. {ACTION_SCENE}"),
    "powder": (f"Wide 16:9 game key art from a high three-quarter camera: {NAVY}, in a dining room whose floor is "
               f"covered with brown cocoa powder, leaving a clean winding trail of shiny floor behind its floor head, "
               f"powder swirling into the nozzle. {ACTION_SCENE}"),
    "garage": (f"Wide 16:9 game key art: {NAVY}, in the spotlight in the middle of a big garage showroom, surrounded by "
               f"a dozen other household vacuum cleaners of every kind parked in a semicircle behind it (a robot disc, "
               f"uprights, canisters, a yellow workshop drum, a cordless stick), all plain appliances with no faces, "
               f"a concrete floor, tool walls, neon tubes. {ACTION_SCENE}"),
}


def log(*a):
    print(*a, flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--model", default="bytedance/seedream-v4-edit")
    ap.add_argument("--style", default="swap", choices=["swap", "race", "navy", "action"], help="swap: keep the cartoon composition; race: racing-game key art from the reference alone; navy: the icon's studio (2026-09-22) edited from the icon; action: one action picture per store subject, edited from the icon (2026-09-22 night)")
    a = ap.parse_args()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    items = {"race": RACE_ITEMS, "navy": NAVY_ITEMS, "action": ACTION_ITEMS}.get(a.style, ITEMS)
    reference = NAVY_REFERENCE if a.style in ("navy", "action") else REFERENCE
    names = [n for n in items if not only or n in only]
    tag = {"race": "race_", "navy": "navy_", "action": "action_"}.get(a.style, "real_")
    out_dir = os.path.join(SRC, "action") if a.style == "action" else SRC
    os.makedirs(out_dir, exist_ok=True)

    os.makedirs(CARTOON, exist_ok=True)
    if a.list:
        for n in names:
            raw = os.path.join(RAW, f"{tag}{n}.png")
            log(f"{'done' if os.path.exists(raw) else 'todo':5} {n:18} -> {os.path.relpath(os.path.join(out_dir, n + '.png'), ROOT)}")
        return 0
    if not os.path.exists(reference):
        log("reference render missing: " + reference)
        return 1

    k = kiemod.Kie()
    before = None if a.dry_run else k.credits()
    log(f"balance before: {before}")
    ref_url = None
    failures = []
    for n in names:
        # the cartoon original is the base: moved aside once, edited from there every time
        base = os.path.join(CARTOON, n + ".png")
        if a.style == "swap" and not os.path.exists(base):
            shutil.copy2(os.path.join(SRC, n + ".png"), base)
        if a.style == "navy":   # the racing pictures step aside the same way the cartoons did
            keep = os.path.join(SRC, "race", n + ".png")
            if not os.path.exists(keep) and os.path.exists(os.path.join(RAW, f"race_{n}.png")):
                os.makedirs(os.path.dirname(keep), exist_ok=True)
                shutil.copy2(os.path.join(RAW, f"race_{n}.png"), keep)
        raw = os.path.join(RAW, f"{tag}{n}.png")
        if os.path.exists(raw) and os.path.getsize(raw) > 10000 and not a.force:
            log(f"[{n}] already there, skipped")
        elif a.dry_run:
            log(f"[{n}] would edit with {a.model}")
            continue
        else:
            try:
                if ref_url is None:
                    ref_url = k.upload(reference, "images/vcs")
                    log("   reference uploaded")
                urls = [ref_url] if a.style in ("race", "navy", "action") else [k.upload(base, "images/vcs"), ref_url]
                inp = {"prompt": items[n], "image_urls": urls, "output_format": "png"}
                if a.style == "navy" and n in NAVY_SIZE:
                    inp["image_size"] = NAVY_SIZE[n]   # measured afterwards: the models do not always honour it
                elif a.style == "action":
                    inp["image_size"] = "landscape_16_9"
                r = k._request(k.base + "/api/v1/jobs/createTask", {"model": a.model, "input": inp})
                tid = (r.get("data") or {}).get("taskId")
                if not tid:
                    raise kiemod.KieError("no taskId: " + json.dumps(r)[:200])
                log(f"[{n}] task {tid} ({a.model})")
                url = k.poll(tid)
                size = k.download(url, raw)
                log(f"   OK -> {os.path.relpath(raw, ROOT)} ({size // 1024} KB)")
            except Exception as e:  # noqa: BLE001
                failures.append((n, str(e)))
                log(f"[{n}] FAILED: {e}")
                continue
        if a.style == "navy" and n == "key_art":
            # a square asked of seedream comes back as an icon: rounded corners on a white margin (2026-09-22)
            sys.path.insert(0, os.path.join(ROOT, "tools"))
            import wordmark  # noqa: E402
            from PIL import Image  # noqa: E402
            wordmark.edge_to_edge(Image.open(raw)).convert("RGB").save(os.path.join(SRC, n + ".png"))
        else:
            shutil.copy2(raw, os.path.join(out_dir, n + ".png"))
        log(f"   -> {os.path.relpath(os.path.join(out_dir, n + '.png'), ROOT)}")
    after = None if a.dry_run else k.credits()
    log(f"balance after: {after}  spent: {None if before is None or after is None else round(before - after, 2)}")
    if failures:
        log("FAILURES: " + ", ".join(f"{n}: {e}" for n, e in failures))
        return 1
    log("CAMPAIGN OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
