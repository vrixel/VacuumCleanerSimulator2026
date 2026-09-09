"""Stacks the line-up sheets rendered by the player (-lineup) into docs/screenshots/models-lineup.png,
with the measured height of each machine written under its row (read back from Builds/lineup.log)."""
import os
import re
import glob
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Builds", "lineup")
LOG = os.path.join(ROOT, "Builds", "lineup.log")
OUT = os.path.join(ROOT, "docs", "screenshots", "models-lineup.png")
PER_SHEET = 5

sheets = sorted(glob.glob(os.path.join(SRC, "lineup-*.png")),
                key=lambda f: int(re.findall(r"lineup-(\d+)", f)[0]))
if not sheets:
    raise SystemExit("no lineup-*.png in " + SRC)

names = []
if os.path.exists(LOG):
    for line in open(LOG, encoding="utf-8", errors="ignore"):
        m = re.search(r"\[VCS\] Lineup (\S+)\s+(built|mesh)\s+w ([\d.]+) h ([\d.]+) d ([\d.]+)", line)
        if m and m.group(1) not in [n[0] for n in names]:
            names.append((m.group(1), m.group(4)))

band = 34
w, h = Image.open(sheets[0]).size
sheet = Image.new("RGB", (w, (h + band) * len(sheets)), (18, 18, 20))
d = ImageDraw.Draw(sheet)
for i, f in enumerate(sheets):
    y = i * (h + band)
    row = names[i * PER_SHEET:(i + 1) * PER_SHEET]
    caption = "   ".join("%s %s m" % (n, ht) for n, ht in row) or os.path.basename(f)
    d.text((10, y + 10), caption, fill=(245, 200, 60))
    sheet.paste(Image.open(f), (0, y + band))

os.makedirs(os.path.dirname(OUT), exist_ok=True)
sheet.save(OUT)
print("wrote", OUT, sheet.size)
