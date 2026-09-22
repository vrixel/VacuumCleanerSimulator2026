#!/usr/bin/env python3
"""Store screenshots with captions, the model gallery and the Play feature graphic (2026-09-22, his "add frames,
graphic effects, a gallery of hoover models").

Inputs are the raw smoke captures (tools/smoke-test.ps1, copied by hand or by a capture chain into
Builds/store-raw/<set>/smoke-*.png) and the studio renders of tools/gallery.ps1 (Builds/gallery/after-*.png and
import-*.png). Every caption is drawn with tools/brand.py, so the pictures speak like the HUD.

    python tools/store_shots.py              # everything that has inputs
    python tools/store_shots.py --only gallery,pc,phone,iphone,ipad,feature,action

Sets and sizes (a player window cannot exceed the display, so the sets are captured at a fraction with -Super):
    pc      960x540  -Super 2 -> 1920x1080   Microsoft Store screenshots      -> marketing/store/screens/NN-name.png
    phone   960x432  -Super 2 -> 1920x864    Google Play (letterboxed to 1920x1080, Play refuses 20:9) -> marketing/play
    iphone  717x330  -Super 4 -> 2868x1320   App Store iPhone 6.9" and, resized, the 6.5" slot 2688x1242 -> marketing/appstore
    ipad    688x516  -Super 4 -> 2752x2064   App Store iPad 13"                -> marketing/appstore
The gallery (1920x1080, all nineteen machines with their name tabs) goes to marketing/store/gallery_1920x1080.png
plus one copy per store folder at that store's size. The feature graphic (1024x500) is the wide hero with the wordmark on its empty left.
The action set (marketing/store/action/NN-name.png, 1920x1080) captions the eight generated action pictures of
marketing_real.py --style action for the Microsoft and Play galleries; never for the App Store (pictures of the app in use only).

Look at every output: the tool proves the files exist, not that a caption sits clear of the HUD.
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import brand  # noqa: E402

ROOT = brand.ROOT
RAW = os.path.join(ROOT, "Builds", "store-raw")
GALLERY = os.path.join(ROOT, "Builds", "gallery")
STORE = os.path.join(ROOT, "marketing", "store")
PLAY = os.path.join(ROOT, "marketing", "play")
APPSTORE = os.path.join(ROOT, "marketing", "appstore")

# The catalogue order of the garage: id, render file, name.
MACHINES = [
    ("dusty", "after-dusty.png", "Dusty"),
    ("roomboo", "after-roomboo.png", "Roomboo S9"),
    ("cyclonic", "after-cyclonic.png", "Cyclonic V-Storm"),
    ("harold", "after-harold.png", "Harold"),
    ("stick", "after-stick.png", "Stickmaster Cordless"),
    ("grandma", "after-grandma.png", "Grandma's Upright 1978"),
    ("rowinta", "after-rowinta.png", "Rowinta Silence Farce"),
    ("shopdrum", "after-shopdrum.png", "Shop Drum 3000"),
    ("m_redcanister", "import-henry.png", "Hubert the Grin"),
    ("m_cyclone", "import-dyson_upright.png", "Baron Vortex"),
    ("m_aquastick", "import-philips_aquatrio.png", "Sir Mops-a-Lot"),
    ("m_yellowdrum", "import-vacuum_4k.png", "Big Bertha"),
    ("m_greystick", "import-sixth_hm.png", "Twiglet"),
    ("m_wand", "import-vacuum_20k.png", "Wanda"),
    ("m_redsled", "import-vacuum_82k.png", "Monsieur Traineau"),
    ("m_bluedrum", "import-canister_a.png", "Bluebarrel"),
    ("m_greyrobot", "import-robvac.png", "Bumper"),
    ("m_roundone", "import-roomba_888.png", "Puck"),
    ("m_littlered", "import-henry_lowpoly.png", "Hubert Junior"),
]

# Store order: (slug, raw file, headline, subline). "gallery" is composed, not captured.
SHOTS = [
    ("game", "smoke-game.png", "YOU ARE THE VACUUM", "A filthy house. Eat the crumbs, then the chairs."),
    ("gallery", None, None, None),
    ("cat", "smoke-cat.png", "CHASE THE CAT", "It is faster than you. Mostly."),
    ("turbo", "smoke-turbo.png", "HOLD THE BOOST", "Sparks, speed lines and a trail of dust"),
    ("cord", "smoke-taut.png", "A CORD THAT FIGHTS BACK", "Pull too far and the plug pops out of the wall"),
    ("bin", "smoke-bin-far.png", "BAG FULL? FIND THE BIN", "Walls turn to glass, a marker shows the way"),
    ("powder", "smoke-powder.png", "LEAVE YOUR MARK", "Cocoa powder shows every path you clean"),
    ("tutorial", "smoke-tutorial.png", "LEARN IT IN SIX STEPS", "An interactive walkthrough on your first run"),
    ("garage", "smoke-model-m_redsled.png", "PICK YOUR MACHINE", "Nineteen vacuums, each one drives differently"),
    ("rewind", "smoke-rewind.png", "REWIND THE CORD", "The plug whips across the floor and takes the mess with it"),
]


def machine_box(im):
    """Bounding box of the machine on a studio render: where the picture departs from its own blurred copy
    (the cyclorama and the soft shadow are smooth, the machine is not), plus saturated pixels."""
    from PIL import ImageChops, ImageFilter
    w, h = im.size
    blur = im.filter(ImageFilter.GaussianBlur(14))
    diff = ImageChops.difference(im, blur).convert("L").point(lambda v: 255 if v > 16 else 0)
    sat = im.convert("HSV").split()[1].point(lambda v: 255 if v > 46 else 0)
    mask = ImageChops.lighter(diff, sat)
    ImageDraw.Draw(mask).rectangle((0, 0, w, int(h * 0.06)), fill=0)
    small = mask.resize((w // 8, h // 8), Image.BOX).point(lambda p: 255 if p > 110 else 0)
    box = small.getbbox()
    if box is None or (box[2] - box[0]) * (box[3] - box[1]) < (w // 8) * (h // 8) * 0.02:
        return (int(w * 0.2), int(h * 0.2), int(w * 0.8), int(h * 0.8))
    l, t, r, b = [c * 8 for c in box]
    return (max(0, l - 8), max(0, t - 8), min(w, r + 16), min(h, b + 16))


def tile_from_render(path, tw, th):
    """Crop the render around the machine at the tile's aspect ratio, so the studio grey stays continuous."""
    im = Image.open(path).convert("RGB")
    w, h = im.size
    l, t, r, b = machine_box(im)
    bw, bh = r - l, b - t
    # fit the box into the tile with a margin, keeping the tile aspect
    margin = 1.18
    cw, ch = bw * margin, bh * margin
    if cw / ch < tw / th:
        cw = ch * tw / th
    else:
        ch = cw * th / tw
    cw, ch = min(cw, w), min(ch, h)
    cx, cy = (l + r) / 2, (t + b) / 2
    x0 = min(max(0, cx - cw / 2), w - cw)
    y0 = min(max(0, cy - ch / 2), h - ch)
    return im.crop((int(x0), int(y0), int(x0 + cw), int(y0 + ch))).resize((tw, th), Image.LANCZOS)


def gallery(width=1920, height=1080, cols=5, rows=4):
    """All nineteen machines with their name tabs, the wordmark in the twentieth cell."""
    gap = 18
    tw = (width - gap * (cols + 1)) // cols
    th = (height - gap * (rows + 1)) // rows
    tab_h = int(th * 0.19)
    im = Image.new("RGBA", (width, height), brand.PANEL + (255,))
    d = ImageDraw.Draw(im)
    # a faint diagonal hatch on the panel, never a flat rectangle (his 2026-09-06 rule)
    for x in range(-height, width, 28):
        d.line([(x, height), (x + height, 0)], fill=(28, 32, 42, 255), width=6)
    cells = [(i % cols, i // cols) for i in range(cols * rows)]
    for (mid, fname, name), (cx, cy) in zip(MACHINES, cells):
        x = gap + cx * (tw + gap)
        y = gap + cy * (th + gap)
        p = os.path.join(GALLERY, fname)
        if os.path.exists(p):
            tile = tile_from_render(p, tw, th - tab_h)
            im.paste(tile, (x, y))
        else:
            d.rectangle((x, y, x + tw, y + th - tab_h), fill=(60, 60, 70, 255))
        d.rectangle((x, y + th - tab_h, x + tw - 1, y + th - 1), fill=brand.INK + (255,))
        size = int(tab_h * 0.5)
        lab = brand.tab(name, size, brand.YELLOW, skew=True)
        while lab.size[0] > tw - 16 and size > 12:
            size -= 2
            lab = brand.tab(name, size, brand.YELLOW, skew=True)
        im.alpha_composite(lab, (x + 8, y + th - tab_h + (tab_h - lab.size[1]) // 2))
    # the last cell: the wordmark and the count
    cx, cy = cells[len(MACHINES)]
    x = gap + cx * (tw + gap)
    y = gap + cy * (th + gap)
    d.rectangle((x, y, x + tw - 1, y + th - 1), fill=brand.INK + (255,))
    wm = brand.wordmark(tw - 40)
    im.alpha_composite(wm, (x + 20, y + 18))
    count = brand.arcade_text("19 MACHINES", int(th * 0.2), brand.WHITE, depth=5, edge_w=4)
    im.alpha_composite(count, (x + (tw - count.size[0]) // 2, y + th - count.size[1] - 18))
    return im.convert("RGB")


def overlay_caption(im, headline, subline, size, bottom_frac, max_w_frac=0.7, centre_frac=0.5):
    """Caption plate centred on centre_frac of the width, its bottom edge at bottom_frac of the height.
    Touch sets centre it at 0.40, in the clear zone between the stick (left) and the button diamond (right)."""
    w, h = im.size
    cap = brand.caption(headline, subline, size)
    while cap.size[0] > w * max_w_frac and size > 24:
        size -= 4
        cap = brand.caption(headline, subline, size)
    out = im.convert("RGBA")
    out.alpha_composite(cap, (int(w * centre_frac - cap.size[0] / 2), int(h * bottom_frac) - cap.size[1] + 40))   # 40: the shadow pad
    return out.convert("RGB")


def letterbox(im, width, height, headline, subline):
    """The 20:9 phone capture on top, a caption band below (Play wants 16:9)."""
    out = Image.new("RGBA", (width, height), brand.PANEL + (255,))
    pic = im.convert("RGB").resize((width, int(im.size[1] * width / im.size[0])), Image.LANCZOS)
    out.paste(pic, (0, 0))
    band_y = pic.size[1]
    d = ImageDraw.Draw(out)
    for x in range(-height, width, 28):
        d.line([(x, height), (x + height, band_y)], fill=(28, 32, 42, 255), width=6)
    rule = brand.stripes(width, 6)
    out.alpha_composite(rule, (0, band_y))
    band_h = height - band_y
    size = int(band_h * 0.34)
    head = brand.arcade_text(headline, size, brand.YELLOW)
    sub = brand.plain_text(subline, int(size * 0.55), brand.WHITE, "Exo2")
    while max(head.size[0], sub.size[0]) > width * 0.9 and size > 20:
        size -= 4
        head = brand.arcade_text(headline, size, brand.YELLOW)
        sub = brand.plain_text(subline, int(size * 0.55), brand.WHITE, "Exo2")
    total = head.size[1] + 10 + sub.size[1]
    y = band_y + 12 + (band_h - 12 - total) // 2
    out.alpha_composite(head, ((width - head.size[0]) // 2, y))
    out.alpha_composite(sub, ((width - sub.size[0]) // 2, y + head.size[1] + 10))
    return out.convert("RGB")


def save(im, folder, name):
    os.makedirs(folder, exist_ok=True)
    p = os.path.join(folder, name)
    im.save(p, "PNG", optimize=True)
    print(f"{os.path.relpath(p, ROOT)}  {im.size[0]}x{im.size[1]}")


def raw(setname, fname):
    p = os.path.join(RAW, setname, fname)
    if not os.path.exists(p):
        print("  missing", os.path.relpath(p, ROOT))
        return None
    return Image.open(p).convert("RGB")


def run_set(setname, gal, out_dir, prefix, size, style, expected, keep=None, extra_sizes=()):
    """One store set. style: 'overlay' (plate over the picture) or 'letterbox' (caption band below)."""
    n = 0
    for slug, fname, head, sub in SHOTS:
        if keep is not None and slug not in keep:
            continue
        n += 1
        if slug == "gallery":
            im = gal.resize(size, Image.LANCZOS)
        else:
            src = raw(setname, fname)
            if src is None:
                continue
            if src.size != expected:
                print(f"  {fname}: {src.size[0]}x{src.size[1]}, expected {expected[0]}x{expected[1]} (recapture)")
            if style == "letterbox":
                im = letterbox(src, size[0], size[1], head, sub)
            else:
                pic = src.resize(size, Image.LANCZOS) if src.size != size else src
                im = overlay_caption(pic, head, sub, int(size[1] * 0.06), style_bottom(setname),
                                     max_w_frac=CAPTION_ZONE[setname][0], centre_frac=CAPTION_ZONE[setname][1])
        save(im, out_dir, f"{prefix}{n:02d}-{slug}.png")
        for tag, sz in extra_sizes:
            save(im.resize(sz, Image.LANCZOS), out_dir, f"{tag}{n:02d}-{slug}.png")


# The caption's zone per set: (max width as a fraction, centre as a fraction). On the touch layouts it must sit
# between the stick (left) and the REWIND button (right, at 0.63 of the width on a phone, 0.66 on the iPad).
CAPTION_ZONE = {"pc": (0.7, 0.5), "phone": (0.5, 0.40), "iphone": (0.44, 0.37), "ipad": (0.42, 0.36)}


# The generated action pictures (2026-09-22 night, his "AI gen images to show better images of the actions ... like a
# hoover swallowing a toilet"): marketing/source/action/<name>.png from marketing_real.py --style action, captioned
# here like the captures. For the Microsoft Store gallery, the Play listing and the site; Apple wants pictures of
# the app in use (guideline 2.3.3), so they never go in the App Store slots.
ACTION_SHOTS = [
    ("toilet", "EATS EVERYTHING. YES, THE TOILET.", "Level up until the whole house fits in the nozzle"),
    ("couch", "THEN THE COUCH", "Chairs, sofas, the fridge: nothing is too big"),
    ("cat", "CHASE THE CAT", "It is faster than you. Mostly."),
    ("turbo", "HOLD THE BOOST", "Sparks, speed lines and a trail of dust"),
    ("cord", "A CORD THAT FIGHTS BACK", "Pull too far and the plug pops out of the wall"),
    ("blowout", "BAG FULL? BLOW IT ALL OUT", "Empty it into the bin, or fire it back across the room"),
    ("powder", "LEAVE YOUR MARK", "Cocoa powder shows every path you clean"),
    ("garage", "PICK YOUR MACHINE", "Nineteen vacuums, each one drives differently"),
]


def cover(im, width, height, anchor=0.5):
    """Scale to cover width x height and crop, anchor = where the vertical crop keeps its centre (0 top, 1 bottom)."""
    sw, sh = im.size
    scale = max(width / sw, height / sh)
    im = im.resize((int(round(sw * scale)), int(round(sh * scale))), Image.LANCZOS)
    x = (im.size[0] - width) // 2
    y = int((im.size[1] - height) * anchor)
    return im.crop((x, y, x + width, y + height))


def run_action(out_dir, size=(1920, 1080)):
    src_dir = os.path.join(ROOT, "marketing", "source", "action")
    for i, (name, head, sub) in enumerate(ACTION_SHOTS, 1):
        src = os.path.join(src_dir, name + ".png")
        if not os.path.exists(src):
            print("  missing", os.path.relpath(src, ROOT))
            continue
        im = cover(Image.open(src).convert("RGB"), *size)
        im = overlay_caption(im, head, sub, 60, 0.94, max_w_frac=0.8)
        save(im, out_dir, f"{i:02d}-{name}.png")


# The fused pictures (2026-09-22, his "hope you will mix and match action images with actual screenshots ... i miss
# the garage view" and "pas un ordre mix une sorte de fusion entre generated art and screenshots"): the action art
# fills the frame, the real capture stands on it as a tilted photo plate on the side the machine's body leaves free,
# and the machine (cut out by marketing_real.py --cutouts) comes back on top so the gag bursts over the gameplay.
# (action, capture in Builds/store-raw/pc, or None for the nineteen-machine gallery, plate width, plate top)
FUSION_PAIRS = [
    ("toilet", "smoke-bin.png", 0.50, 0.06),
    ("garage", None, 0.50, 0.06),
    ("cat", "smoke-cat.png", 0.50, 0.06),
    ("turbo", "smoke-turbo.png", 0.50, 0.06),
    ("cord", "smoke-rewind.png", 0.50, 0.06),
    ("blowout", "smoke-bin.png", 0.42, 0.30),   # the debris fountain fills the top right
    ("powder", "smoke-powder.png", 0.50, 0.06),
    ("couch", "smoke-game.png", 0.50, 0.06),
]


def photo_plate(shot, width, angle, edge=14):
    """The capture as a tilted photo plate: white edge, ink outline, soft shadow."""
    pic = shot.convert("RGB").resize((width, int(shot.size[1] * width / shot.size[0])), Image.LANCZOS)
    pw, ph = pic.size[0] + 2 * edge, pic.size[1] + 2 * edge
    card = Image.new("RGBA", (pw, ph), (255, 255, 255, 255))
    ImageDraw.Draw(card).rectangle((0, 0, pw - 1, ph - 1), outline=brand.INK + (255,), width=4)
    card.paste(pic, (edge, edge))
    card = card.rotate(angle, Image.BICUBIC, expand=True)
    return brand.drop_shadow(card, 22, (0, 18), 190)


def body_centre(cut):
    """Horizontal centre of the red shell in a cutout (the body, not the hose, the cord or the gag object)."""
    small = cut.resize((max(1, cut.size[0] // 8), max(1, cut.size[1] // 8)))
    px = small.load()
    xs = []
    for y in range(small.size[1]):
        for x in range(small.size[0]):
            r, g, b, a = px[x, y]
            if a > 200 and r > 140 and r > 2 * g and r > 2 * b:
                xs.append(x)
    return (sum(xs) / len(xs)) * 8 if xs else cut.size[0] / 2


def fuse(art, cut, shot, headline, subline, width, top, tilt=3.5, size=(1920, 1080)):
    W, H = size
    base = cover(art.convert("RGBA"), W, H)
    cut = cover(cut.convert("RGBA"), W, H)
    mx = body_centre(cut)
    right = mx < W / 2                       # the plate goes to the side the body leaves free
    pl = photo_plate(shot, int(W * width), -tilt if right else tilt)
    if right:
        x = min(int(mx + W * 0.10), W + int(pl.size[0] * 0.04) - pl.size[0])
    else:
        x = max(int(mx - W * 0.10 - pl.size[0]), -int(pl.size[0] * 0.04))
    out = base.copy()
    out.alpha_composite(pl, (x, int(H * top)))
    out.alpha_composite(cut)
    return overlay_caption(out, headline, subline, 60, 0.955, max_w_frac=0.82).convert("RGB")


def run_fusion(gal, out_dir, size=(1920, 1080)):
    src_dir = os.path.join(ROOT, "marketing", "source", "action")
    captions = {n: (h, s) for n, h, s in ACTION_SHOTS}
    done = []
    for i, (name, shotfile, width, top) in enumerate(FUSION_PAIRS, 1):
        art, cut = os.path.join(src_dir, name + ".png"), os.path.join(src_dir, "cut", name + ".png")
        if not (os.path.exists(art) and os.path.exists(cut)):
            print("  missing", os.path.relpath(cut if os.path.exists(art) else art, ROOT))
            continue
        shot = gal if shotfile is None else raw("pc", shotfile)
        if shot is None:
            continue
        im = fuse(Image.open(art), Image.open(cut), shot, *captions[name], width, top, size=size)
        save(im, out_dir, f"{i:02d}-{name}.png")
        done.append(im)
    if done:
        cols, tw, th, pad = 2, 960, 540, 16
        sheet = Image.new("RGB", (cols * (tw + pad) + pad, ((len(done) + cols - 1) // cols) * (th + pad) + pad), (36, 40, 48))
        for i, im in enumerate(done):
            sheet.paste(im.resize((tw, th), Image.LANCZOS), (pad + (i % cols) * (tw + pad), pad + (i // cols) * (th + pad)))
        save(sheet, STORE, "fusion_sheet.png")


def style_bottom(setname):
    # PC: above the 250 px cockpit (830 of 1080). Touch layers: between the stick and the button cluster.
    return 0.75 if setname == "pc" else 0.965


def feature_graphic():
    """Play's 1024x500: the wide navy hero (2026-09-22, the icon's studio) with the wordmark in the empty left third."""
    src = os.path.join(ROOT, "marketing", "source", "hero_wide.png")
    if not os.path.exists(src):
        print("  missing", os.path.relpath(src, ROOT))
        return None
    im = Image.open(src).convert("RGB")
    sw, sh = im.size
    scale = max(1024 / sw, 500 / sh)
    im = im.resize((int(round(sw * scale)), int(round(sh * scale))), Image.LANCZOS)
    x = (im.size[0] - 1024) // 2
    y = int((im.size[1] - 500) * 0.55)   # keep the floor head and the reflection, lose a little sky
    im = im.crop((x, y, x + 1024, y + 500)).convert("RGBA")
    wm = brand.wordmark(350)   # 430 touched the wand
    wm = brand.drop_shadow(wm, 16, (0, 10), 190)
    im.alpha_composite(wm, (48 - 48, (500 - wm.size[1]) // 2 - 10))
    return im.convert("RGB")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    want = lambda k: not only or k in only  # noqa: E731
    gal = gallery()
    if want("gallery"):
        save(gal, STORE, "gallery_1920x1080.png")
    if want("pc"):
        run_set("pc", gal, os.path.join(STORE, "screens"), "", (1920, 1080), "overlay", (1920, 1080))
    if want("phone"):
        run_set("phone", gal, PLAY, "phone-", (1920, 1080), "letterbox", (1920, 864),
                keep={"game", "gallery", "cat", "turbo", "cord", "bin", "powder", "tutorial"})
    if want("iphone"):
        run_set("iphone", gal, APPSTORE, "iphone69-", (2868, 1320), "overlay", (2868, 1320),
                keep={"game", "gallery", "cat", "turbo", "cord", "bin", "powder", "tutorial"},
                extra_sizes=[("iphone65-", (2688, 1242))])
    if want("ipad"):
        run_set("ipad", gal, APPSTORE, "ipad13-", (2752, 2064), "overlay", (2752, 2064),
                keep={"game", "gallery", "cat", "turbo", "cord", "bin", "powder", "tutorial"})
    if want("action"):
        run_action(os.path.join(STORE, "action"))
    if want("fusion"):
        run_fusion(gal, os.path.join(STORE, "fusion"))
    if want("feature"):
        fg = feature_graphic()
        if fg is not None:
            save(fg, PLAY, "feature_1024x500.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
