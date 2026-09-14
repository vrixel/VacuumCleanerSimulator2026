#!/usr/bin/env python3
"""A contact sheet for sounds: one waveform strip per clip (ffmpeg showwavespic) with its name and length, so a
take can be looked at before it ships: a "single meow" that came back as four cries in a row, a hose hit whose
first event is 6 s in, a loop that swells. Raw takes by default (take 1 and take 2 side by side), or the
processed wavs with --processed.

    python tools/assets/audio_sheet.py                 # tools/assets/raw/contact-audio.png, every raw sfx
    python tools/assets/audio_sheet.py --processed     # Assets/Resources/Audio/Sfx/*.wav
    python tools/assets/audio_sheet.py meow yowl absorb   # name filters
"""
import argparse
import os
import subprocess
import sys
import tempfile

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "tools", "assets", "raw")
SFX = os.path.join(ROOT, "Assets", "Resources", "Audio", "Sfx")
W, H, GAP = 900, 96, 8
SECONDS = 16.0  # every strip shares one time scale, so lengths compare at a glance


def duration(path):
    return float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                          "-of", "default=nw=1:nk=1", path]).decode().strip())


def strip(path, out):
    d = duration(path)
    w = max(4, int(W * min(d, SECONDS) / SECONDS))
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", path,
                    "-lavfi", f"showwavespic=s={w}x{H}:colors=#ffd23a:scale=lin:split_channels=0", "-frames:v", "1", out],
                    check=True)
    return d


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("filters", nargs="*")
    ap.add_argument("--processed", action="store_true")
    ap.add_argument("--out", default="")
    ap.add_argument("--dir", default="", help="read raw takes from this folder instead of tools/assets/raw")
    a = ap.parse_args()
    if a.processed:
        files = sorted(os.path.join(SFX, f) for f in os.listdir(SFX) if f.endswith(".wav"))
    else:
        src = a.dir or RAW
        files = sorted(os.path.join(src, f) for f in os.listdir(src) if f.startswith("sfx_") and f.endswith(".mp3"))
    if a.filters:
        files = [f for f in files if any(k in os.path.basename(f) for k in a.filters)]
    if not files:
        print("nothing matches")
        return 1
    out = a.out or os.path.join(RAW, "contact-audio-processed.png" if a.processed else "contact-audio.png")
    label_w = 230
    sheet = Image.new("RGB", (label_w + W + GAP, len(files) * (H + GAP) + GAP), (24, 26, 30))
    draw = ImageDraw.Draw(sheet)
    with tempfile.TemporaryDirectory() as td:
        for i, f in enumerate(files):
            png = os.path.join(td, f"{i}.png")
            d = strip(f, png)
            y = GAP + i * (H + GAP)
            name = os.path.basename(f).rsplit(".", 1)[0]
            draw.text((8, y + 30), name, fill=(235, 235, 235))
            draw.text((8, y + 50), f"{d:.2f} s", fill=(160, 200, 255))
            draw.rectangle([label_w, y, label_w + W, y + H], fill=(38, 40, 46))
            for s in range(0, int(SECONDS) + 1):
                x = label_w + int(W * s / SECONDS)
                draw.line([x, y, x, y + H], fill=(60, 62, 70))
            sheet.paste(Image.open(png), (label_w, y))
    sheet.save(out)
    print("wrote", os.path.relpath(out, ROOT), "with", len(files), "clips")
    return 0


if __name__ == "__main__":
    sys.exit(main())
