#!/usr/bin/env python3
"""The game's typography and colours for PIL: the same arcade look as the HUD (UIStyle.ArcadeText, 2026-09-05:
Russo One italic, hard black edge, block shadow, safety yellow / electric blue / red / white, no glow), so the
store pictures, the wordmark, the captions and the video cards all speak like the game. Imported by
tools/wordmark.py, tools/store_shots.py and tools/store_video.py.
"""
import os

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
FONTS = os.path.join(ROOT, "Assets", "Resources", "Fonts")

YELLOW = (255, 214, 0)
BLUE = (0, 168, 255)
RED = (255, 56, 41)
GREEN = (77, 255, 89)
WHITE = (255, 255, 255)
INK = (10, 10, 15)
PANEL = (20, 23, 31)
STEEL = (199, 212, 230)
NAVY = (4, 14, 63)        # the store art backdrop (2026-09-22, his icon pick): deep navy under an electric-blue burst
GLOW = (0, 90, 230)


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name + ".ttf"), size)


def arcade(text, size, fill=WHITE, shadow=INK, edge=INK, depth=None, edge_w=None, fontname="RussoOne"):
    """Upright text with a hard edge and a block shadow, as one RGBA image cropped to its content."""
    f = font(fontname, size)
    depth = int(size * 0.09) if depth is None else depth
    edge_w = max(2, int(size * 0.06)) if edge_w is None else edge_w
    pad = depth + edge_w * 2 + int(size * 0.35)
    l, t, r, b = f.getbbox(text)
    w, h = r - l + pad * 2, b - t + pad * 2
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    ox, oy = pad - l, pad - t
    # the block: the same glyphs stamped along the diagonal, in the shadow colour
    for i in range(depth, 0, -1):
        d.text((ox + i, oy + i), text, font=f, fill=shadow, stroke_width=edge_w, stroke_fill=shadow)
    d.text((ox, oy), text, font=f, fill=fill, stroke_width=edge_w, stroke_fill=edge)
    return im.crop(im.getbbox())


def italic_shear(im, shear=0.18):
    w, h = im.size
    out = Image.new("RGBA", (w + int(h * shear) + 2, h), (0, 0, 0, 0))
    # x_out = x_in + shear * (h - y): the bottom stays, the top leans right
    sheared = im.transform((w + int(h * shear) + 2, h), Image.AFFINE, (1, -shear, shear * h, 0, 1, 0), Image.BICUBIC)
    out.alpha_composite(sheared)
    return out.crop(out.getbbox())


def arcade_text(text, size, fill=WHITE, shadow=INK, edge=INK, italic=True, depth=None, edge_w=None, fontname="RussoOne"):
    """The arcade headline: upright render, then a true shear when italic (the HUD's Russo One italic)."""
    im = arcade(text, size, fill, shadow, edge, depth, edge_w, fontname)
    return italic_shear(im) if italic else im


def plain_text(text, size, fill=WHITE, fontname="Exo2", edge=None, edge_w=0):
    f = font(fontname, size)
    l, t, r, b = f.getbbox(text)
    pad = edge_w + 4
    im = Image.new("RGBA", (r - l + pad * 2, b - t + pad * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    if edge is not None and edge_w > 0:
        d.text((pad - l, pad - t), text, font=f, fill=fill, stroke_width=edge_w, stroke_fill=edge)
    else:
        d.text((pad - l, pad - t), text, font=f, fill=fill)
    return im.crop(im.getbbox())


def tab(text, size, fill=YELLOW, ink=INK, pad_x=None, skew=True, fontname="RussoOne"):
    """A yellow tab label like the HUD's: ink text on a coloured parallelogram."""
    label = plain_text(text, size, ink, fontname)
    pad_x = int(size * 0.5) if pad_x is None else pad_x
    pad_y = int(size * 0.22)
    w, h = label.size[0] + pad_x * 2, label.size[1] + pad_y * 2
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(im).rectangle((0, 0, w - 1, h - 1), fill=fill)
    im.alpha_composite(label, (pad_x, pad_y))
    return italic_shear(im, 0.22) if skew else im


def wordmark(width=1600):
    """VACUUM CLEANER / SIMULATOR / 2026 stacked, transparent, scaled to `width`."""
    top = arcade_text("VACUUM CLEANER", 150, WHITE, INK, INK, True, depth=12, edge_w=8)
    mid = arcade_text("SIMULATOR", 236, YELLOW, INK, INK, True, depth=18, edge_w=10)
    year = tab("2026", 96, BLUE, INK, skew=True)
    gap = 14
    w = max(top.size[0], mid.size[0] + gap + year.size[0])
    h = top.size[1] + mid.size[1] - 6
    im = Image.new("RGBA", (w + 40, h + 40), (0, 0, 0, 0))
    x0 = 20
    im.alpha_composite(top, (x0 + 12, 20))
    im.alpha_composite(mid, (x0, 20 + top.size[1] - 6))
    im.alpha_composite(year, (x0 + mid.size[0] + gap, 20 + top.size[1] + mid.size[1] - year.size[1] - 24))
    im = im.crop(im.getbbox())
    s = width / im.size[0]
    return im.resize((width, int(im.size[1] * s)), Image.LANCZOS)


def stripes(width, height=10):
    """The HUD's double rule: yellow over blue."""
    im = Image.new("RGBA", (width, height * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, width, height - 1), fill=YELLOW)
    d.rectangle((0, height, width, height * 2 - 1), fill=BLUE)
    return im


def drop_shadow(im, radius=18, offset=(0, 14), alpha=170):
    """A soft shadow under an RGBA element (used under panels and the wordmark, never a glow on text)."""
    pad = radius * 3
    sh = Image.new("RGBA", (im.size[0] + pad * 2, im.size[1] + pad * 2), (0, 0, 0, 0))
    a = im.split()[3].point(lambda v: v * alpha // 255)
    sh.paste((0, 0, 0, 255), (pad + offset[0], pad + offset[1]), a)
    sh = sh.filter(ImageFilter.GaussianBlur(radius))
    sh.alpha_composite(im, (pad, pad))
    return sh


def vignette(size, strength=0.55, inner=0.55):
    """A dark vignette layer (RGBA) to compose over a picture."""
    w, h = size
    small = Image.new("L", (64, 64), 0)
    d = ImageDraw.Draw(small)
    d.ellipse((-8, -8, 71, 71), fill=255)
    small = small.filter(ImageFilter.GaussianBlur(10)).resize((w, h), Image.BICUBIC)
    a = small.point(lambda v: int((255 - v) * strength))
    layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    layer.putalpha(a)
    return layer


BLUEPRINT = os.path.join(ROOT, "marketing", "source", "blueprint_layer.png")


def studio(size, centre=(0.5, 0.5), radius=0.38, blueprint=0.0):
    """The navy studio of the store art: NAVY with an electric-blue radial glow, so cards match the key art.
    `blueprint` lays the technical drawing over it at that opacity (his pick of 2026-09-23: 0.45 on the cards, the
    icon stays plain). The layer is light-blue lines on transparency, cleaned of the figures the model wrote."""
    w, h = size
    small = Image.new("L", (64, 64), 0)
    d = ImageDraw.Draw(small)
    cx, cy = int(64 * centre[0]), int(64 * centre[1])
    r = int(64 * radius)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=255)
    small = small.filter(ImageFilter.GaussianBlur(14)).resize((w, h), Image.BICUBIC)
    glow = Image.new("RGBA", (w, h), GLOW + (255,))
    glow.putalpha(small.point(lambda v: int(v * 0.6)))
    im = Image.new("RGBA", (w, h), NAVY + (255,))
    im.alpha_composite(glow)
    if blueprint > 0 and os.path.exists(BLUEPRINT):
        bp = Image.open(BLUEPRINT).convert("RGBA").resize((w, h), Image.LANCZOS)
        r, g, b, a = bp.split()
        im.alpha_composite(Image.merge("RGBA", (r, g, b, a.point(lambda v: int(v * blueprint)))))
    return im


def wordmark_line(height=120):
    """VACUUM CLEANER SIMULATOR 2026 on one line (banners, video cards), transparent, scaled to `height`."""
    a = arcade_text("VACUUM CLEANER", 150, WHITE, INK, INK, True, depth=12, edge_w=8)
    b = arcade_text("SIMULATOR", 150, YELLOW, INK, INK, True, depth=12, edge_w=8)
    year = tab("2026", 70, BLUE, INK, skew=True)
    gap = 34
    w = a.size[0] + gap + b.size[0] + gap + year.size[0]
    h = max(a.size[1], b.size[1])
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    im.alpha_composite(a, (0, h - a.size[1]))
    im.alpha_composite(b, (a.size[0] + gap, h - b.size[1]))
    im.alpha_composite(year, (a.size[0] + gap + b.size[0] + gap, h - year.size[1] - 14))
    im = im.crop(im.getbbox())
    s = height / im.size[1]
    return im.resize((int(im.size[0] * s), height), Image.LANCZOS)


def skewed_plate(w, h, fill=PANEL, alpha=225, skew=0.22):
    """A dark parallelogram plate, the HUD's tab shape at panel size."""
    im = Image.new("RGBA", (w + int(h * skew), h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    o = int(h * skew)
    d.polygon([(o, 0), (w + o, 0), (w, h), (0, h)], fill=fill + (alpha,))
    return im


def caption(headline, subline=None, size=64, width=None, sub_size=None, head_fill=YELLOW):
    """A store caption: arcade headline over a plain subline on a skewed dark plate with the yellow / blue rule."""
    sub_size = int(size * 0.5) if sub_size is None else sub_size
    head = arcade_text(headline, size, head_fill, INK, INK, True)
    sub = plain_text(subline, sub_size, WHITE, "Exo2") if subline else None
    pad_x, pad_y = int(size * 0.6), int(size * 0.3)
    inner_w = max(head.size[0], sub.size[0] if sub else 0)
    w = (width or inner_w + pad_x * 2)
    h = pad_y + head.size[1] + (int(size * 0.2) + sub.size[1] if sub else 0) + pad_y
    plate = skewed_plate(w, h)
    rule = stripes(w, max(3, size // 12))
    im = Image.new("RGBA", (plate.size[0], h + rule.size[1]), (0, 0, 0, 0))
    im.alpha_composite(plate, (0, 0))
    im.alpha_composite(rule, (int(h * 0.22) // 2, h))
    cx = plate.size[0] // 2
    im.alpha_composite(head, (cx - head.size[0] // 2, pad_y))
    if sub:
        im.alpha_composite(sub, (cx - sub.size[0] // 2, pad_y + head.size[1] + int(size * 0.2)))
    return drop_shadow(im, radius=14, offset=(0, 10), alpha=150)
