#!/usr/bin/env python3
"""Hose slurps, synthesised (2026-09-29, his "it sounds like radio noise rather than smooth vacuum slurps, not
satisfying"). The generated absorb_* hits are broadband hiss (up to 20 % of their energy above 4 kHz, spectral
flatness 0.2), so they read as static. A real slurp has a shape instead: the object plugs the nozzle and the
airflow narrows into a rising whistle, then it pops through the hose with a low wet "thwup", then the air rushes
back. Built here from filtered noise and swept sines, no API call, deterministic (fixed seeds):

    python tools/assets/slurps.py            # writes Assets/Resources/Audio/Sfx/slurp_{small,medium,big}_N.wav
    python tools/assets/slurps.py --demo D   # also D/slurp-demo.wav: old hits vs new ones over the motor loop

GameAudio loads slurp_* before absorb_*; the kie raws and their processed absorb_* files stay untouched, so
kie_assets.py --reprocess never overwrites these.
"""
import argparse
import os

import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt, sosfiltfilt

SR = 48000
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SFX = os.path.join(ROOT, "Assets", "Resources", "Audio", "Sfx")


def env(n, attack, release_pow=1.6):
    t = np.linspace(0, 1, n)
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    return a * (1 - t) ** release_pow


def swept_band(noise, f0, f1, q0, q1, block=256):
    """Noise through a band-pass whose centre and Q glide from (f0, q0) to (f1, q1): the hose whistle."""
    out = np.zeros_like(noise)
    n = len(noise)
    zi = None
    for s in range(0, n, block):
        k = s / max(n - 1, 1)
        fc = f0 * (f1 / f0) ** k
        q = q0 + (q1 - q0) * k
        bw = fc / q
        lo, hi = max(40, fc - bw / 2), min(SR / 2 - 100, fc + bw / 2)
        sos = butter(2, [lo, hi], btype="band", fs=SR, output="sos")
        seg = noise[s:s + block]
        if zi is None or zi.shape[0] != sos.shape[0]:
            zi = np.zeros((sos.shape[0], 2))
        y, zi = sosfilt_zi(sos, seg, zi)
        out[s:s + block] = y
    return out


def sosfilt_zi(sos, x, zi):
    y, zf = sosfilt(sos, x, zi=zi)
    return y, zf


def thwup(dur, f0, f1, rng):
    """The pop through the hose: a sine dropping in pitch, a touch of noise for wetness."""
    n = int(dur * SR)
    t = np.arange(n) / SR
    f = f0 * (f1 / f0) ** (t / dur)
    ph = 2 * np.pi * np.cumsum(f) / SR
    body = np.sin(ph) + 0.25 * np.sin(2 * ph)
    wet = sosfiltfilt(butter(2, [f0 * 0.8, f0 * 3], btype="band", fs=SR, output="sos"), rng.standard_normal(n))
    return (body + 0.35 * wet / (np.abs(wet).max() + 1e-9)) * env(n, 0.04, 2.2)


def rattle(dur, count, f, rng):
    """Something hard knocking its way up the pipe: decaying clicks, closer together as it speeds up."""
    n = int(dur * SR)
    out = np.zeros(n)
    times = np.cumsum(np.linspace(1.4, 0.6, count))
    times = times / times[-1] * dur * 0.85
    for i, tt in enumerate(times):
        s = int(tt * SR)
        m = int(0.03 * SR)
        k = np.arange(m) / SR
        click = np.sin(2 * np.pi * f * (1 + 0.1 * rng.standard_normal()) * k) * np.exp(-k * 160)
        e = min(n, s + m)
        out[s:e] += click[:e - s] * (0.6 + 0.4 * rng.random()) * (1 - 0.5 * i / count)
    return out


def slurp(size, seed):
    rng = np.random.default_rng(seed)
    # (whistle length, start Hz, end Hz, pop length, pop from, pop to, rattle clicks, rattle Hz)
    p = {
        "small": (0.16, 420, 1500, 0.09, 260, 110, 0, 0),
        "medium": (0.24, 300, 1100, 0.13, 190, 75, 5, 900),
        "big": (0.34, 200, 750, 0.20, 130, 48, 8, 520),
    }[size]
    wl, f0, f1, pl, p0, p1, clicks, cf = p
    j = 1 + 0.08 * rng.standard_normal()
    wl *= j
    n = int(wl * SR)
    noise = rng.standard_normal(n)
    whistle = swept_band(noise, f0 * j, f1 * j, 2.0, 9.0)
    whistle /= np.abs(whistle).max() + 1e-9
    whistle *= np.clip(np.linspace(0, 1, n) / 0.3, 0, 1) ** 1.5   # swells as the nozzle closes
    pop = thwup(pl, p0 * j, p1, rng)
    total = n + int(0.02 * SR) + len(pop) + int(0.12 * SR)
    out = np.zeros(total)
    out[:n] += 0.55 * whistle
    s = n - int(0.01 * SR)
    out[s:s + len(pop)] += 0.9 * pop
    if clicks:
        r = rattle(wl * 0.9 + pl, clicks, cf, rng)
        out[int(0.05 * SR):int(0.05 * SR) + len(r)] += 0.35 * r
    # the air rushing back in after the pop: soft low-passed breath, fading
    tail_n = int(0.18 * SR)
    tail = sosfiltfilt(butter(2, 900, fs=SR, output="sos"), rng.standard_normal(tail_n)) * env(tail_n, 0.15, 2.0)
    ts = s + len(pop) // 2
    e = min(total, ts + tail_n)
    out[ts:e] += 0.25 * tail[:e - ts] / (np.abs(tail).max() + 1e-9)
    # no hiss anywhere: everything above 5 kHz goes
    out = sosfiltfilt(butter(4, 5000, fs=SR, output="sos"), out)
    out = sosfiltfilt(butter(2, 45, btype="high", fs=SR, output="sos"), out)
    fade = int(0.01 * SR)
    out[-fade:] *= np.linspace(1, 0, fade)
    return out / (np.abs(out).max() + 1e-9) * 0.89   # -1 dBFS like the other one-shots


def write(path, a):
    wavfile.write(path, SR, (np.clip(a, -1, 1) * 32767).astype(np.int16))


def load(path):
    sr, a = wavfile.read(path)
    a = a.astype(float) / 32768
    return a.mean(1) if a.ndim > 1 else a


def demo(out_dir, made):
    motor = load(os.path.join(SFX, "motor_loop.wav"))
    air = load(os.path.join(SFX, "suction_loop.wav"))
    air_soft = sosfiltfilt(butter(2, 1600, fs=SR, output="sos"), air)
    old = [load(os.path.join(SFX, f"absorb_small_{i}.wav")) for i in (1, 2, 3)] + \
          [load(os.path.join(SFX, "absorb_medium_1.wav")), load(os.path.join(SFX, "absorb_big_1.wav"))]
    new = made["small"] + [made["medium"][0], made["big"][0]]
    seq = [0, 1, 2, 0, 3, 1, 2, 4]
    times = [0.4, 0.75, 1.0, 1.3, 1.8, 2.3, 2.6, 3.2]

    def scene(hits, bed):
        n = int(4.5 * SR)
        mix = 0.4 * np.resize(motor, n) + 0.5 * np.resize(bed, n)
        for i, t in zip(seq, times):
            h = hits[min(i, len(hits) - 1)] * 0.6
            s = int(t * SR)
            e = min(n, s + len(h))
            mix[s:e] += h[:e - s]
        return mix / max(1.0, np.abs(mix).max()) * 0.9

    gap = np.zeros(int(0.8 * SR))
    write(os.path.join(out_dir, "slurp-demo.wav"), np.concatenate([scene(old, air), gap, scene(new, air_soft)]))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--demo", default="")
    a = ap.parse_args()
    made = {}
    for size, count, base in (("small", 3, 11), ("medium", 2, 21), ("big", 2, 31)):
        made[size] = []
        for i in range(count):
            s = slurp(size, base + i)
            made[size].append(s)
            path = os.path.join(SFX, f"slurp_{size}_{i + 1}.wav")
            write(path, s)
            print(f"{path}  {len(s) / SR:.2f} s")
    if a.demo:
        os.makedirs(a.demo, exist_ok=True)
        demo(a.demo, made)
        print("demo in", a.demo)


if __name__ == "__main__":
    main()
