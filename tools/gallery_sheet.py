"""Composes Builds/gallery/before-*.png and after-*.png into docs/screenshots/models-before-after.png."""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "Builds", "gallery")
OUT = os.path.join(ROOT, "docs", "screenshots", "models-before-after.png")
ORDER = [("dusty", "Dusty"), ("roomboo", "Roomboo S9"), ("cyclonic", "Cyclonic V-Storm"), ("harold", "Harold"),
         ("stick", "Stickmaster"), ("grandma", "Grandma's Upright 1978"), ("rowinta", "Rowinta Silence Farce"), ("shopdrum", "Shop Drum 3000")]
CELL, LABEL, GAP = 480, 54, 16
font_path = os.path.join(ROOT, "Assets", "Resources", "Fonts", "RussoOne.ttf")
try:
    font = ImageFont.truetype(font_path, 26)
    small = ImageFont.truetype(font_path, 20)
except Exception:
    font = small = ImageFont.load_default()

cols, rows = 2, 4
W = cols * (2 * CELL + GAP) + (cols + 1) * GAP
H = rows * (CELL + LABEL + GAP) + GAP + 70
sheet = Image.new("RGB", (W, H), (18, 18, 24))
d = ImageDraw.Draw(sheet)
d.text((GAP, 18), "VACUUM CLEANER SIMULATOR 2026  |  models: before (v0.2 shells)  vs  after (v2: faceted, grooved, electronic)", fill=(255, 212, 0), font=font)
for i, (mid, name) in enumerate(ORDER):
    r, c = divmod(i, cols)
    x0 = GAP + c * (2 * CELL + 2 * GAP)
    y0 = 70 + GAP + r * (CELL + LABEL + GAP)
    for k, tag in enumerate(("before", "after")):
        p = os.path.join(SRC, f"{tag}-{mid}.png")
        x = x0 + k * (CELL + GAP)
        if os.path.exists(p):
            im = Image.open(p).convert("RGB").resize((CELL, CELL), Image.LANCZOS)
            sheet.paste(im, (x, y0 + LABEL))
        d.text((x, y0 + 26), tag.upper(), fill=(200, 200, 210) if k == 0 else (90, 200, 255), font=small)
    d.text((x0, y0), name, fill=(255, 255, 255), font=font)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
sheet.save(OUT, optimize=True)
print(OUT, sheet.size)
