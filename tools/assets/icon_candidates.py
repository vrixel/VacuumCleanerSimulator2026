#!/usr/bin/env python3
"""Icon candidates for the stores (2026-09-22, his "the logo I dont like too much").

The shipped icon is a product shot on flat yellow: no motion, no chaos, no idea. Each candidate below is one idea,
generated bare (no text: the models cannot write, the wordmark is composed afterwards by tools/wordmark.py) with
seedream-v4-edit from the reference render of Monsieur Traineau, so the machine stays the game's hero.

    python tools/assets/icon_candidates.py --list
    python tools/assets/icon_candidates.py [--only chaos,vortex] [--force] [--dry-run]

Raws land in tools/assets/raw/icon_<name>.png (kept, never overwritten without --force), copies in
marketing/icon-candidates/<name>.png. Look at every one on the contact sheet (tools/wordmark.py --sheet) before
choosing; a `success` proves an image came back, not that the prompt was honoured.

Second round (his 2026-09-22 verdict on the first sheet: "I like the vortex style but I want a traineaux vacuum not
the dyson type ... make sure the background color and vacuum color are contrasting and use same as top selling
games"): the sled_* items keep the vortex composition but the machine is a glossy red canister on a black base, from
his own reference photo (tools/assets/raw/icon_sled_reference.png, local only: it is a product photo, shape and
colour inspiration, never shipped), and the background is one flat saturated colour that fights the red machine,
the way the top-grossing game icons do (one big centred subject, an edge-to-edge burst, nothing small).
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
OUT = os.path.join(ROOT, "marketing", "icon-candidates")
REFERENCE = os.path.join(ROOT, "docs", "research", "traineau-reference.png")
SLED_REFERENCE = os.path.join(RAW, "icon_sled_reference.png")   # his photo, gitignored with the raws
MODEL = "bytedance/seedream-v4-edit"

MACHINE = ("this exact vacuum cleaner from the image, a red and dark-grey canister vacuum on two large spoked rear wheels "
           "with a ribbed black hose and a metal wand ending in a flat floor head, NO eyes, NO face, NO mouth")
NO_TEXT = " ABSOLUTELY NO TEXT of any kind: no title, no letters, no numbers, no logos, no watermark."
ICON = ("App icon, square, edge to edge, bold and readable when tiny (60 pixels), one strong silhouette, high contrast, "
        "clean 3D render, family friendly.")
SLED = ("this exact vacuum cleaner from the image: a glossy candy-red canister vacuum with a domed body on a black base "
        "and black bumper, small chrome buttons on top, a black corrugated hose arching up out of the front of the body, "
        "a chrome telescopic wand and a flat black floor head, NO eyes, NO face, NO mouth")
SLED_SCENE = (f"{ICON} {SLED}, the whole machine visible and big, centred: the flat black floor head in the foreground "
              f"at floor level bottom-left with a dramatic spiralling tornado of dust, crumbs, a striped sock, toy bricks "
              f"and a coin twisting down into its intake, the chrome wand and the black hose curving up and back to the "
              f"big glossy red body on the right, tilted as if lunging forward, an electric-blue glow inside the intake, "
              f"a few orange sparks, a wet-floor reflection under the machine. Punchy, cinematic, the red body must pop "
              f"against the background. Background: ")
SLED_BG = {
    "blue": "a flat vivid electric-blue (royal blue) filling the whole frame with a subtle lighter-blue radial "
            "sunburst of straight rays from behind the machine, no room, no horizon.",
    "cyan": "a flat bright cyan-turquoise filling the whole frame, a little lighter at the top, with a subtle "
            "radial sunburst of straight paler rays from behind the machine, no room, no horizon.",
    "navy": "a deep navy-blue filling the whole frame with a strong electric-blue radial glow right behind the "
            "machine so its red body is rimmed with light, no room, no horizon.",
}

ITEMS = {
    # the machine eating the mess: the game in one picture
    "chaos": (f"{ICON} {MACHINE}, seen from a low three-quarter front angle, big and centred, tilted as if cornering, "
              f"a spiralling vortex of crumbs, a striped sock, toy bricks and a coin being sucked into its floor head, "
              f"a small dust cloud behind it. Background: a flat bright safety-yellow with a subtle darker-yellow radial "
              f"sunburst of straight rays from the centre, no floor, no room.{NO_TEXT}"),
    # the nozzle as a monster mouth of air: iconic, no face needed
    "vortex": (f"{ICON} Extreme close-up of the wide floor head of {MACHINE}, seen from the front at floor level, filling "
               f"the bottom half of the frame, with a dramatic spiralling tornado of dust, crumbs, a red sock, toy bricks "
               f"and a coin twisting down into its intake. Dark charcoal background with an electric-blue glow inside the "
               f"intake and orange sparks, wet-floor reflection. Punchy, cinematic.{NO_TEXT}"),
    # the cat: the joke everybody gets
    "cat": (f"{ICON} {MACHINE} charging straight at the camera at full speed, dust plume behind it, and a fluffy grey "
            f"cartoon-realistic cat leaping sideways out of its way in the foreground with wide eyes and puffed tail, "
            f"comic panic, harmless and funny. Background: flat bright safety-yellow with a diagonal electric-blue "
            f"stripe, no room, no floor detail.{NO_TEXT}"),
    # the sticker: a graphic emblem that reads at any size
    "sticker": (f"Flat vector sticker-style app icon, square, edge to edge: {MACHINE} drawn as a bold simplified emblem "
                f"in three-quarter side view with a thick black outline, hard cel shading, glossy red and dark-grey panels, "
                f"a stylised whoosh of air and three flying crumbs into its floor head, centred on a flat electric-blue "
                f"background with a subtle radial burst of lighter-blue rays. Clean, graphic, readable when tiny, family "
                f"friendly, NO eyes, NO face.{NO_TEXT}"),
}
# second round: the vortex idea on the red sled, three backgrounds that contrast with the machine
for _bg, _desc in SLED_BG.items():
    ITEMS[f"sled_{_bg}"] = SLED_SCENE + _desc + NO_TEXT
SLED_ITEMS = {n for n in ITEMS if n.startswith("sled_")}


def log(*a):
    print(*a, flush=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--model", default=MODEL)
    a = ap.parse_args()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    names = [n for n in ITEMS if not only or n in only]
    os.makedirs(OUT, exist_ok=True)
    if a.list:
        for n in names:
            log(f"{'done' if os.path.exists(os.path.join(RAW, 'icon_' + n + '.png')) else 'todo':5} {n}")
        return 0
    for n in names:
        need = SLED_REFERENCE if n in SLED_ITEMS else REFERENCE
        if not os.path.exists(need):
            log(f"reference missing for {n}: {need}")
            return 1
    k = kiemod.Kie()
    before = None if a.dry_run else k.credits()
    log(f"balance before: {before}")
    ref_urls = {}
    failures = []
    for n in names:
        raw = os.path.join(RAW, f"icon_{n}.png")
        if os.path.exists(raw) and os.path.getsize(raw) > 10000 and not a.force:
            log(f"[{n}] already there, skipped")
        elif a.dry_run:
            log(f"[{n}] would generate with {a.model}")
            continue
        else:
            try:
                ref = SLED_REFERENCE if n in SLED_ITEMS else REFERENCE
                if ref not in ref_urls:
                    ref_urls[ref] = k.upload(ref, "images/vcs")
                    log(f"   reference uploaded: {os.path.basename(ref)}")
                inp = {"prompt": ITEMS[n], "image_urls": [ref_urls[ref]], "output_format": "png"}
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
        shutil.copy2(raw, os.path.join(OUT, n + ".png"))
    after = None if a.dry_run else k.credits()
    log(f"balance after: {after}  spent: {None if before is None or after is None else round(before - after, 2)}")
    if failures:
        log("FAILURES: " + ", ".join(f"{n}: {e}" for n, e in failures))
        return 1
    log("CAMPAIGN OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
