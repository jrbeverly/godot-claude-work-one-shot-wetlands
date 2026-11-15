#!/usr/bin/env python3
"""Copy the rendered hero shots into docs/screenshots/ as 960x540 JPEGs for the README and world bible.
Usage: tools/curate_screenshots.py [screenshots/fallowmere] [docs/screenshots]"""
import sys, os, glob
from PIL import Image

src = sys.argv[1] if len(sys.argv) > 1 else "screenshots/fallowmere"
dst = sys.argv[2] if len(sys.argv) > 2 else "docs/screenshots"
os.makedirs(dst, exist_ok=True)
for png in sorted(glob.glob(os.path.join(src, "*.png"))):
    name = os.path.splitext(os.path.basename(png))[0]
    im = Image.open(png).convert("RGB")
    im = im.resize((960, 540), Image.LANCZOS)
    out = os.path.join(dst, f"{name}.jpg")
    im.save(out, "JPEG", quality=86, optimize=True)
    print(f"{out}: {os.path.getsize(out) // 1024} KiB")
