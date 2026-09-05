#!/usr/bin/env python3
"""Art-direction concepts for the vacuums (not used by the game): one photoreal industrial-design render per machine
in an angular, grooved, electronic style, plus a line-up. Outputs docs/concepts/<id>.png and docs/concepts/sheet.png.

    python tools\assets\concepts.py [--only harold] [--force] [--dry-run]

Same kie.ai discipline as kie_assets.py (raw downloads kept, idempotent, balance printed).
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import kie_assets as K  # noqa: E402
from PIL import Image, ImageDraw, ImageFont  # noqa: E402

OUT = os.path.join(K.ROOT, "docs", "concepts")

STYLE = ("Photorealistic industrial design concept render of a vacuum cleaner. Angular faceted body panels instead of "
         "round smooth shapes, deep horizontal grooves and panel seams, textured matte polymer with fine grain, brushed "
         "aluminium and dark anodised metal accents, exposed hex screws, rubber bumper strips, small LED status "
         "indicators and a tiny monochrome display, visible vent grilles. Consumer electronics feel, like a premium "
         "power tool. Studio three-point lighting on a neutral dark grey backdrop, subtle floor reflection, "
         "three-quarter front view, sharp focus, 8k product photography. No text, no logos, no people.")

CONCEPTS = {
    "dusty": "A boxy prototype canister vacuum: dark graphite polymer panels bolted together, a translucent smoked "
             "dust-bag housing on the back, a wide low nozzle head with a rubber lip, chunky black rubber wheels, one "
             "orange accent stripe and a single amber LED. " + STYLE,
    "roomboo": "An octagonal robot vacuum: flat faceted top in brushed steel with a recessed illuminated ring, a "
               "sensor turret, a segmented rubber bumper with a groove, a three-armed side brush, ribbed dark "
               "polymer sides, teal LED accents. " + STYLE,
    "cyclonic": "A bagless upright vacuum on a ball: faceted transparent bin showing sharp cyclone fins, purple "
                "anodised aluminium cyclone cap, ribbed hose, a ball-shaped head with grooves, a small LCD battery "
                "and suction display, dark grey polymer body. " + STYLE,
    "harold": "A cylindrical canister vacuum with a faceted drum: matte red powder-coated body with vertical "
              "grooves, a riveted dark metal lid with a carry handle, chrome latches, a subtle friendly face printed "
              "as a flat decal (two dots and a smile), ribbed grey hose, four rubber casters. " + STYLE,
    "stick": "A cordless stick vacuum: angular motor unit with heat-sink fins and a battery pack, translucent bin, "
             "brushed aluminium wand, a floor head with an LED headlight bar, cyan LED trigger indicator, dark "
             "grey and electric blue polymer. " + STYLE,
    "grandma": "A vintage 1978 upright vacuum reinterpreted: avocado green textured housing with chrome trim, a "
               "faceted motor cowl with grille slots, a headlight, a tan cloth dust bag with a zipper, a bakelite "
               "handle, steel wheels with rubber tyres. " + STYLE,
    "rowinta": "A compact French canister vacuum: dark navy faceted body with fine ribbed grooves, a tricolour blue "
               "white red stripe, a silence-mode rotary dial, a telescopic brushed steel tube, a slim parquet floor "
               "head, small rear wheels. " + STYLE,
    "shopdrum": "A wet and dry workshop vacuum: yellow rotomoulded drum with deep horizontal grooves, a black "
                "motor head with wide vent slots, steel latches, an oversized rubber caster base, a thick ribbed "
                "hose, an industrial power switch with an LED. " + STYLE,
    "lineup": "Eight vacuum cleaners in a row like a product family photo: a boxy graphite prototype canister, an "
              "octagonal robot vacuum, a purple bagless upright on a ball, a red faceted canister with a tiny decal "
              "face, a blue cordless stick, an avocado green 1978 upright with a cloth bag, a navy French canister "
              "with a tricolour stripe, and a yellow grooved wet-and-dry drum. All share one design language: angular "
              "faceted panels, grooves and seams, textured matte polymer, brushed metal accents, small LEDs. Wide "
              "landscape studio shot on a neutral dark grey backdrop, three-point lighting. No text, no logos.",
}
NAMES = {"dusty": "Dusty", "roomboo": "Roomboo S9", "cyclonic": "Cyclonic V-Storm", "harold": "Harold",
         "stick": "Stickmaster", "grandma": "Grandma's Upright 1978", "rowinta": "Rowinta Silence Farce",
         "shopdrum": "Shop Drum 3000", "lineup": "Line-up"}


def sheet(path):
    font_path = os.path.join(K.ROOT, "Assets", "Resources", "Fonts", "RussoOne.ttf")
    try:
        font = ImageFont.truetype(font_path, 30)
    except Exception:
        font = ImageFont.load_default()
    ids = [i for i in CONCEPTS if i != "lineup"]
    cell, label, gap = 620, 48, 14
    cols = 4
    rows = (len(ids) + cols - 1) // cols
    lineup = os.path.join(OUT, "lineup.png")
    lineup_h = 0
    lineup_im = None
    if os.path.exists(lineup):
        lineup_im = Image.open(lineup).convert("RGB")
        w = cols * cell + (cols - 1) * gap
        lineup_im = lineup_im.resize((w, int(lineup_im.height * w / lineup_im.width)), Image.LANCZOS)
        lineup_h = lineup_im.height + gap + label
    W = cols * cell + (cols + 1) * gap
    H = 80 + rows * (cell + label + gap) + lineup_h + gap
    im = Image.new("RGB", (W, H), (16, 16, 20))
    d = ImageDraw.Draw(im)
    d.text((gap, 22), "VACUUM CLEANER SIMULATOR 2026  |  art direction concepts: angular, grooved, electronic", fill=(255, 212, 0), font=font)
    y = 80
    for n, cid in enumerate(ids):
        r, c = divmod(n, cols)
        x = gap + c * (cell + gap)
        yy = y + r * (cell + label + gap)
        p = os.path.join(OUT, cid + ".png")
        if os.path.exists(p):
            src = Image.open(p).convert("RGB")
            sw, sh = src.size
            s = min(cell / sw, cell / sh)
            src = src.resize((int(sw * s), int(sh * s)), Image.LANCZOS)
            im.paste(src, (x + (cell - src.width) // 2, yy + label + (cell - src.height) // 2))
        d.text((x, yy + 8), NAMES[cid], fill=(255, 255, 255), font=font)
    if lineup_im is not None:
        yy = y + rows * (cell + label + gap)
        d.text((gap, yy + 8), "Line-up, one design language", fill=(255, 255, 255), font=font)
        im.paste(lineup_im, (gap, yy + label))
    im.save(path, optimize=True)
    print("sheet", path, im.size)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", action="append")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--sheet-only", action="store_true")
    a = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    if not a.sheet_only:
        kie = K.Campaign(force=a.force, dry=a.dry_run)
        for cid, prompt in CONCEPTS.items():
            if a.only and cid not in a.only:
                continue
            dst = os.path.join(OUT, cid + ".png")
            kie.image("concept_" + cid, prompt, dst, 2048, key=False, square=False)
    sheet(os.path.join(OUT, "sheet.png"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
