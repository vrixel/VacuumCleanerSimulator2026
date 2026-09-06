#!/usr/bin/env python3
"""Generates the cockpit art, music and sound effects with kie.ai.

Idempotent campaign in the style of the kie-ai skill: a finished file is skipped, nothing is
overwritten without --force, the balance is printed before and after, results are downloaded as
soon as a poll says success. Raw downloads land in tools/assets/raw (gitignored); the processed
files go to Assets/Resources where Unity picks them up.

    python tools/assets/kie_assets.py --list
    python tools/assets/kie_assets.py --dry-run
    python tools/assets/kie_assets.py [--only id,id] [--force] [--images-only | --audio-only]
"""
import argparse
import concurrent.futures
import json
import os
import subprocess
import sys
import time
import urllib.request

SKILL = os.path.join(os.path.expanduser("~"), ".claude", "skills", "kie-ai", "scripts")
sys.path.insert(0, SKILL)
import kie as kiemod  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.path.join(ROOT, "tools", "assets", "raw")
RES = os.path.join(ROOT, "Assets", "Resources")
FFMPEG = "ffmpeg"

IMAGE_MODEL = "bytedance/seedream-v4-text-to-image"
# nano-banana-edit applies a delta and leaves the object alone; gpt-image-2-image-to-image redrew every
# container as the same bagless bin (measured 2026-09-05, 4 of 4 wrong).
EDIT_MODEL = "google/nano-banana-edit"
MUSIC_MODEL = "V4_5"
SOUND_MODEL = "V5"

NO_TEXT = ("ABSOLUTELY NO TEXT of any kind: no numbers, no letters, no labels, no logos, no watermark. "
           "Pure black background. Front view, perfectly centred, the object fills the frame.")

GAUGE_BASE = ("A round suction gauge face from a vacuum cleaner instrument panel, photorealistic industrial "
              "instrument, dial with fine tick marks only and a red danger zone at the end of the scale, "
              "NO needle. {style}. Product-photography lighting, subtle reflections. " + NO_TEXT)

GAUGES = {
    "dusty": "Cheap black plastic bezel, plain white paper dial, hand-cut cardboard prototype feel, a strip of masking tape on the bezel",
    "roomboo": "A ROUND gauge (not a phone, no device, no screen mockup): sleek modern dark smoked-glass face inside a thin matte black round bezel, thin teal LED ring segments around the edge, minimalist, soft glow",
    "cyclonic": "Futuristic purple and brushed-silver bezel, concentric LED ring, a small engraved cyclone motif in the centre",
    "harold": "Vintage 1960s bakelite VU-meter, cream dial with warm amber backlight, red and black, slightly worn",
    "stick": "Minimal OLED instrument set into brushed aluminium, one thin blue arc, ultra-modern and flat",
    "grandma": "1970s chrome bezel, avocado green dial face, small amber lamp, worn mechanical instrument with a scratch",
    "rowinta": "Elegant navy blue dial face with thin white and red accents, refined French design, brushed steel bezel",
    "shopdrum": "Rugged industrial gauge, yellow and black, thick rubber bezel, big red LED segments, scratched and dusty",
}

CONTAINERS = {
    "bag": "A vacuum cleaner paper dust bag shown as a cutaway cross-section so the inside is visible, EMPTY, beige paper, cardboard collar at the top, photorealistic",
    "cyclone": "A transparent cyclonic dust bin from a bagless vacuum, clear cylinder with a purple cyclone cone inside, EMPTY, photorealistic",
    "tray": "A small robot-vacuum debris tray with a transparent lid, seen from the front, EMPTY, dark grey plastic, photorealistic",
    "drum": "A wet-and-dry vacuum steel drum tank shown as a cutaway cross-section so the inside is visible, EMPTY, yellow and black, photorealistic",
}

FULL_EDIT = ("Fill the inside of this exact container to the brim with grey household dust, crumbs, hair and small colourful "
             "debris. This is the same object: change nothing else, keep its shape, colours, opening, camera position and "
             "framing identical edge to edge, do not crop, do not zoom, do not recentre, do not replace it with another "
             "container. Keep the pure black background. ABSOLUTELY NO TEXT of any kind.")

PANEL = ("Seamless texture of a dark brushed aluminium instrument panel with a subtle carbon-fibre weave, a few small "
         "hex screws, matte, photorealistic, evenly lit, very dark overall. No objects, no gauges, no text, no logos.")

# HUD plates: opaque frames on black. The interior of each frame is a flat black screen so dynamic text and
# instruments can be drawn over it; the outside is keyed to transparent. Bold arcade cabinet meets flight deck.
HUD_STYLE = ("Bold 1990s arcade racing cabinet meets modern flight-simulator cockpit. Chunky bevelled dark charcoal "
             "metal frame with brushed steel edges and hex screws, sharp diagonal cuts, a thin bright safety-yellow "
             "accent line and a thin electric-blue accent line, high contrast, clean and crisp, flat front view, no "
             "perspective, evenly lit. The inside of the frame is a perfectly flat matte black screen, completely "
             "empty. Nothing lit, no glow, no reflections on the screen. ")
HUD_PLATES = {
    "frame_square": "A square instrument screen frame for a game HUD. " + HUD_STYLE + NO_TEXT,
    "frame_wide": "A wide horizontal instrument screen frame for a game HUD, twice as wide as it is tall. " + HUD_STYLE + NO_TEXT,
    "frame_tall": "A tall vertical instrument screen frame for a game HUD, twice as tall as it is wide. " + HUD_STYLE + NO_TEXT,
    "plate_score": "A wide arcade scoreboard plate for a game HUD, three times as wide as it is tall, bold chunky frame with a "
                   "yellow and black hazard-stripe accent along the bottom edge. " + HUD_STYLE + NO_TEXT,
    "plate_banner": "A very wide announcement banner plate for a game HUD, five times as wide as it is tall, bold arcade style "
                    "with diagonal chevron cuts at both ends and yellow and blue accent stripes. " + HUD_STYLE + NO_TEXT,
    "dash_strip": "A very wide instrument dashboard strip for a game HUD, six times as wide as it is tall, dark brushed "
                  "metal with carbon-fibre inlays, a bevelled top edge, hex screws, and several empty flat black "
                  "rectangular screen areas separated by steel ribs. " + HUD_STYLE + NO_TEXT,
}

# HUD elements (2026-09-06, "plus de rectangles a couleur unie"): every flat-coloured rectangle of the HUD gets a
# generated plate. Neutral WHITE enamel where the game tints (tabs, buttons, readouts, bar fills, lit tiles) so one
# sprite serves every colour; dark where it is always dark. Nine-sliced in UIStyle, procedural fallbacks stay.
# never say "arcade cabinet" here: seedream then draws a whole cabinet instead of the part (measured 2026-09-06, 3 of 8)
ELEMENT_STYLE = ("A single isolated industrial control-panel part, nothing else in the picture: chunky bevelled "
                 "brushed-steel edges, tiny hex screws, sharp diagonal chamfers, crisp, exactly front view, flat, no "
                 "perspective, evenly lit, no glow, no reflections of a room. ")
# dark parts cannot be keyed off a black background: they sit on flat green instead
NO_TEXT_GREEN = NO_TEXT.replace("Pure black background", "Flat pure green background (#00FF00)")
HUD_ELEMENTS = {
    "tab_plate": ("A wide horizontal blank name plate, landscape orientation, much wider than tall: a PURE WHITE glossy "
                  "enamel face inside a thin dark bevelled steel edge with diagonal chamfered corners and a tiny hex "
                  "screw at each end, completely empty face. " + ELEMENT_STYLE + NO_TEXT),
    "button_square": ("A square arcade cabinet push-button plate for a game HUD, PURE WHITE glossy enamel face with a thin "
                      "dark bevelled steel rim and chamfered corners, completely empty face. " + ELEMENT_STYLE + NO_TEXT),
    "tile_off": ("A wide horizontal annunciator lamp tile from an aircraft cockpit warning panel, landscape orientation, "
                 "twice as wide as tall, UNLIT: dark charcoal smoked-glass face inside a thin bevelled steel frame with "
                 "tiny screws, completely empty face. " + ELEMENT_STYLE + NO_TEXT_GREEN),
    "bar_track": ("A long horizontal recessed slot, landscape orientation, very wide and thin: a dark charcoal metal "
                  "groove with a bevelled brushed-steel lip all around, empty, seen exactly from the front. "
                  + ELEMENT_STYLE + NO_TEXT_GREEN),
    "bar_fill": ("A long horizontal glossy PURE WHITE light bar, eight times as wide as it is tall, a lit frosted acrylic "
                 "strip with a bright highlight along the top and softly rounded ends, flat front view, isolated. "
                 + ELEMENT_STYLE + NO_TEXT),
    "screen_glass": ("A rectangular dark instrument screen for a game HUD, three units wide by two tall: nearly black "
                     "smoked glass with a faint vignette and very subtle scanlines, inside a thin bevelled brushed-steel "
                     "frame with four tiny hex screws in the corners, completely empty. " + ELEMENT_STYLE + NO_TEXT),
    "readout_box": ("A wide horizontal pointer plate shaped like a thick arrow pointing RIGHT: a PURE WHITE glossy enamel "
                    "rectangle with a thin dark bevel whose right end is a pointed chevron tip, landscape orientation, "
                    "completely empty face. " + ELEMENT_STYLE + NO_TEXT),
    # banners (2026-09-06, "ce gros rectangle noir est laid"): a comic bang splash and a ray wheel behind bonus text
    "banner_burst": ("A wide comic-book style BANG splash burst shape, landscape, three times as wide as tall: jagged "
                     "starburst explosion silhouette filled with a bright safety-yellow to orange gradient, a thick "
                     "electric-blue outline and a thin black outer line, a few small white speed sparks around it, "
                     "bold flat arcade game bonus splash, completely empty inside, isolated on a pure black background. "
                     "ABSOLUTELY NO TEXT of any kind: no letters, no numbers, no logos."),
    "banner_rays": ("A round sunburst wheel of radiating rays, alternating bright yellow and pale white rays fading out "
                    "toward the edge, soft and glowing, centred, circular, bold arcade game victory background, empty "
                    "centre, isolated on a pure black background. ABSOLUTELY NO TEXT of any kind: no letters, no "
                    "numbers, no logos."),
    "speed_lines": ("A full-frame overlay of radial motion speed lines: thin bright white streaks radiating from the "
                    "centre outward to the edges like a comic-book zoom, dense at the edges, the centre and the middle "
                    "of the frame completely empty, high energy arcade racing effect, isolated on a pure black "
                    "background. ABSOLUTELY NO TEXT of any kind: no letters, no numbers, no logos."),
    "radar_bezel": ("A round radar scope bezel for a game HUD: a thick circular bevelled brushed-steel ring with hex "
                    "screws and a thin safety-yellow accent groove, the centre of the ring is EMPTY flat pure black, "
                    "seen exactly from the front, isolated. " + ELEMENT_STYLE + NO_TEXT),
}
# the lit tile is an edit of the unlit one so the two match pixel for pixel (nano-banana-edit keeps the geometry)
TILE_ON_EDIT = ("Light this exact annunciator tile up: its smoked glass face now glows an even bright PURE WHITE, evenly "
                "lit from behind, edge to edge. Change nothing else: same frame, same screws, same size, same position, "
                "same flat pure green background, do not crop, do not zoom. ABSOLUTELY NO TEXT of any kind.")

# Marketing art and the app icon. Landscape or portrait as the subject suggests (the models decide, we measure).
MARKETING_STYLE = ("Bold saturated colours, thick clean outlines, dynamic composition, exaggerated cartoon physics, "
                   "glossy modern 3D-cartoon render, arcade game poster style, family friendly, no gore. "
                   "ABSOLUTELY NO TEXT of any kind: no title, no letters, no logos, no watermark.")
MARKETING = {
    "key_art": ("Key art for a comedy video game: an upright vacuum cleaner with huge googly eyes and a mischievous grin "
                "tears through a messy suburban living room at full speed, crumbs, socks, toy bricks and coins flying "
                "into its nozzle, a couch tipping over, a lamp airborne, a cat on the curtain rail. Low dynamic camera "
                "angle, wide 16:9 landscape composition with empty space at the top for a title. " + MARKETING_STYLE),
    "hero_wide": ("Ultra-wide cinematic banner for a comedy video game: a cartoon vacuum cleaner with googly eyes "
                  "charging across a chaotic kitchen, cereal and crumbs swirling into it, chairs knocked over, a fridge "
                  "wobbling, bright morning light, very wide landscape composition, subject centred, room to crop at "
                  "both ends. " + MARKETING_STYLE),
    "library_portrait": ("Portrait poster for a comedy video game: a cartoon vacuum cleaner with googly eyes standing "
                         "proudly on a mountain of dust, socks and toys inside a living room, dramatic low angle, "
                         "tall 2:3 portrait composition with empty space at the top for a title. " + MARKETING_STYLE),
    "icon": ("App icon: the friendly face of a cartoon vacuum cleaner, huge googly eyes and a cheeky grin, front view, "
             "filling the frame, bold flat vector style with thick outlines, on a bright safety-yellow rounded-square "
             "background, perfectly centred, square 1:1. " + MARKETING_STYLE),
    "emblem": ("A vintage 1950s canister vacuum cleaner, cylindrical body on small wheels, ribbed hose curling up to a "
               "wand and floor nozzle, its electric cord coiled loosely on the ground beside it, and a single lost sock "
               "lying next to the nozzle. Drawn as a black ink burin line engraving with fine crosshatching, in the manner "
               "of a 19th century catalogue illustration, isolated object with only a small hatched ground shadow, "
               "centred on a pure white background, square. No frame, no border, no backdrop, no scene, no colour. "
               "ABSOLUTELY NO TEXT of any kind."),
    "garage_lineup": ("A line-up of eight cartoon vacuum cleaners with googly eyes posing together like a team photo: "
                      "a robot disc, a purple bagless upright on a ball, a red smiling canister, a cordless stick, a "
                      "1970s green upright with a cloth bag, a blue French canister with a beret and a baguette, a yellow "
                      "wet-and-dry drum, and a boxy orange prototype. Studio backdrop, wide landscape. " + MARKETING_STYLE),
}

MUSIC = {
    "title": "Quirky upbeat jazzy theme for a silly video game about a vacuum cleaner, instrumental, kazoo, tuba, ukulele and brushed drums, cheerful and mischievous, 95 bpm, loopable, no vocals",
    "game": "Energetic playful funk with chiptune touches for a comedy physics game, instrumental, bouncy bass, clavinet, hand claps, 120 bpm, loopable, no vocals",
}

SOUNDS = {
    "motor_loop": {"prompt": "A household vacuum cleaner motor running steadily, close microphone, constant hum with airflow, no music, clean loop", "loop": True, "trim": 6.0},
    "pop_real": {"prompt": "A vacuum cleaner nozzle sucking up a small object, one short thwop pop with a rattle inside the hose, no music", "loop": False, "trim": 1.2},
    "bag_alarm": {"prompt": "Small household appliance warning beep, three short electronic beeps, bag full alarm, no music", "loop": False, "trim": 2.0},
    # 2026-09-06, his feedback "les bruitages sont un peu legers": a suction layer over the motor, a reel loop for the
    # rewind, and three object-absorption hits by size class.
    "suction_loop": {"prompt": "Vacuum cleaner nozzle sucking hard on a carpet, powerful airflow roar and hiss with turbulence, close microphone, steady, no motor whine, no music, clean seamless loop", "loop": True, "trim": 5.0},
    "rewind_loop": {"prompt": "Vacuum cleaner power cord retracting fast into its spring reel, whirring spin, cable sliding and slapping on the floor, fast ratchet ticking, no music, clean seamless loop", "loop": True, "trim": 3.0},
    "rewind_end": {"prompt": "A vacuum cleaner cord reel stopping, the plug snapping hard into the plastic housing, one sharp clack with a short spring twang, no music", "loop": False, "trim": 1.0},
    "absorb_small": {"prompt": "A sock sucked into a vacuum cleaner hose, one fast whoosh thwip with a soft flap, no music", "loop": False, "trim": 0.9},
    "absorb_medium": {"prompt": "A wooden toy block sucked into a vacuum cleaner hose, rattling up the pipe then a hollow thud into the bin, no music", "loop": False, "trim": 1.4},
    "absorb_big": {"prompt": "A heavy object slamming into a big vacuum cleaner drum, one deep hollow bonk with a rattle and a suction gulp, no music", "loop": False, "trim": 1.5},
    # the boost (2026-09-06, "I want to feel the boost"): spool-up, a screaming loop, spool-down
    "turbo_up": {"prompt": "A vacuum cleaner motor surging from normal speed to a screaming high-rpm turbo whine in one second, rising pitch, air rush, punchy, no music", "loop": False, "trim": 1.2},
    "turbo_loop": {"prompt": "A vacuum cleaner motor screaming at maximum turbo rpm, high-pitched whine with roaring airflow, steady, intense, no music, clean seamless loop", "loop": True, "trim": 3.0},
    "turbo_down": {"prompt": "A screaming vacuum cleaner motor spooling down from turbo rpm to normal speed in one second, falling pitch with a soft airflow sigh, no music", "loop": False, "trim": 1.2},
}


def log(*a):
    print(*a, flush=True)


def ok_file(p, min_bytes=4096):
    return os.path.exists(p) and os.path.getsize(p) > min_bytes


def key_background(im, connected=True):
    """Makes the flat studio background transparent. The models ignore 'pure black background' now and
    then and hand back white or grey: the background colour is read from the corners, not assumed.
    connected=False also clears enclosed regions of the background colour (the empty centre of a ring)."""
    import numpy as np
    a = np.asarray(im.convert("RGB")).astype(np.float32)
    h, w, _ = a.shape
    m = max(4, min(h, w) // 40)
    corners = np.concatenate([a[:m, :m].reshape(-1, 3), a[:m, -m:].reshape(-1, 3),
                              a[-m:, :m].reshape(-1, 3), a[-m:, -m:].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    dist = np.sqrt(((a - bg) ** 2).sum(axis=2))
    # keep a soft edge: fully transparent within 22 units of the background colour, opaque beyond 70
    alpha = np.clip((dist - 22.0) / 48.0, 0.0, 1.0)
    # anything not connected to the border stays opaque (a black button on a black background)
    from PIL import Image
    mask = (alpha < 0.5).astype(np.uint8)
    border = np.zeros_like(mask)
    stack = [(0, 0), (0, w - 1), (h - 1, 0), (h - 1, w - 1)]
    # flood fill from the corners over the background-coloured region
    from collections import deque
    q = deque([p for p in stack if mask[p]])
    for p in q:
        border[p] = 1
    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not border[ny, nx]:
                border[ny, nx] = 1
                q.append((ny, nx))
    if connected:
        alpha = np.where(border == 1, alpha, np.maximum(alpha, 1.0))
    rgba = np.dstack([a, alpha * 255.0]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA"), bg


def _fit(im, size, square):
    from PIL import Image
    w, h = im.size
    if square:
        side = min(w, h)
        im = im.crop(((w - side) // 2, (h - side) // 2, (w - side) // 2 + side, (h - side) // 2 + side))
        return im.resize((size, size), Image.LANCZOS)
    # keep the aspect the model chose (it decides from the subject), bound the long side
    scale = size / max(w, h)
    return im.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.LANCZOS)


def largest_component(im):
    """Keeps only the biggest opaque blob: the models sometimes add a second small part beside the one asked."""
    import numpy as np
    from collections import deque
    from PIL import Image
    a = np.asarray(im)
    alpha = a[:, :, 3] > 8
    h, w = alpha.shape
    label = np.zeros((h, w), dtype=np.int32)
    best, best_n, n = 0, 0, 0
    for y in range(h):
        for x in range(w):
            if not alpha[y, x] or label[y, x]:
                continue
            n += 1
            q = deque([(y, x)]); label[y, x] = n; cnt = 0
            while q:
                cy, cx = q.popleft(); cnt += 1
                for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                    if 0 <= ny < h and 0 <= nx < w and alpha[ny, nx] and not label[ny, nx]:
                        label[ny, nx] = n; q.append((ny, nx))
            if cnt > best_n:
                best, best_n = n, cnt
    if n > 1:
        out = a.copy()
        out[:, :, 3] = np.where(label == best, out[:, :, 3], 0)
        log(f"   kept the largest of {n} parts")
        return Image.fromarray(out, "RGBA")
    return im


def process_image(src, dst, size, key=True, square=True, keyall=False, largest=False, mask_src=None, radial_fade=False, lum_alpha=False):
    from PIL import Image
    import numpy as np
    im = _fit(Image.open(src).convert("RGB"), size, square)
    if lum_alpha:
        # white marks on black (speed lines, glows): brightness becomes the alpha of a white sprite, so the game
        # tints it and the black never shows. The corner-colour key would pick the white and invert it.
        a = np.asarray(im).astype(np.float32)
        lum = a.max(axis=2)
        rgba = np.dstack([np.full_like(lum, 255.0), np.full_like(lum, 255.0), np.full_like(lum, 255.0), lum]).astype(np.uint8)
        im = Image.fromarray(rgba, "RGBA")
        log("   brightness taken as alpha")
        key = False
    if key:
        im, bg = key_background(im, connected=not keyall)
        log(f"   background keyed: rgb({int(bg[0])},{int(bg[1])},{int(bg[2])})" + (" everywhere" if keyall else ""))
        if mask_src:
            # an edit of another element keeps its geometry: borrow the base's silhouette (light spill on the
            # background is not the background colour and would survive the key)
            m, _ = key_background(_fit(Image.open(mask_src).convert("RGB"), size, square).resize(im.size, Image.LANCZOS), connected=not keyall)
            a = np.asarray(im).copy()
            a[:, :, 3] = np.minimum(a[:, :, 3], np.asarray(m)[:, :, 3])
            im = Image.fromarray(a, "RGBA")
            log("   silhouette taken from " + os.path.basename(mask_src))
        if radial_fade:
            # a glow that must melt into the screen: fade the alpha out from 60 percent of the radius to the edge
            a = np.asarray(im).copy()
            h2, w2 = a.shape[:2]
            yy, xx = np.mgrid[0:h2, 0:w2]
            r = np.sqrt(((xx - (w2 - 1) / 2) / (w2 / 2)) ** 2 + ((yy - (h2 - 1) / 2) / (h2 / 2)) ** 2)
            fade = np.clip((1.0 - r) / 0.4, 0.0, 1.0)
            a[:, :, 3] = (a[:, :, 3] * fade).astype(np.uint8)
            im = Image.fromarray(a, "RGBA")
            log("   radial fade applied")
        if largest:
            # hard-edged panel parts: kill the faint haze the key leaves around shadows (alpha below a quarter), then
            # keep one part
            a = np.asarray(im).copy()
            al = a[:, :, 3].astype(np.float32)
            a[:, :, 3] = np.clip((al - 64.0) * (255.0 / 191.0), 0, 255).astype(np.uint8)
            im = largest_component(Image.fromarray(a, "RGBA"))
        # the models answer square whatever the aspect asked: crop to the opaque object (plus a 2 px margin) so a
        # nine-sliced plate never stretches transparent margins
        bbox = im.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
        if bbox:
            x0, y0, x1, y1 = bbox
            im = im.crop((max(0, x0 - 2), max(0, y0 - 2), min(im.size[0], x1 + 2), min(im.size[1], y1 + 2)))
            log(f"   cropped to the object: {im.size[0]}x{im.size[1]}")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    im.save(dst, "PNG", optimize=True)
    log(f"   -> {os.path.relpath(dst, ROOT)} {im.size[0]}x{im.size[1]}")


def contact_sheet(items, dst, cell=256):
    from PIL import Image, ImageDraw
    files = [(id_, s["dst"]) for kind, id_, s in items if kind in ("image", "edit") and ok_file(s["dst"])]
    cols = 6
    rows = (len(files) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell, rows * (cell + 20)), (40, 40, 48))
    draw = ImageDraw.Draw(sheet)
    for i, (id_, f) in enumerate(files):
        im = Image.open(f).convert("RGBA").resize((cell, cell))
        bg = Image.new("RGBA", (cell, cell), (60, 60, 70, 255))
        bg.alpha_composite(im)
        x, y = (i % cols) * cell, (i // cols) * (cell + 20)
        sheet.paste(bg.convert("RGB"), (x, y))
        draw.text((x + 4, y + cell + 4), id_, fill=(230, 230, 230))
    sheet.save(dst)
    log(f"contact sheet -> {dst} ({len(files)} images)")


def run_ffmpeg(args):
    subprocess.run([FFMPEG, "-y", "-loglevel", "error"] + args, check=True)


def process_audio(src, dst, trim, loop):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    fade = 0.5 if loop else 0.05
    if trim:
        filt = f"afade=t=in:st=0:d={fade},afade=t=out:st={max(0.0, trim - fade)}:d={fade}"
        run_ffmpeg(["-i", src, "-t", str(trim), "-af", filt, dst])
    else:
        # music: fade half a second at both ends so the loop point does not click
        dur = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                             "-of", "default=nw=1:nk=1", src]).decode().strip())
        filt = f"afade=t=in:st=0:d=0.5,afade=t=out:st={max(0.0, dur - 0.5)}:d=0.5"
        run_ffmpeg(["-i", src, "-af", filt, dst])
    log(f"   -> {os.path.relpath(dst, ROOT)}")


class Campaign:
    def __init__(self, force=False, dry=False, reprocess=False):
        self.k = kiemod.Kie(verbose=True)
        self.force = force
        self.dry = dry
        self.reprocess = reprocess
        self.spent_log = []

    # ---------------------------------------------------------------- images
    def image(self, id_, prompt, dst, size, model=IMAGE_MODEL, base=None, key=True, square=True, keyall=False, largest=False, mask_src=None, radial_fade=False, lum_alpha=False):
        raw = os.path.join(RAW, id_ + ".png")
        if self.reprocess:
            if ok_file(raw):
                log(f"[{id_}] reprocessing from raw")
                process_image(raw, dst, size, key, square, keyall, largest, mask_src, radial_fade, lum_alpha)
            else:
                log(f"[{id_}] no raw file, cannot reprocess")
            return
        if ok_file(dst) and not self.force:
            log(f"[{id_}] already there, skipped")
            return
        if self.dry:
            log(f"[{id_}] would generate with {model} -> {os.path.relpath(dst, ROOT)}")
            return
        if not ok_file(raw) or self.force:
            log(f"[{id_}] generating with {model}")
            self.k.generate(prompt, raw, model=model, image=base, ratio="1:1", output_format="png", force=True)
        process_image(raw, dst, size, key, square, keyall, largest, mask_src, radial_fade, lum_alpha)

    # ----------------------------------------------------------------- audio
    def _audio_task(self, path, body):
        r = self.k._request(self.k.base + path, body)
        tid = (r.get("data") or {}).get("taskId")
        if not tid:
            raise kiemod.KieError("no taskId: " + json.dumps(r)[:300])
        return tid

    def _audio_poll(self, tid, tries=80, every=6):
        for _ in range(tries):
            time.sleep(every)
            try:
                d = self.k._get("/api/v1/generate/record-info", {"taskId": tid}).get("data") or {}
            except kiemod.KieError as e:
                log("   poll:", e)
                continue
            status = str(d.get("status", "")).upper()
            if status == "SUCCESS":
                urls = []
                self._collect_audio_urls(d, urls)
                if not urls:
                    raise kiemod.KieError("success without audio url: " + json.dumps(d)[:300])
                return urls
            if "FAIL" in status or "ERROR" in status:
                raise kiemod.KieError("audio task failed: " + json.dumps(d)[:300])
        raise kiemod.KieError("audio poll timeout")

    def _collect_audio_urls(self, node, out):
        if isinstance(node, dict):
            for v in node.values():
                self._collect_audio_urls(v, out)
        elif isinstance(node, list):
            for v in node:
                self._collect_audio_urls(v, out)
        elif isinstance(node, str):
            s = node.strip()
            if s.startswith("{") or s.startswith("["):
                try:
                    self._collect_audio_urls(json.loads(s), out)
                    return
                except ValueError:
                    pass
            low = s.lower()
            if low.startswith("http") and (low.endswith(".mp3") or low.endswith(".wav") or "audio" in low) and s not in out:
                out.append(s)

    def _download(self, url, dest):
        req = urllib.request.Request(url, headers={"User-Agent": kiemod.UA})
        with urllib.request.urlopen(req, timeout=300) as r:
            data = r.read()
        os.makedirs(os.path.dirname(dest), exist_ok=True)
        open(dest, "wb").write(data)
        log(f"   downloaded {len(data) / 1024:.0f} KB -> {os.path.relpath(dest, ROOT)}")

    def music(self, id_, prompt, dst):
        raw = os.path.join(RAW, "music_" + id_ + ".mp3")
        if ok_file(dst, 50000) and not self.force:
            log(f"[music {id_}] already there, skipped")
            return
        if self.dry:
            log(f"[music {id_}] would generate ({MUSIC_MODEL}) -> {os.path.relpath(dst, ROOT)}")
            return
        if not ok_file(raw, 50000) or self.force:
            log(f"[music {id_}] generating ({MUSIC_MODEL})")
            tid = self._audio_task("/api/v1/generate", {
                "prompt": prompt, "customMode": False, "instrumental": True,
                "model": MUSIC_MODEL, "callBackUrl": "https://example.invalid/kie-callback"})
            urls = self._audio_poll(tid)
            log(f"   {len(urls)} take(s): " + " | ".join(urls[:3]))
            self._download(urls[0], raw)
            for i, u in enumerate(urls[1:3], start=2):
                try:
                    self._download(u, os.path.join(RAW, f"music_{id_}_take{i}.mp3"))
                except Exception as e:  # noqa: BLE001
                    log("   extra take failed:", e)
        process_audio(raw, dst, None, True)

    def sound(self, id_, prompt, dst, loop, trim):
        raw = os.path.join(RAW, "sfx_" + id_ + ".mp3")
        if ok_file(dst, 10000) and not self.force:
            log(f"[sfx {id_}] already there, skipped")
            return
        if self.dry:
            log(f"[sfx {id_}] would generate ({SOUND_MODEL}) -> {os.path.relpath(dst, ROOT)}")
            return
        if not ok_file(raw, 10000) or self.force:
            log(f"[sfx {id_}] generating ({SOUND_MODEL})")
            tid = self._audio_task("/api/v1/generate/sounds", {
                "prompt": prompt[:500], "model": SOUND_MODEL, "soundLoop": bool(loop),
                "callBackUrl": "https://example.invalid/kie-callback"})
            urls = self._audio_poll(tid)
            log(f"   {len(urls)} result(s): " + " | ".join(urls[:3]))
            self._download(urls[0], raw)
        process_audio(raw, dst, trim, loop)


def plan(only):
    items = []
    for gid, style in GAUGES.items():
        items.append(("image", f"gauge_{gid}", dict(prompt=GAUGE_BASE.format(style=style),
                                                    dst=os.path.join(RES, "UI", "Gauges", f"gauge_{gid}.png"), size=512)))
    for cid, desc in CONTAINERS.items():
        items.append(("image", f"{cid}_empty", dict(prompt=desc + ". " + NO_TEXT,
                                                    dst=os.path.join(RES, "UI", "Containers", f"{cid}_empty.png"), size=512)))
    for cid in CONTAINERS:
        items.append(("edit", f"{cid}_full", dict(prompt=FULL_EDIT, base_id=f"{cid}_empty",
                                                  dst=os.path.join(RES, "UI", "Containers", f"{cid}_full.png"), size=512)))
    items.append(("image", "panel", dict(prompt=PANEL, dst=os.path.join(RES, "UI", "panel.png"), size=1024, key=False)))
    for pid, prompt in HUD_PLATES.items():
        items.append(("image", pid, dict(prompt=prompt, dst=os.path.join(RES, "UI", "Hud", f"{pid}.png"), size=1024, square=False)))
    for eid, prompt in HUD_ELEMENTS.items():
        big = eid.startswith("banner_") or eid == "speed_lines"
        items.append(("image", eid, dict(prompt=prompt, dst=os.path.join(RES, "UI", "Hud", f"{eid}.png"), size=1024 if big else 512, square=False,
                                         keyall=(eid == "radar_bezel"), largest=not big, radial_fade=(eid == "banner_rays"),
                                         lum_alpha=(eid == "speed_lines"))))
    items.append(("edit", "tile_on", dict(prompt=TILE_ON_EDIT, base_id="tile_off", dst=os.path.join(RES, "UI", "Hud", "tile_on.png"), size=512, square=False,
                                          largest=True, mask_from_base=True)))
    for mid, prompt in MARKETING.items():
        items.append(("image", f"mk_{mid}", dict(prompt=prompt, dst=os.path.join(ROOT, "marketing", "source", f"{mid}.png"),
                                                 size=2048 if mid != "emblem" else 768, square=mid in ("icon", "emblem"),
                                                 key=(mid == "emblem"))))
    for mid, prompt in MUSIC.items():
        items.append(("music", f"music_{mid}", dict(prompt=prompt, dst=os.path.join(RES, "Audio", "Music", f"{mid}.mp3"))))
    for sid, spec in SOUNDS.items():
        items.append(("sound", f"sfx_{sid}", dict(prompt=spec["prompt"], loop=spec["loop"], trim=spec["trim"],
                                                  dst=os.path.join(RES, "Audio", "Sfx", f"{sid}.wav"))))
    if only:
        items = [it for it in items if it[1] in only]
    return items


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--only", default="")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--images-only", action="store_true")
    ap.add_argument("--audio-only", action="store_true")
    ap.add_argument("--workers", type=int, default=3)
    ap.add_argument("--reprocess", action="store_true", help="rebuild the processed files from raw downloads, no API calls")
    ap.add_argument("--sheet", default="", help="write a contact sheet of the processed images to this path")
    ap.add_argument("--edit-model", default=EDIT_MODEL)
    a = ap.parse_args()

    only = {s.strip() for s in a.only.split(",") if s.strip()}
    items = plan(only)
    if a.images_only:
        items = [it for it in items if it[0] in ("image", "edit")]
    if a.audio_only:
        items = [it for it in items if it[0] in ("music", "sound")]
    if a.list:
        for kind, id_, spec in items:
            state = "done" if ok_file(spec["dst"]) else "todo"
            log(f"{state:5} {kind:6} {id_:16} -> {os.path.relpath(spec['dst'], ROOT)}")
        return 0

    if a.sheet:
        contact_sheet(items, a.sheet)
        return 0

    c = Campaign(force=a.force, dry=a.dry_run, reprocess=a.reprocess)
    before = c.k.credits() if not (a.dry_run or a.reprocess) else None
    log(f"balance before: {before}")
    t0 = time.time()

    images = [it for it in items if it[0] == "image"]
    edits = [it for it in items if it[0] == "edit"]
    audio = [it for it in items if it[0] in ("music", "sound")] if not a.reprocess else []

    def do_image(it):
        _, id_, s = it
        try:
            c.image(id_, s["prompt"], s["dst"], s["size"], key=s.get("key", True), square=s.get("square", True), keyall=s.get("keyall", False), largest=s.get("largest", False), radial_fade=s.get("radial_fade", False), lum_alpha=s.get("lum_alpha", False))
            return id_, None
        except Exception as e:  # noqa: BLE001
            return id_, str(e)

    failures = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=max(1, a.workers)) as ex:
        for id_, err in ex.map(do_image, images):
            if err:
                failures.append((id_, err))
                log(f"[{id_}] FAILED: {err}")

    for _, id_, s in edits:
        base = os.path.join(RAW, s["base_id"] + ".png")
        if not ok_file(base):
            failures.append((id_, "base missing: " + s["base_id"]))
            log(f"[{id_}] skipped, base missing")
            continue
        try:
            c.image(id_, s["prompt"], s["dst"], s["size"], model=a.edit_model, base=base, key=s.get("key", True), square=s.get("square", True),
                    largest=s.get("largest", False), mask_src=base if s.get("mask_from_base") else None)
        except Exception as e:  # noqa: BLE001
            failures.append((id_, str(e)))
            log(f"[{id_}] FAILED: {e}")

    for kind, id_, s in audio:
        try:
            if kind == "music":
                c.music(id_[len("music_"):], s["prompt"], s["dst"])
            else:
                c.sound(id_[len("sfx_"):], s["prompt"], s["dst"], s["loop"], s["trim"])
        except Exception as e:  # noqa: BLE001
            failures.append((id_, str(e)))
            log(f"[{id_}] FAILED: {e}")

    after = c.k.credits() if not (a.dry_run or a.reprocess) else None
    log(f"balance after: {after}  spent: {None if before is None or after is None else round(before - after, 2)}  "
        f"time: {int(time.time() - t0)} s")
    if failures:
        log("FAILURES:")
        for id_, err in failures:
            log(f"  {id_}: {err}")
        return 1
    log("CAMPAIGN OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())
