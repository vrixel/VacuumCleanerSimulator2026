#!/usr/bin/env python3
"""Installs the Android Build Support module for the editor without the Hub and without UAC.

The Hub refuses to add modules to an editor it did not install, so this does what the Hub would do, from the
same release manifest (services.api.unity.com): runs the Android target-support installer silently into the
editor folder (RunAsInvoker, like tools/install-unity.ps1), then unpacks OpenJDK, the SDK tools, the NDK, the
build-tools, the platform-tools, the platforms and the command-line tools into
Editor/Data/PlaybackEngines/AndroidPlayer, applying the manifest's rename rules. About 3 GB of downloads, kept
in D:/Temp/claude/unity-android so a rerun only fetches what is missing.

    python tools/install-android.py [--version 6000.3.23f1] [--editor "D:/Program Files/Unity/Hub/Editor/6000.3.23f1"] [--dry-run]
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.request
import zipfile

sys.stdout.reconfigure(encoding="utf-8")
CACHE = r"D:\Temp\claude\unity-android"
API = "https://services.api.unity.com/unity/editor/release/v1/releases?version={v}&platform=WINDOWS&architecture=X86_64&limit=1"


def log(*a):
    print(*a, flush=True)


def fetch(url, dest, expect=None):
    if os.path.exists(dest) and (expect is None or abs(os.path.getsize(dest) - expect) < 1024 * 1024):
        log(f"   cached {os.path.basename(dest)} ({os.path.getsize(dest) // 1_000_000} MB)")
        return
    log(f"   downloading {os.path.basename(dest)} ...")
    tmp = dest + ".part"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=120) as r, open(tmp, "wb") as f:
        total = int(r.headers.get("Content-Length") or 0)
        got = 0
        last = 0
        while True:
            buf = r.read(1 << 20)
            if not buf:
                break
            f.write(buf)
            got += len(buf)
            if time.time() - last > 15:
                last = time.time()
                pct = f"{100 * got // total}%" if total else ""
                log(f"      {got // 1_000_000} MB {pct} {got / 1e6 / max(1e-6, time.time() - t0):.1f} MB/s")
    os.replace(tmp, dest)
    log(f"   done {os.path.basename(dest)} ({got // 1_000_000} MB in {int(time.time() - t0)} s)")


def unzip(zip_path, dest):
    os.makedirs(dest, exist_ok=True)
    with zipfile.ZipFile(zip_path) as z:
        z.extractall(dest)


def collect(mods, out):
    for m in mods:
        if m["id"].startswith("android") or "android" in m["id"] or m["id"].startswith("open-jdk"):
            out.append(m)
        collect(m.get("subModules", []), out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", default="6000.3.23f1")
    ap.add_argument("--editor", default=r"D:\Program Files\Unity\Hub\Editor\6000.3.23f1")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    unity = a.editor.replace("\\", "/")
    os.makedirs(CACHE, exist_ok=True)

    rel = json.load(urllib.request.urlopen(API.format(v=a.version), timeout=60))["results"][0]
    dl = next(d for d in rel["downloads"] if d["platform"] == "WINDOWS")
    mods = []
    collect(dl.get("modules", []), mods)
    if not mods:
        log("no android module in the manifest")
        return 1
    for m in mods:
        log(f"{m['id']:36} {m['type']:4} {round(m.get('downloadSize', {}).get('value', 0) / 1e6):5} MB -> {m.get('destination', '')}")
    if a.dry_run:
        return 0

    player = os.path.join(unity, "Editor/Data/PlaybackEngines/AndroidPlayer")
    for m in mods:
        url = m["url"]
        name = m["id"] + (".exe" if m["type"] == "EXE" else ".zip")
        path = os.path.join(CACHE, name)
        log(f"[{m['id']}]")
        fetch(url, path, m.get("downloadSize", {}).get("value"))
        dest = (m.get("destination") or "{UNITY_PATH}").replace("{UNITY_PATH}", unity)
        if m["type"] == "EXE":
            if os.path.isdir(player) and os.listdir(player):
                log("   AndroidPlayer already present, installer skipped")
                continue
            log("   running the target support installer silently (no UAC)...")
            env = dict(os.environ, __COMPAT_LAYER="RunAsInvoker")
            p = subprocess.run([path, "/S", "/D=" + a.editor], env=env)
            log(f"   installer exit {p.returncode}; AndroidPlayer present: {os.path.isdir(player)}")
            if not os.path.isdir(player):
                log("   the installer did not produce Editor/Data/PlaybackEngines/AndroidPlayer, stopping")
                return 1
            continue
        marker = os.path.join(dest, ".vcs-" + m["id"])
        if os.path.exists(marker):
            log(f"   already unpacked into {dest}")
            continue
        log(f"   unpacking into {dest}")
        unzip(path, dest)
        ren = m.get("extractedPathRename")
        if ren:
            src = ren["from"].replace("{UNITY_PATH}", unity)
            dst = ren["to"].replace("{UNITY_PATH}", unity)
            if os.path.isdir(src) and os.path.abspath(src) != os.path.abspath(dst):
                if os.path.isdir(dst) and os.listdir(dst):
                    # merge (the destination may be the parent that already holds the extracted folder)
                    for item in os.listdir(src):
                        shutil.move(os.path.join(src, item), os.path.join(dst, item))
                    shutil.rmtree(src, ignore_errors=True)
                else:
                    if os.path.isdir(dst):
                        os.rmdir(dst)
                    shutil.move(src, dst)
                log(f"   renamed {os.path.relpath(src, unity)} -> {os.path.relpath(dst, unity)}")
        open(marker, "w").write(url)
    log("ANDROID MODULE OK")
    for probe in ("OpenJDK/bin/java.exe", "NDK/ndk-build.cmd", "SDK/platform-tools/adb.exe", "SDK/build-tools/36.0.0/aapt2.exe", "SDK/platforms/android-35/android.jar", "SDK/cmdline-tools/16.0/bin/sdkmanager.bat"):
        log(f"   {'ok ' if os.path.exists(os.path.join(player, probe)) else 'MISSING'} {probe}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
