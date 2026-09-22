#!/usr/bin/env python3
"""The wordmark and the icon candidates, composed in PIL (2026-09-22, his "the logo I dont like too much").

    python tools/wordmark.py            # everything below
    python tools/wordmark.py --sheet    # only the icon contact sheet

Writes:
  marketing/logo/wordmark.png            stacked VACUUM CLEANER / SIMULATOR / 2026, transparent, 1600 wide
  marketing/logo/wordmark-line.png       the one-line version for banners and video cards
  marketing/logo/wordmark-on-dark.png, -on-light.png   previews
  marketing/icon-candidates/<name>-bare.png   each kie candidate made edge to edge (the models draw rounded corners
                                              and a white margin; the stores apply their own mask), 1024 square
  marketing/icon-candidates/<name>-badge.png  the same with a compact SIMULATOR 2026 badge in the lower band
  marketing/icon-candidates/sheet.png         every candidate at 512, 128 and 64 px next to the shipped icon,
                                              bare and badged: the picture to choose from

Nothing here replaces the shipped icon (marketing/icon, Assets/Icon/icon.png): that is his pick, then
`python tools/marketing.py` cuts the sizes from marketing/source/icon.png. Since 2026-09-22 the shipped icon is
sled_navy_clean bare (copied there by hand), so the sheet's first row and that candidate are the same picture.
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import brand  # noqa: E402

ROOT = brand.ROOT
LOGO = os.path.join(ROOT, "marketing", "logo")
CAND = os.path.join(ROOT, "marketing", "icon-candidates")
SHIPPED = os.path.join(ROOT, "marketing", "source", "icon.png")
NAMES = ["chaos", "vortex", "cat", "sticker", "sled_blue", "sled_cyan", "sled_navy",
         "sled_navy_clean", "sled_navy_clean_nb", "sled_navy_plain"]
INSET = {"sled_navy_plain": 0.075}   # that take drew a thicker black margin


def edge_to_edge(im, inset=0.045):
    """Crop the rounded-corner margin the models leave and fill the frame again.

    The models draw their own rounded icon on white, with a radius the inset does not reach, so what is left of
    each white corner is flooded with the background colour sampled at the middle of the nearest edge (the
    Microsoft Store applies no mask: a bare icon must be square to the last pixel)."""
    im = im.convert("RGBA")
    w, h = im.size
    box = (int(w * inset), int(h * inset), int(w * (1 - inset)), int(h * (1 - inset)))
    out = im.crop(box).resize((1024, 1024), Image.LANCZOS).convert("RGB")
    W, H = out.size
    for cx, cy, ex, ey in ((0, 0, W // 2, 2), (W - 1, 0, W // 2, 2), (0, H - 1, W // 2, H - 3), (W - 1, H - 1, W // 2, H - 3)):
        c, e = out.getpixel((cx, cy)), out.getpixel((ex, ey))
        if sum(abs(a - b) for a, b in zip(c, e)) > 90:   # a leftover margin (white, or black on the navy takes)
            ImageDraw.floodfill(out, (cx, cy), e, thresh=60)
    return out.convert("RGBA")


def badge(im):
    """A compact wordmark band in the lower part of the icon: SIMULATOR big, VACUUM CLEANER small, 2026 tab."""
    out = im.copy()
    w, h = out.size
    band_h = int(h * 0.27)
    plate = Image.new("RGBA", (w, band_h), (0, 0, 0, 0))
    d = ImageDraw.Draw(plate)
    # a dark wedge, slightly tilted like the HUD tabs, fading nothing (no glow, no gradient)
    d.polygon([(0, int(band_h * 0.18)), (w, 0), (w, band_h), (0, band_h)], fill=brand.PANEL + (235,))
    out.alpha_composite(plate, (0, h - band_h))
    rule = brand.stripes(w, max(4, h // 110))
    out.alpha_composite(rule, (0, h - band_h - rule.size[1] // 2 + int(band_h * 0.09)))
    small = brand.arcade_text("VACUUM CLEANER", int(h * 0.058), brand.WHITE, depth=4, edge_w=4)
    year = brand.tab("2026", int(h * 0.046), brand.BLUE, skew=True)
    gap = int(h * 0.012)
    size = int(h * 0.13)
    while True:   # SIMULATOR + the year tab must sit inside 86 % of the width
        big = brand.arcade_text("SIMULATOR", size, brand.YELLOW, depth=8, edge_w=7)
        if big.size[0] + gap + year.size[0] <= w * 0.86 or size < 40:
            break
        size -= 4
    cx = w // 2
    y0 = h - band_h + int(band_h * 0.2)
    out.alpha_composite(small, (cx - small.size[0] // 2, y0))
    row_w = big.size[0] + gap + year.size[0]
    x = cx - row_w // 2
    yb = y0 + small.size[1] - 2
    out.alpha_composite(big, (x, yb))
    out.alpha_composite(year, (x + big.size[0] + gap, yb + big.size[1] - year.size[1] - 6))
    return out


def rounded(im, radius):
    """Preview only: the stores' own corner mask, so the sheet shows what a phone shows."""
    mask = Image.new("L", im.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, im.size[0] - 1, im.size[1] - 1), radius=radius, fill=255)
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    out.paste(im, (0, 0), mask)
    return out


def sheet(candidates):
    """Rows: shipped icon then each candidate; columns: bare 512, badged 512, then 128 and 64 of each."""
    rows = [("shipped", Image.open(SHIPPED).convert("RGBA").resize((1024, 1024)), None)] + candidates
    col_w = [512, 512, 128, 128, 64, 64]
    pad = 24
    label_h = 44
    w = sum(col_w) + pad * (len(col_w) + 1) + 220
    h = pad + (512 + label_h + pad) * len(rows)
    im = Image.new("RGBA", (w, h), (36, 40, 48, 255))
    y = pad
    for name, bare, badged in rows:
        if badged is None:
            badged = bare
        x = pad
        size = 34
        while True:   # long names shrink to fit the label column
            lab = brand.plain_text(name.upper(), size, brand.YELLOW, "RussoOne")
            if lab.size[0] <= 210 or size <= 16:
                break
            size -= 2
        im.alpha_composite(lab, (x, y + 200))
        x += 220
        for i, (src, size) in enumerate([(bare, 512), (badged, 512), (bare, 128), (badged, 128), (bare, 64), (badged, 64)]):
            tile = rounded(src.resize((size, size), Image.LANCZOS), int(size * 0.22))
            im.alpha_composite(tile, (x, y + (512 - size) if size < 512 else y))
            x += size + pad
        cap = brand.plain_text("bare / badged at 512, 128, 64 px, with the store corner mask", 24, brand.STEEL, "Exo2")
        im.alpha_composite(cap, (pad + 220, y + 512 + 8))
        y += 512 + label_h + pad
    return im


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--sheet", action="store_true", help="only the icon contact sheet")
    a = ap.parse_args()
    os.makedirs(LOGO, exist_ok=True)
    os.makedirs(CAND, exist_ok=True)
    if not a.sheet:
        wm = brand.wordmark(1600)
        wm.save(os.path.join(LOGO, "wordmark.png"))
        line = brand.wordmark_line(160)
        line.save(os.path.join(LOGO, "wordmark-line.png"))
        for tag, colour in (("dark", (20, 23, 31)), ("light", (236, 238, 242))):
            prev = Image.new("RGBA", (wm.size[0] + 200, wm.size[1] + line.size[1] + 300), colour + (255,))
            prev.alpha_composite(brand.drop_shadow(wm, 22, (0, 16), 160), (100 - 66, 100 - 66))
            prev.alpha_composite(line, (100, wm.size[1] + 200))
            prev.save(os.path.join(LOGO, f"wordmark-on-{tag}.png"))
        print("wordmark", wm.size, "line", line.size)
    candidates = []
    for n in NAMES:
        src = os.path.join(CAND, n + ".png")
        if not os.path.exists(src):
            print("missing candidate", src)
            continue
        bare = edge_to_edge(Image.open(src), INSET.get(n, 0.045))
        badged = badge(bare)
        if not a.sheet:
            bare.save(os.path.join(CAND, f"{n}-bare.png"))
            badged.save(os.path.join(CAND, f"{n}-badge.png"))
        candidates.append((n, bare, badged))
    sh = sheet(candidates)
    sh.save(os.path.join(CAND, "sheet.png"))
    print("sheet", sh.size, "->", os.path.relpath(os.path.join(CAND, "sheet.png"), ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
