#!/usr/bin/env python3
"""The store trailer, assembled from the game's own recording (2026-09-22, his "edited video capture").

    powershell -File tools\\record.ps1          # the game writes Builds\\record\\frame_*.jpg and marks.txt
    python tools\\store_video.py                 # -> marketing\\video\\trailer_1920x1080.mp4 (+ poster, contact sheet)
    python tools\\store_video.py --no-audio      # picture only, for a quick look

A screen recorder cannot run on this PC (locked RDP session), so SmokeRunner.RecordRun locks the game clock to
30 steps per second and saves every frame; here PIL puts a title card in front, a chapter caption on each mark
(the arcade caption of tools/brand.py, faded in and out), an end card behind, and pipes the frames raw into
ffmpeg with the game's music under a low motor hum. H.264 + AAC, 1920 x 1080, under 60 s: the Microsoft Store
trailer format; Play takes the same file through YouTube. The App Store preview is a separate job (device-size
footage, no other platform named), not made here.

Look at marketing/video/trailer_sheet.png afterwards: the log only proves ffmpeg ran.
"""
import argparse
import glob
import os
import subprocess
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import brand  # noqa: E402

ROOT = brand.ROOT
REC = os.path.join(ROOT, "Builds", "record")
OUT = os.path.join(ROOT, "marketing", "video")
MUSIC = os.path.join(ROOT, "Assets", "Resources", "Audio", "Music", "game.mp3")
MOTOR = os.path.join(ROOT, "Assets", "Resources", "Audio", "Sfx", "motor_loop.wav")
FPS = 30
SIZE = (1920, 1080)
TITLE_S = 2.6
END_S = 3.2
COCKPIT = 250   # px, the strip at the bottom of the desktop HUD; captions sit above it

# chapter -> (headline, subline); None = no caption
CAPTIONS = {
    "title": None,
    "garage": ("NINETEEN MACHINES", "Pick your vacuum. Each one drives, sucks and hops differently."),
    "drive": ("YOU ARE THE VACUUM", "The house is filthy. It is not going to clean itself."),
    "turbo": ("HOLD THE BOOST", "Sparks, speed lines and a trail of dust."),
    "cat": ("CHASE THE CAT", "It has opinions about you."),
    "powder": ("LEAVE YOUR MARK", "The cocoa powder remembers where you went."),
    "cord": ("A CORD THAT FIGHTS BACK", "Keep pulling and the plug pops out of the wall."),
    "bin": ("BAG FULL? FIND THE BIN", "Empty it, or blow it all back out and start again."),
    "end": None,
}
CAPTION_MAX_S = 3.4
FADE_S = 0.25


def read_marks():
    marks = []
    with open(os.path.join(REC, "marks.txt"), encoding="utf-8") as f:
        for line in f:
            parts = line.split()
            if len(parts) == 2:
                marks.append((parts[0], int(parts[1])))
    return marks


def card(kind):
    """The title and end cards: wordmark on the navy studio of the store art, the yellow / blue rule."""
    im = brand.studio(SIZE, (0.5, 0.45), 0.34, blueprint=0.45)   # navy studio + blueprint at 45 % (his pick, 2026-09-23)
    wm = brand.wordmark(1240)
    wm = brand.drop_shadow(wm, 26, (0, 18), 170)
    im.alpha_composite(wm, ((SIZE[0] - wm.size[0]) // 2, (SIZE[1] - wm.size[1]) // 2 - (60 if kind == "end" else 30)))
    rule = brand.stripes(SIZE[0], 12)
    im.alpha_composite(rule, (0, SIZE[1] - 120))
    if kind == "title":
        sub = brand.plain_text("Suck it up. All of it.", 46, brand.STEEL, "Exo2")   # S3 of docs/STORE-MATRIX.md, the one tagline
        im.alpha_composite(sub, ((SIZE[0] - sub.size[0]) // 2, SIZE[1] - 210))
    else:
        t = brand.tab("OUT NOW", 74, brand.YELLOW, skew=True)
        im.alpha_composite(t, ((SIZE[0] - t.size[0]) // 2, SIZE[1] // 2 + 150))
        sub = brand.plain_text("Windows  /  Android  /  iPhone and iPad", 44, brand.STEEL, "Exo2")
        im.alpha_composite(sub, ((SIZE[0] - sub.size[0]) // 2, SIZE[1] - 210))
    return im.convert("RGB")


def with_alpha(layer, a):
    if a >= 0.999:
        return layer
    r, g, b, al = layer.split()
    al = al.point(lambda v: int(v * a))
    return Image.merge("RGBA", (r, g, b, al))


def fade_to_black(im, k):
    """k = 1 keeps the picture, 0 gives black."""
    if k >= 0.999:
        return im
    return Image.blend(Image.new("RGB", im.size, (0, 0, 0)), im, max(0.0, k))


def build_plan(frames, marks):
    """Per game frame: which caption (if any) and its alpha, plus the frame index of the chapter it belongs to."""
    n = len(frames)
    starts = {name: f for name, f in marks}
    order = [m for m in marks]
    plan = [(None, 0.0)] * n
    for i, (name, f0) in enumerate(order):
        f1 = order[i + 1][1] if i + 1 < len(order) else n
        cap = CAPTIONS.get(name)
        if not cap:
            continue
        a = f0 + int(0.3 * FPS)
        b = min(f1 - int(0.15 * FPS), a + int(CAPTION_MAX_S * FPS))
        if b - a < int(0.8 * FPS):
            continue
        fade = int(FADE_S * FPS)
        for f in range(a, b):
            if f >= n:
                break
            k = min(1.0, (f - a + 1) / fade, (b - f) / fade)
            plan[f] = (name, k)
    return plan, starts


def caption_image(name, style):
    head, sub = CAPTIONS[name]
    return brand.caption(head, sub, 66, sub_size=32)


def encode(frames, marks, no_audio):
    os.makedirs(OUT, exist_ok=True)
    mp4 = os.path.join(OUT, "trailer_1920x1080.mp4")
    plan, starts = build_plan(frames, marks)
    caps = {name: caption_image(name, None) for name in CAPTIONS if CAPTIONS[name]}
    title_card = card("title")
    end_card = card("end")
    game_frames = len(frames)
    total_s = TITLE_S + game_frames / FPS + END_S
    print(f"{game_frames} game frames, {total_s:.1f} s total")

    cmd = ["ffmpeg", "-y", "-hide_banner", "-loglevel", "error",
           "-f", "rawvideo", "-pix_fmt", "rgb24", "-s", f"{SIZE[0]}x{SIZE[1]}", "-r", str(FPS), "-i", "-"]
    if not no_audio:
        motor_delay = int(TITLE_S * 1000)
        motor_len = game_frames / FPS
        cmd += ["-i", MUSIC, "-stream_loop", "-1", "-i", MOTOR,
                "-filter_complex",
                f"[1:a]atrim=0:{total_s:.3f},afade=t=in:st=0:d=0.6,afade=t=out:st={total_s - 2.2:.3f}:d=2.2,volume=0.9[m];"
                f"[2:a]atrim=0:{motor_len:.3f},afade=t=in:st=0:d=0.4,afade=t=out:st={motor_len - 0.6:.3f}:d=0.6,"
                f"volume=0.22,adelay={motor_delay}|{motor_delay}[e];"
                f"[m][e]amix=inputs=2:duration=first:normalize=0[a]",
                "-map", "0:v", "-map", "[a]", "-c:a", "aac", "-b:a", "192k"]
    cmd += ["-c:v", "libx264", "-preset", "medium", "-crf", "18", "-pix_fmt", "yuv420p", "-movflags", "+faststart",
            "-t", f"{total_s:.3f}", mp4]
    p = subprocess.Popen(cmd, stdin=subprocess.PIPE)
    w = p.stdin

    def emit(im):
        w.write(im.tobytes())

    # the title card: fade in from black, hold, hard cut to the game
    nt = int(TITLE_S * FPS)
    for f in range(nt):
        emit(fade_to_black(title_card, (f + 1) / (0.6 * FPS)))
    # the game, with captions
    poster = None
    for f, path in enumerate(frames):
        im = Image.open(path).convert("RGB")
        if im.size != SIZE:
            im = im.resize(SIZE, Image.LANCZOS)
        name, k = plan[f]
        if name and k > 0:
            layer = with_alpha(caps[name], k)
            on_hud = f >= starts.get("drive", 0) and f < starts.get("end", len(frames))
            bottom = SIZE[1] - (COCKPIT + 28 if on_hud else 90)
            pos = ((SIZE[0] - layer.size[0]) // 2, bottom - layer.size[1])
            im = im.convert("RGBA")
            im.alpha_composite(layer, pos)
            im = im.convert("RGB")
        if poster is None and f == starts.get("cat", -1) + int(1.4 * FPS):
            poster = im.copy()
        if f >= len(frames) - int(0.5 * FPS):   # fade to black into the end card
            im = fade_to_black(im, (len(frames) - f) / (0.5 * FPS))
        emit(im)
    ne = int(END_S * FPS)
    for f in range(ne):
        k = min(1.0, (f + 1) / (0.5 * FPS), (ne - f) / (0.8 * FPS))
        emit(fade_to_black(end_card, k))
    w.close()
    rc = p.wait()
    if rc != 0:
        print("ffmpeg failed", rc)
        return None
    if poster is None:
        poster = Image.open(frames[len(frames) // 2]).convert("RGB")
    poster.save(os.path.join(OUT, "trailer_poster.png"))
    return mp4


def sheet(mp4, cols=4, n=16):
    """Sixteen frames spread over the film, the picture to check."""
    tmp = os.path.join(OUT, "_sheet_%02d.png")
    dur = float(subprocess.check_output(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                                         "-of", "csv=p=0", mp4]).decode().strip())
    subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", mp4,
                    "-vf", f"fps={n / dur:.5f},scale=480:-1", "-frames:v", str(n), tmp], check=True)
    files = sorted(glob.glob(os.path.join(OUT, "_sheet_*.png")))
    tw, th = 480, 270
    rows = (len(files) + cols - 1) // cols
    im = Image.new("RGB", (cols * (tw + 8) + 8, rows * (th + 8) + 8), (36, 40, 48))
    for i, fpath in enumerate(files):
        t = Image.open(fpath).convert("RGB")
        im.paste(t, (8 + (i % cols) * (tw + 8), 8 + (i // cols) * (th + 8)))
        os.remove(fpath)
    out = os.path.join(OUT, "trailer_sheet.png")
    im.save(out)
    print("sheet", os.path.relpath(out, ROOT), f"({dur:.1f} s)")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--no-audio", action="store_true")
    a = ap.parse_args()
    frames = sorted(glob.glob(os.path.join(REC, "frame_*.jpg")))
    if not frames or not os.path.exists(os.path.join(REC, "marks.txt")):
        print("no recording in", REC, ": run tools\\record.ps1 first")
        return 2
    marks = read_marks()
    print("marks:", ", ".join(f"{n}@{f}" for n, f in marks))
    mp4 = encode(frames, marks, a.no_audio)
    if not mp4:
        return 1
    print("video", os.path.relpath(mp4, ROOT), os.path.getsize(mp4) // 1024, "KB")
    sheet(mp4)
    return 0


if __name__ == "__main__":
    sys.exit(main())
