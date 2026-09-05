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

MUSIC = {
    "title": "Quirky upbeat jazzy theme for a silly video game about a vacuum cleaner, instrumental, kazoo, tuba, ukulele and brushed drums, cheerful and mischievous, 95 bpm, loopable, no vocals",
    "game": "Energetic playful funk with chiptune touches for a comedy physics game, instrumental, bouncy bass, clavinet, hand claps, 120 bpm, loopable, no vocals",
}

SOUNDS = {
    "motor_loop": {"prompt": "A household vacuum cleaner motor running steadily, close microphone, constant hum with airflow, no music, clean loop", "loop": True, "trim": 6.0},
    "pop_real": {"prompt": "A vacuum cleaner nozzle sucking up a small object, one short thwop pop with a rattle inside the hose, no music", "loop": False, "trim": 1.2},
    "bag_alarm": {"prompt": "Small household appliance warning beep, three short electronic beeps, bag full alarm, no music", "loop": False, "trim": 2.0},
}


def log(*a):
    print(*a, flush=True)


def ok_file(p, min_bytes=4096):
    return os.path.exists(p) and os.path.getsize(p) > min_bytes


def key_background(im):
    """Makes the flat studio background transparent. The models ignore 'pure black background' now and
    then and hand back white or grey: the background colour is read from the corners, not assumed."""
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
    alpha = np.where(border == 1, alpha, np.maximum(alpha, 1.0))
    rgba = np.dstack([a, alpha * 255.0]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA"), bg


def process_image(src, dst, size, key=True):
    from PIL import Image
    im = Image.open(src).convert("RGB")
    w, h = im.size
    side = min(w, h)
    im = im.crop(((w - side) // 2, (h - side) // 2, (w - side) // 2 + side, (h - side) // 2 + side))
    im = im.resize((size, size), Image.LANCZOS)
    if key:
        im, bg = key_background(im)
        log(f"   background keyed: rgb({int(bg[0])},{int(bg[1])},{int(bg[2])})")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    im.save(dst, "PNG", optimize=True)
    log(f"   -> {os.path.relpath(dst, ROOT)} {size}x{size}")


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
    def image(self, id_, prompt, dst, size, model=IMAGE_MODEL, base=None, key=True):
        raw = os.path.join(RAW, id_ + ".png")
        if self.reprocess:
            if ok_file(raw):
                log(f"[{id_}] reprocessing from raw")
                process_image(raw, dst, size, key)
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
        process_image(raw, dst, size, key)

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
            c.image(id_, s["prompt"], s["dst"], s["size"], key=s.get("key", True))
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
            c.image(id_, s["prompt"], s["dst"], s["size"], model=a.edit_model, base=base)
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
