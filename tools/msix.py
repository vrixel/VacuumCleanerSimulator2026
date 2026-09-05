#!/usr/bin/env python3
"""Packages the Win64 build as an MSIX for the Microsoft Store (full-trust desktop app, no signing: the Store signs).

    python tools\msix.py --identity-name 12345Cosnuau.VacuumCleanerSimulator2026 --publisher "CN=..." \
        --publisher-display Cosnuau [--version 0.1.1] [--out Builds\VacuumCleanerSimulator2026.msix]

The three identity values come from Partner Center: Product management > Product identity. The version must be
x.y.z.0 (the Store reserves the last number). Needs makeappx.exe from the Microsoft.Windows.SDK.BuildTools NuGet
package extracted under D:\DevTools\WindowsSDK-BuildTools (no admin needed), or set VCS_MAKEAPPX.
"""
import argparse
import glob
import os
import re
import shutil
import subprocess
import sys

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
BUILD = os.path.join(ROOT, "Builds", "Win64")
STAGE = os.path.join(ROOT, "Builds", "msix")
EXE = "VacuumCleanerSimulator2026.exe"
APP_ID = "VacuumCleanerSimulator2026"
DISPLAY = "Vacuum Cleaner Simulator 2026"
DESCRIPTION = ("You are the vacuum cleaner. The house is filthy. A silly physics sandbox with a suspiciously serious "
               "cockpit. Ages 8 and up.")

MANIFEST = """<?xml version="1.0" encoding="utf-8"?>
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
         IgnorableNamespaces="uap rescap">
  <Identity Name="{name}" Publisher="{publisher}" Version="{version}" ProcessorArchitecture="x64" />
  <Properties>
    <DisplayName>{display}</DisplayName>
    <PublisherDisplayName>{publisher_display}</PublisherDisplayName>
    <Logo>Assets\\StoreLogo.png</Logo>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Applications>
    <Application Id="{app_id}" Executable="{exe}" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements DisplayName="{display}" Description="{description}" BackgroundColor="transparent"
                          Square150x150Logo="Assets\\Square150x150Logo.png" Square44x44Logo="Assets\\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\\Wide310x150Logo.png" Square310x310Logo="Assets\\Square310x310Logo.png"
                         Square71x71Logo="Assets\\Square71x71Logo.png" />
        <uap:SplashScreen Image="Assets\\SplashScreen.png" />
      </uap:VisualElements>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"""


def find_makeappx():
    env = os.environ.get("VCS_MAKEAPPX")
    if env and os.path.exists(env):
        return env
    hits = sorted(glob.glob(r"D:\DevTools\WindowsSDK-BuildTools\bin\*\x64\makeappx.exe"))
    if hits:
        return hits[-1]
    sys.exit("makeappx.exe not found: extract the Microsoft.Windows.SDK.BuildTools NuGet package under "
             r"D:\DevTools\WindowsSDK-BuildTools or set VCS_MAKEAPPX")


def bundle_version():
    src = open(os.path.join(ROOT, "Assets", "Editor", "ProjectSetup.cs"), encoding="utf-8").read()
    m = re.search(r'bundleVersion\s*=\s*"([\d.]+)"', src)
    return m.group(1) if m else "0.1.0"


def cover(im, w, h, anchor=(0.5, 0.5)):
    sw, sh = im.size
    scale = max(w / sw, h / sh)
    im = im.resize((int(round(sw * scale)), int(round(sh * scale))), Image.LANCZOS)
    x = int((im.width - w) * anchor[0])
    y = int((im.height - h) * anchor[1])
    return im.crop((x, y, x + w, y + h))


def logos(assets):
    os.makedirs(assets, exist_ok=True)
    icon = Image.open(os.path.join(ROOT, "marketing", "icon", "icon_1024.png")).convert("RGBA")
    key = Image.open(os.path.join(ROOT, "marketing", "store", "key_art.png")).convert("RGB")
    for name, size in (("Square150x150Logo", 150), ("Square44x44Logo", 44), ("Square310x310Logo", 310),
                       ("Square71x71Logo", 71), ("StoreLogo", 50)):
        icon.resize((size, size), Image.LANCZOS).save(os.path.join(assets, name + ".png"))
    cover(key, 310, 150, (0.5, 0.45)).save(os.path.join(assets, "Wide310x150Logo.png"))
    cover(key, 620, 300, (0.5, 0.45)).save(os.path.join(assets, "SplashScreen.png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--identity-name", required=True)
    ap.add_argument("--publisher", required=True, help='e.g. "CN=A1B2C3D4-...."')
    ap.add_argument("--publisher-display", required=True)
    ap.add_argument("--version", default=None, help="x.y.z, .0 is appended (default: bundleVersion)")
    ap.add_argument("--out", default=None)
    a = ap.parse_args()

    if not os.path.exists(os.path.join(BUILD, EXE)):
        sys.exit(f"no build at {BUILD}: run tools\\build.ps1 first")
    version = (a.version or bundle_version()).strip()
    if version.count(".") == 2:
        version += ".0"
    if not re.fullmatch(r"\d+\.\d+\.\d+\.0", version):
        sys.exit(f"version must be x.y.z.0 for the Store, got {version}")
    out = a.out or os.path.join(ROOT, "Builds", f"VacuumCleanerSimulator2026-v{version}.msix")

    if os.path.exists(STAGE):
        shutil.rmtree(STAGE)
    shutil.copytree(BUILD, STAGE, ignore=shutil.ignore_patterns("*.pdb", "*_BurstDebugInformation_DoNotShip"))
    logos(os.path.join(STAGE, "Assets"))
    manifest = MANIFEST.format(name=a.identity_name, publisher=a.publisher, version=version, display=DISPLAY,
                               publisher_display=a.publisher_display, app_id=APP_ID, exe=EXE, description=DESCRIPTION)
    with open(os.path.join(STAGE, "AppxManifest.xml"), "w", encoding="utf-8") as f:
        f.write(manifest)

    makeappx = find_makeappx()
    if os.path.exists(out):
        os.remove(out)
    cmd = [makeappx, "pack", "/o", "/d", STAGE, "/p", out]
    print(" ".join(cmd))
    r = subprocess.run(cmd, capture_output=True, text=True)
    sys.stdout.write(r.stdout[-2000:])
    sys.stderr.write(r.stderr[-2000:])
    if r.returncode != 0:
        sys.exit(f"makeappx failed ({r.returncode})")
    print(f"MSIX: {out}  {os.path.getsize(out) // (1024 * 1024)} MB  (unsigned: upload it to Partner Center, the Store signs)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
