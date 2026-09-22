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
MODEL = "bytedance/seedream-v4-edit"

MACHINE = ("this exact vacuum cleaner from the image, a red and dark-grey canister vacuum on two large spoked rear wheels "
           "with a ribbed black hose and a metal wand ending in a flat floor head, NO eyes, NO face, NO mouth")
NO_TEXT = " ABSOLUTELY NO TEXT of any kind: no title, no letters, no numbers, no logos, no watermark."
ICON = ("App icon, square, edge to edge, bold and readable when tiny (60 pixels), one strong silhouette, high contrast, "
        "clean 3D render, family friendly.")

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
    if not os.path.exists(REFERENCE):
        log("reference render missing: " + REFERENCE)
        return 1
    k = kiemod.Kie()
    before = None if a.dry_run else k.credits()
    log(f"balance before: {before}")
    ref_url = None
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
                if ref_url is None:
                    ref_url = k.upload(REFERENCE, "images/vcs")
                    log("   reference uploaded")
                inp = {"prompt": ITEMS[n], "image_urls": [ref_url], "output_format": "png"}
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
