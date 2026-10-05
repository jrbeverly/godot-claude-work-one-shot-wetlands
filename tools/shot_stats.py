#!/usr/bin/env python3
"""Luminance statistics for rendered shots: checklist item 3 (value structure) as numbers.
Prints p5/p25/p50/p75/p95 lightness (0-100), the fraction of pixels above 80 and below 12, and the mean hue/sat,
so 'nothing pure white, nothing pure black, three value bands' can be checked per iteration.
Usage: tools/shot_stats.py screenshots/<run>/*.png"""
import sys
import numpy as np
from PIL import Image

for path in sys.argv[1:]:
    im = np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255.0
    lum = (0.2126 * im[:, :, 0] + 0.7152 * im[:, :, 1] + 0.0722 * im[:, :, 2]) * 100
    hsv = np.asarray(Image.open(path).convert("HSV")).astype(np.float32)
    p = np.percentile(lum, [5, 25, 50, 75, 95])
    print(f"{path.split('/')[-1]:16s} L p5/25/50/75/95 = {p[0]:4.0f} {p[1]:4.0f} {p[2]:4.0f} {p[3]:4.0f} {p[4]:4.0f}  >80: {np.mean(lum > 80) * 100:4.1f}%  <12: {np.mean(lum < 12) * 100:4.1f}%  sat mean {hsv[:, :, 1].mean() / 2.55:4.0f}")
