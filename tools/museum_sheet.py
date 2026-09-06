"""Composes Builds/museum/museum-*.png (two views per piece) into docs/screenshots/museum-orientation.png."""
import glob, os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Builds", "museum")
DST = os.path.join(ROOT, "docs", "screenshots", "museum-orientation.png")
CELL = 384
files = sorted(glob.glob(os.path.join(SRC, "museum-*-a.png")))
ids = [os.path.basename(f)[len("museum-"):-len("-a.png")] for f in files]
cols = 2
rows = (len(ids) + cols - 1) // cols
sheet = Image.new("RGB", (cols * 3 * CELL, rows * (CELL + 28)), (30, 30, 34))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype(os.path.join(ROOT, "Assets", "Resources", "Fonts", "RussoOne.ttf"), 20)
except OSError:
    font = ImageFont.load_default()
for i, id_ in enumerate(ids):
    r, c = divmod(i, cols)
    x0, y0 = c * 3 * CELL, r * (CELL + 28)
    for k, view in enumerate(("a", "b", "top")):
        im = Image.open(os.path.join(SRC, f"museum-{id_}-{view}.png")).resize((CELL, CELL), Image.LANCZOS)
        sheet.paste(im, (x0 + k * CELL, y0 + 28))
    draw.text((x0 + 8, y0 + 4), f"{id_}   (yaw -35 | yaw 145 | top: +z up, +x right, ticks 0.25 m)", fill=(255, 220, 60), font=font)
os.makedirs(os.path.dirname(DST), exist_ok=True)
sheet.save(DST)
print(DST, sheet.size)
