#!/usr/bin/env python3
"""Measures the processed sound effects instead of trusting them: length, peak, loudness, leading silence, and for
the loops the seam (a click at the join shows as a sample jump far above the clip's own sample-to-sample motion,
a level step as an RMS difference between the last and the first 100 ms).

    python tools/assets/audio_check.py            # every clip in Assets/Resources/Audio/Sfx
    python tools/assets/audio_check.py motor_loop suction_choke
"""
import math
import os
import struct
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SFX = os.path.join(ROOT, "Assets", "Resources", "Audio", "Sfx")
LOOPS = {"motor_loop", "motor_full", "suction_loop", "suction_choke", "rewind_loop", "turbo_loop"}


def samples(path):
    # decoded at the file's own rate and channel count: resampling overshoots on a clip normalised close to full
    # scale, and ffmpeg's stereo-to-mono downmix sums the channels; both report peaks above 0 dBFS that are not
    # in the file. Returns the first channel and the rate.
    info = subprocess.check_output(["ffprobe", "-v", "error", "-select_streams", "a:0", "-show_entries",
                                    "stream=sample_rate,channels", "-of", "default=nw=1:nk=1", path]).decode().split()
    rate, channels = int(info[0]), int(info[1])
    raw = subprocess.check_output(["ffmpeg", "-v", "error", "-i", path, "-f", "f32le", "-"])
    xs = struct.unpack("<%df" % (len(raw) // 4), raw)
    return xs[::channels], rate


def db(x):
    return 20 * math.log10(x) if x > 1e-9 else -99.0


def rms(xs):
    return math.sqrt(sum(v * v for v in xs) / max(1, len(xs)))


def measure(name):
    xs, rate = samples(os.path.join(SFX, name + ".wav"))
    n = len(xs)
    peak = max(abs(v) for v in xs)
    lead = next((i for i, v in enumerate(xs) if abs(v) > 0.01), n) / rate
    row = {"name": name, "sec": n / rate, "peak": db(peak), "rms": db(rms(xs)), "lead": lead}
    if name in LOOPS:
        # the join: the jump between the last and the first sample against the clip's own biggest sample-to-sample
        # step (a click is a jump the clip never makes itself), and the level step across the join
        w = rate // 10
        slew = max(abs(xs[i + 1] - xs[i]) for i in range(n - 1)) or 1e-6
        row["seam_jump"] = abs(xs[0] - xs[-1]) / slew
        row["seam_level"] = db(rms(xs[:w])) - db(rms(xs[-w:]))
    return row


def main():
    names = sys.argv[1:] or sorted(f[:-4] for f in os.listdir(SFX) if f.endswith(".wav"))
    print(f"{'clip':18} {'sec':>5} {'peak':>6} {'rms':>6} {'lead':>5}  seam")
    for name in names:
        r = measure(name)
        seam = ""
        if "seam_jump" in r:
            ok = r["seam_jump"] < 1.0 and abs(r["seam_level"]) < 1.5
            seam = f"jump {r['seam_jump']:.2f} of slew, level {r['seam_level']:+.1f} dB {'ok' if ok else 'CHECK'}"
        print(f"{r['name']:18} {r['sec']:5.2f} {r['peak']:6.1f} {r['rms']:6.1f} {r['lead']:5.2f}  {seam}")


if __name__ == "__main__":
    main()
