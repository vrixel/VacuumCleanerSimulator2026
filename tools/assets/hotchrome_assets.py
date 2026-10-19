#!/usr/bin/env python3
"""Hot Chrome HUD sprites (exploration, branch explore/hot-chrome only).

The same parts as the shipped HUD (kie_assets.HUD_PLATES / HUD_ELEMENTS) redrawn in the pink car-ad look: mirror
chrome edges catching hot-pink reflections, black-cherry lacquer, hot-pink accent lines. Same names, written to
Assets/Resources/UI/HudPink, which UIStyle reads first when the game runs with -pink (missing ones fall back to
UI/Hud, so bar_fill and speed_lines are reused as they are). Raws land in tools/assets/raw as pk_<name>.png.

    python tools/assets/hotchrome_assets.py --dry-run
    python tools/assets/hotchrome_assets.py [--only frame_wide,tile_on] [--force] [--reprocess]
    python tools/assets/hotchrome_assets.py --sheet tools/assets/raw/pink_sheet.png
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import kie_assets as K  # noqa: E402

DST = os.path.join(K.RES, "UI", "HudPink")
NO_TEXT = K.NO_TEXT
NO_TEXT_GREEN = K.NO_TEXT_GREEN

HC = ("Hot Chrome luxury car-advert style: mirror-polished chrome bevelled edges reflecting hot pink and magenta "
      "light, deep glossy black-cherry lacquer surfaces, one thin crisp hot-pink (#FF2E8A) accent line, sharp "
      "diagonal chamfers, tiny chrome hex screws, high contrast, clean and crisp, flat front view, no perspective, "
      "evenly lit, no glow, no bloom. ")
FRAME = HC + ("The inside of the frame is a perfectly flat matte black screen, completely empty, nothing lit, no "
              "reflections on the screen. ")
NO_FLOOR = "Floating in empty space: no floor, no reflection, no shadow, nothing under or around it. "
PART = ("A single isolated control-panel part, nothing else in the picture. " + HC)

PLATES = {
    "frame_square": "A square instrument screen frame for a game HUD. " + FRAME + NO_FLOOR + NO_TEXT_GREEN,
    "frame_wide": "A wide horizontal instrument screen frame for a game HUD, twice as wide as it is tall. " + FRAME + NO_FLOOR + NO_TEXT_GREEN,
    "frame_tall": "A tall vertical instrument screen frame for a game HUD, twice as tall as it is wide. " + FRAME + NO_TEXT,
    "plate_score": ("A wide scoreboard plate for a game HUD, three times as wide as it is tall, chunky chrome frame with a "
                    "hot-pink and black diagonal stripe accent along the bottom edge. " + FRAME + NO_FLOOR + NO_TEXT_GREEN),
    "plate_banner": ("A very wide announcement banner plate for a game HUD, five times as wide as it is tall, with diagonal "
                     "chevron cuts at both ends and hot-pink and chrome accent stripes. " + FRAME + NO_FLOOR + NO_TEXT_GREEN),
    "dash_strip": ("A very wide instrument dashboard strip for a game HUD, six times as wide as it is tall, black-cherry "
                   "lacquer with polished chrome ribs separating several empty flat black rectangular screen areas, a "
                   "chrome bevelled top edge. " + FRAME + NO_TEXT),
}

ELEMENTS = {
    "tab_plate": ("A wide horizontal blank name plate, landscape orientation, much wider than tall: a PURE WHITE glossy "
                  "enamel face inside a thin mirror-chrome bevelled edge with diagonal chamfered corners and a tiny chrome "
                  "screw at each end, completely empty face. " + PART + NO_TEXT),
    "button_square": ("A square push-button plate, PURE WHITE glossy enamel face with a thin mirror-chrome bevelled rim and "
                      "chamfered corners, completely empty face. " + PART + NO_TEXT),
    "readout_box": ("A wide horizontal pointer plate shaped like a thick arrow pointing RIGHT: a PURE WHITE glossy enamel "
                    "rectangle with a thin mirror-chrome bevel whose right end is a pointed chevron tip, landscape "
                    "orientation, completely empty face. " + PART + NO_TEXT),
    "tile_off": ("A wide horizontal annunciator lamp tile, landscape orientation, twice as wide as tall, UNLIT: dark "
                 "black-cherry smoked-glass face inside a thin mirror-chrome frame with tiny screws, completely empty "
                 "face. " + PART + NO_TEXT_GREEN),
    "bar_track": ("A long horizontal recessed slot, landscape orientation, very wide and thin: a dark black-cherry groove "
                  "with a bevelled mirror-chrome lip all around, empty, seen exactly from the front. " + PART + NO_TEXT_GREEN),
    "screen_glass": ("A rectangular dark instrument screen, three units wide by two tall: nearly black smoked glass with a "
                     "faint deep-cherry vignette, inside a thin mirror-chrome frame with four tiny hex screws in the "
                     "corners, completely empty. " + PART + NO_TEXT),
    "radar_bezel": ("A round radar scope bezel: a thick circular bevelled mirror-chrome ring reflecting hot pink, with hex "
                    "screws and a thin hot-pink accent groove, the centre of the ring is EMPTY flat pure black, seen "
                    "exactly from the front, isolated. " + PART + NO_TEXT),
    "banner_burst": ("A wide comic-book style BANG splash burst shape, landscape, three times as wide as tall: jagged "
                     "starburst silhouette filled with a hot-pink to deep magenta gradient, a thick mirror-chrome outline "
                     "and a thin black outer line, a few small white sparks around it, bold flat bonus splash, completely "
                     "empty inside, isolated on a pure black background. ABSOLUTELY NO TEXT of any kind: no letters, no "
                     "numbers, no logos."),
    "banner_rays": ("A round sunburst wheel of radiating rays, alternating hot pink and pale blush-white rays fading out "
                    "toward the edge, centred, circular, victory background, empty centre, isolated on a pure black "
                    "background. ABSOLUTELY NO TEXT of any kind: no letters, no numbers, no logos."),
}

GREEN = {"tile_off", "tile_on", "bar_track"}

TILE_ON_EDIT = ("Light this exact annunciator tile up: its smoked glass face now glows an even bright PURE WHITE, evenly "
                "lit from behind, edge to edge. Change nothing else: same chrome frame, same screws, same size, same "
                "position, same flat pure green background, do not crop, do not zoom. ABSOLUTELY NO TEXT of any kind.")


def despill(dst):
    """The key leaves the model's green-tinted shadow around dark parts: clear anything clearly greener than it is
    red or blue, then crop to what is left."""
    from PIL import Image
    import numpy as np
    a = np.asarray(Image.open(dst).convert("RGBA")).copy()
    r, g, b = (a[:, :, i].astype(np.int16) for i in range(3))
    spill = (g > np.maximum(r, b) + 30)
    a[:, :, 3][spill] = 0
    im = K.largest_component(Image.fromarray(a, "RGBA"))
    bbox = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bbox:
        im = im.crop(bbox)
    im.save(dst, "PNG", optimize=True)
    K.log(f"   despilled: {int(spill.sum())} px, {im.size[0]}x{im.size[1]}")


def plan(only):
    items = []
    for pid, prompt in PLATES.items():
        items.append(("image", pid, dict(prompt=prompt, size=1024, square=False)))
    for eid, prompt in ELEMENTS.items():
        big = eid.startswith("banner_")
        items.append(("image", eid, dict(prompt=prompt, size=1024 if big else 512, square=False,
                                         keyall=(eid == "radar_bezel"), largest=not big,
                                         radial_fade=(eid == "banner_rays"))))
    items.append(("edit", "tile_on", dict(prompt=TILE_ON_EDIT, base="tile_off", size=512, square=False, largest=True)))
    return [it for it in items if not only or it[1] in only]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--reprocess", action="store_true")
    ap.add_argument("--sheet", default="")
    a = ap.parse_args()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    items = plan(only)
    os.makedirs(DST, exist_ok=True)

    if a.sheet:
        K.contact_sheet([(k, "pk_" + i, dict(s, dst=os.path.join(DST, i + ".png"))) for k, i, s in items], a.sheet)
        return 0

    c = K.Campaign(force=a.force, dry=a.dry_run, reprocess=a.reprocess)
    before = c.k.credits() if not (a.dry_run or a.reprocess) else None
    K.log(f"balance before: {before}")
    failures = []
    for kind, id_, s in items:
        dst = os.path.join(DST, id_ + ".png")
        try:
            if kind == "image":
                c.image("pk_" + id_, s["prompt"], dst, s["size"], square=s["square"], keyall=s.get("keyall", False),
                        largest=s.get("largest", False), radial_fade=s.get("radial_fade", False))
            else:
                base = os.path.join(K.RAW, "pk_" + s["base"] + ".png")
                if not K.ok_file(base) and not a.dry_run:
                    raise RuntimeError("base missing: pk_" + s["base"])
                c.image("pk_" + id_, s["prompt"], dst, s["size"], model=K.EDIT_MODEL, base=base, square=s["square"],
                        largest=s["largest"], mask_src=base)
            if not a.dry_run and K.ok_file(dst) and (id_ in GREEN or s["prompt"].find("green") >= 0):
                despill(dst)
        except Exception as e:  # noqa: BLE001
            failures.append((id_, str(e)))
            K.log(f"[{id_}] FAILED: {e}")
    if not (a.dry_run or a.reprocess):
        K.log(f"balance after: {c.k.credits()} (before {before})")
    for f in failures:
        K.log("FAILED", *f)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
