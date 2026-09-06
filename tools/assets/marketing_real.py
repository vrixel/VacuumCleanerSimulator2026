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


def log(*a):
    print(*a, flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--model", default="bytedance/seedream-v4-edit")
    ap.add_argument("--style", default="swap", choices=["swap", "race"], help="swap: keep the cartoon composition; race: racing-game key art from the reference alone")
    a = ap.parse_args()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    items = RACE_ITEMS if a.style == "race" else ITEMS
    names = [n for n in items if not only or n in only]
    tag = "race_" if a.style == "race" else "real_"

    os.makedirs(CARTOON, exist_ok=True)
    if a.list:
        for n in names:
            raw = os.path.join(RAW, f"{tag}{n}.png")
            log(f"{'done' if os.path.exists(raw) else 'todo':5} {n:18} <- {os.path.relpath(os.path.join(CARTOON, n + '.png'), ROOT)}")
        return 0
    if not os.path.exists(REFERENCE):
        log("reference render missing: " + REFERENCE)
        return 1

    k = kiemod.Kie()
    before = None if a.dry_run else k.credits()
    log(f"balance before: {before}")
    ref_url = None
    failures = []
    for n in names:
        # the cartoon original is the base: moved aside once, edited from there every time
        base = os.path.join(CARTOON, n + ".png")
        if not os.path.exists(base):
            shutil.copy2(os.path.join(SRC, n + ".png"), base)
        raw = os.path.join(RAW, f"{tag}{n}.png")
        if os.path.exists(raw) and os.path.getsize(raw) > 10000 and not a.force:
            log(f"[{n}] already there, skipped")
        elif a.dry_run:
            log(f"[{n}] would edit with {a.model}")
            continue
        else:
            try:
                if ref_url is None:
                    ref_url = k.upload(REFERENCE, "images/vcs")
                    log("   reference uploaded")
                urls = [ref_url] if a.style == "race" else [k.upload(base, "images/vcs"), ref_url]
                inp = {"prompt": items[n], "image_urls": urls, "output_format": "png"}
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
        shutil.copy2(raw, os.path.join(SRC, n + ".png"))
        log(f"   -> {os.path.relpath(os.path.join(SRC, n + '.png'), ROOT)}")
    after = None if a.dry_run else k.credits()
    log(f"balance after: {after}  spent: {None if before is None or after is None else round(before - after, 2)}")
    if failures:
        log("FAILURES: " + ", ".join(f"{n}: {e}" for n, e in failures))
        return 1
    log("CAMPAIGN OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
