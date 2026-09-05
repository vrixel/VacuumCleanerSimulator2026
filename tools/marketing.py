#!/usr/bin/env python3
"""Cuts the generated marketing sources (marketing/source) into store sizes and app icons.

    python tools/marketing.py

Sources (from tools/assets/kie_assets.py): key_art.png, hero_wide.png, library_portrait.png, icon.png,
garage_lineup.png. Missing sources are skipped with a note. Outputs go to marketing/store, marketing/icon and
Assets/Icon (the in-game icon picked up by ProjectSetup).
"""
import os
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "marketing", "source")
STORE = os.path.join(ROOT, "marketing", "store")
ICON = os.path.join(ROOT, "marketing", "icon")
GAME_ICON = os.path.join(ROOT, "Assets", "Icon")


def load(name):
    p = os.path.join(SRC, name)
    if not os.path.exists(p):
        print(f"missing source: {name}")
        return None
    return Image.open(p).convert("RGB")


def cover(im, w, h, anchor=(0.5, 0.5)):
    """Scales to cover w x h then crops around the anchor (0..1 fractions)."""
    sw, sh = im.size
    scale = max(w / sw, h / sh)
    nw, nh = int(round(sw * scale)), int(round(sh * scale))
    im = im.resize((nw, nh), Image.LANCZOS)
    x = int((nw - w) * anchor[0])
    y = int((nh - h) * anchor[1])
    return im.crop((x, y, x + w, y + h))


def save(im, folder, name):
    os.makedirs(folder, exist_ok=True)
    p = os.path.join(folder, name)
    im.save(p, "PNG", optimize=True)
    print(f"{os.path.relpath(p, ROOT)}  {im.size[0]}x{im.size[1]}")


def main():
    key = load("key_art.png")
    hero = load("hero_wide.png") or key
    portrait = load("library_portrait.png") or key
    icon = load("icon.png")

    if key is not None:
        save(key, STORE, "key_art.png")
        save(cover(key, 920, 430, (0.5, 0.55)), STORE, "steam_header.png")
        save(cover(key, 462, 174, (0.5, 0.55)), STORE, "steam_small.png")
        save(cover(key, 1232, 706, (0.5, 0.5)), STORE, "steam_main.png")
        save(cover(key, 1200, 630, (0.5, 0.5)), STORE, "og_image.png")
    if hero is not None:
        save(cover(hero, 3840, 1240, (0.5, 0.5)), STORE, "steam_hero.png")
        save(cover(hero, 1920, 620, (0.5, 0.5)), STORE, "site_banner.png")
    if portrait is not None:
        save(cover(portrait, 600, 900, (0.5, 0.4)), STORE, "steam_library.png")
    if icon is not None:
        sq = cover(icon, 1024, 1024)
        save(sq, ICON, "icon_1024.png")
        for s in (512, 300, 256, 192, 128, 64, 48, 32, 16):
            save(sq.resize((s, s), Image.LANCZOS), ICON, f"icon_{s}.png")
        save(sq.resize((300, 300), Image.LANCZOS), STORE, "ms_icon_300.png")
        ico_sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
        os.makedirs(ICON, exist_ok=True)
        sq.save(os.path.join(ICON, "icon.ico"), format="ICO", sizes=ico_sizes)
        print(f"marketing/icon/icon.ico  {len(ico_sizes)} sizes")
        save(sq.resize((512, 512), Image.LANCZOS), GAME_ICON, "icon.png")
    lineup = load("garage_lineup.png")
    if lineup is not None:
        save(lineup, STORE, "garage_lineup.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
