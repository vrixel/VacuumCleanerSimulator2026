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


def poster_from_icon(icon, w=720, h=1080):
    """2:3 poster from the square icon: a cyan ray burst in the icon's own colours (sampled from its corners)
    fills the frame, the icon sits low at full width and the wordmark takes the top. A blurred stretch of the
    icon was tried first and read as mud."""
    import math
    from PIL import ImageDraw, ImageFilter
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import brand
    px = icon.convert("RGB")
    c1 = px.getpixel((8, 8)); c2 = px.getpixel((px.size[0] - 9, 8))
    base = tuple((x + y) // 2 for x, y in zip(c1, c2))
    bg = Image.new("RGB", (w, h), base)
    rays = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(rays)
    cx, cy, n = w / 2, h - w / 2 - 40, 28
    for i in range(n):
        a0 = 2 * math.pi * i / n; a1 = a0 + math.pi / n * 0.55
        d.polygon([(cx, cy), (cx + 2000 * math.cos(a0), cy + 2000 * math.sin(a0)), (cx + 2000 * math.cos(a1), cy + 2000 * math.sin(a1))], fill=90)
    rays = rays.filter(ImageFilter.GaussianBlur(3))
    bg = Image.composite(Image.new("RGB", (w, h), (255, 255, 255)), bg, rays).convert("RGBA")
    art = icon.convert("RGBA").resize((w, w), Image.LANCZOS)
    fade = Image.linear_gradient("L").resize((w, 160))   # the icon's top edge melts into the burst, no seam
    mask = Image.new("L", (w, w), 255); mask.paste(fade, (0, 0))
    art.putalpha(mask)
    bg.alpha_composite(art, (0, h - w - 40))
    wm = brand.drop_shadow(brand.wordmark(int(w * 0.86)), 14, (0, 8), 200)
    bg.alpha_composite(wm, ((w - wm.size[0]) // 2, max(10, (h - w - 40 - wm.size[1]) // 2)))
    return bg.convert("RGB")


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
    if hero is not None:
        save(cover(hero, 1200, 630, (0.5, 0.5)), STORE, "og_image.png")   # from the wide hero since 2026-09-22 (a 16:9 source)
        save(cover(hero, 3840, 1240, (0.5, 0.5)), STORE, "steam_hero.png")
        save(cover(hero, 1920, 620, (0.5, 0.42)), STORE, "site_banner.png")
    if portrait is not None:
        save(cover(portrait, 600, 900, (0.5, 0.4)), STORE, "steam_library.png")
    # Microsoft Store listing art (2026-09-06): poster from the portrait, box art from the key art, heroes from the wide one.
    # Since 0.4.10 (2026-10-19, the cat icon and his "weak conversion"): the box art is the icon, the poster is the icon
    # on its own burst under the wordmark, and the heroes and the link preview are the cat chase of the action set, so
    # every big picture shows the gag the icon promises instead of a machine posing in a navy studio.
    portrait = load("library_portrait.png")
    hero = load("hero_wide.png")
    key = load("key_art.png")
    gag = load(os.path.join("action", "cat.png"))
    if gag is not None:
        hero = gag
        save(cover(gag, 1200, 630, (0.5, 0.5)), STORE, "og_image.png")
    if icon is not None:
        save(poster_from_icon(icon), STORE, "ms_poster_720x1080.png")
        save(cover(icon, 1080, 1080), STORE, "ms_boxart_1080.png")
    else:
        if portrait is not None: save(cover(portrait, 720, 1080), STORE, "ms_poster_720x1080.png")
        if key is not None: save(cover(key, 1080, 1080), STORE, "ms_boxart_1080.png")
    if hero is not None:
        save(cover(hero, 1920, 1080, (0.5, 0.6)), STORE, "ms_superhero_1920x1080.png")
        save(cover(hero, 2400, 1200, (0.5, 0.6)), STORE, "ms_hero_2400x1200.png")
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
