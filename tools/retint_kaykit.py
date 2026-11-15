#!/usr/bin/env python3
"""Muted palette atlas for the KayKit landmark buildings.

Reads assets/third_party/kaykit_landmarks/hexagons_medieval.png (8x4 gradient swatches) and writes
hexagons_medieval_muted.png next to it. The saturated blues (roofs, banners) become slate blue-grey (~#5C6E80),
bright oranges/reds become warm brown, saturated greens/yellows are calmed, and the greys and woods are kept.
Kit.cs swaps the albedo texture at runtime, so the glTF files stay untouched.

Usage: python3 tools/retint_kaykit.py
"""
from __future__ import annotations

import colorsys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "assets/third_party/kaykit_landmarks/hexagons_medieval.png"
DST = ROOT / "assets/third_party/kaykit_landmarks/hexagons_medieval_muted.png"

SLATE = (0x5C / 255, 0x6E / 255, 0x80 / 255)   # target for the blue roof swatches


def rgb_to_hsv(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1)
    mn = rgb.min(-1)
    d = mx - mn
    h = np.zeros_like(mx)
    nz = d > 1e-6
    rm, gm, bm = (mx == r) & nz, (mx == g) & nz & (mx != r), (mx == b) & nz & (mx != r) & (mx != g)
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    h = h / 6.0
    s = np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0)
    return np.stack([h, s, mx], -1)


def hsv_to_rgb(hsv: np.ndarray) -> np.ndarray:
    h, s, v = hsv[..., 0] % 1.0, hsv[..., 1], hsv[..., 2]
    i = np.floor(h * 6).astype(int)
    f = h * 6 - i
    p, q, t = v * (1 - s), v * (1 - s * f), v * (1 - s * (1 - f))
    i = i % 6
    r = np.choose(i, [v, q, p, p, t, v])
    g = np.choose(i, [t, v, v, q, p, p])
    b = np.choose(i, [p, p, t, v, v, q])
    return np.stack([r, g, b], -1)


def main() -> None:
    img = Image.open(SRC).convert("RGBA")
    arr = np.asarray(img).astype(np.float32) / 255.0
    rgb, alpha = arr[..., :3], arr[..., 3:]
    hsv = rgb_to_hsv(rgb)
    h, s, v = hsv[..., 0] * 360.0, hsv[..., 1], hsv[..., 2]
    out = hsv.copy()

    slate_h, slate_s, slate_v = colorsys.rgb_to_hsv(*SLATE)

    # Saturated blues -> slate blue-grey, keeping each swatch's light-to-dark gradient.
    blue = (h > 180) & (h < 265) & (s > 0.35)
    out[..., 0][blue] = slate_h
    out[..., 1][blue] = np.clip(s[blue] * 0.35 + 0.1, 0.15, 0.4)
    out[..., 2][blue] = np.clip(v[blue] * 0.7 + 0.08, 0.0, slate_v + 0.3)

    # Bright oranges and reds -> warm brown.
    warm = ((h < 48) | (h > 330)) & (s > 0.5) & ~blue
    out[..., 0][warm] = np.where(h[warm] > 330, 20 / 360, np.clip(h[warm], 18, 32) / 360)
    out[..., 1][warm] = s[warm] * 0.6
    out[..., 2][warm] = v[warm] * 0.78

    # Saturated yellows -> straw; saturated greens -> moss. Greys and woods (low saturation) stay.
    yellow = (h >= 48) & (h < 70) & (s > 0.5)
    out[..., 1][yellow] = s[yellow] * 0.65
    out[..., 2][yellow] = v[yellow] * 0.9
    green = (h >= 70) & (h <= 180) & (s > 0.55)
    out[..., 0][green] = np.clip(h[green], 85, 120) / 360
    out[..., 1][green] = s[green] * 0.55
    out[..., 2][green] = v[green] * 0.85

    result = np.concatenate([hsv_to_rgb(out), alpha], -1)
    Image.fromarray((np.clip(result, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(DST)
    print(f"wrote {DST.relative_to(ROOT)} ({img.size[0]}x{img.size[1]})")


if __name__ == "__main__":
    main()
