#!/usr/bin/env python3
"""Paint the alpha-cut foliage cards and bark used by the procedural moss trees and reed clumps
(src/World/MossTree.cs). Same hand-painted 2004 language as tools/paint_textures.py: soft brush dabs
from a 3-5 tone palette, light baked in from the top-left, no photographic detail.

Usage: python3 tools/paint_foliage.py [--only moss_curtain,reeds] [--previews]
Outputs assets/textures/foliage/<name>.png. Deterministic (fixed seed per texture).

  moss_curtain  256x512 RGBA  hanging strands of moss/leaves (card hangs from the canopy rim)
  leaf_clump    512x512 RGBA  one blob of canopy foliage (flat umbrella cards and the skirt)
  reeds         256x512 RGBA  a fan of tall blades with straw tips and a few cattail heads
  bark_pale     256x512 RGB   pale tan-grey bark, tileable both ways (trunk cylinder wraps in U)
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from paint_textures import Canvas, hex_rgb, to_u8  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "assets" / "textures" / "foliage"
PREVIEW_DIR = ROOT / "screenshots"
LIGHT = np.array([-0.707, -0.707])


class AlphaCanvas:
    """Non-wrapping RGBA canvas: colour painted premultiplied-free, alpha accumulated by coverage."""

    def __init__(self, w: int, h: int, rng: np.random.Generator):
        self.w, self.h, self.rng = w, h, rng
        self.img = np.zeros((h, w, 3), dtype=np.float32)
        self.alpha = np.zeros((h, w), dtype=np.float32)

    def u(self, lo, hi):
        return float(self.rng.uniform(lo, hi))

    def vary(self, colour, val=0.06):
        c = np.asarray(colour, dtype=np.float32) * (1.0 + self.rng.normal(0, val))
        return np.clip(c + self.rng.normal(0, 0.005, 3), 0, 1)

    def dab(self, cx, cy, rx, ry, angle, colour, alpha=1.0, wobble=0.25, soft=0.6, light=0.15, edge_dark=0.0):
        r = int(math.ceil(max(rx, ry) * (1.0 + wobble) + 2))
        x0, x1 = max(0, int(cx) - r), min(self.w, int(cx) + r + 1)
        y0, y1 = max(0, int(cy) - r), min(self.h, int(cy) + r + 1)
        if x1 <= x0 or y1 <= y0:
            return
        dy = (np.arange(y0, y1)[:, None] - cy)
        dx = (np.arange(x0, x1)[None, :] - cx)
        ca, sa = math.cos(angle), math.sin(angle)
        u = (dx * ca + dy * sa) / rx
        v = (-dx * sa + dy * ca) / ry
        th = np.arctan2(v, u)
        ph = self.rng.uniform(0, 2 * np.pi, 3)
        edge = 1.0 + wobble * (0.6 * np.sin(2 * th + ph[0]) + 0.3 * np.sin(3 * th + ph[1]) + 0.2 * np.sin(5 * th + ph[2]))
        d = np.sqrt(u * u + v * v) / edge
        h = np.clip(1.0 - d, 0.0, 1.0)
        a = np.clip(h / max(1e-3, soft), 0, 1)
        a = a * a * (3 - 2 * a) * alpha
        rr = max(rx, ry)
        shade = np.clip((dx / rr) * LIGHT[0] + (dy / rr) * LIGHT[1], -1, 1)
        col = np.asarray(colour, dtype=np.float32)[None, None, :] * (1.0 + light * shade)[:, :, None]
        if edge_dark > 0:
            rim = np.clip(1.0 - h * 3.0, 0, 1) * (h > 0)
            col = col * (1.0 - edge_dark * rim)[:, :, None]
        region = self.img[y0:y1, x0:x1]
        aa = a[:, :, None]
        self.img[y0:y1, x0:x1] = region * (1 - aa) + col * aa
        self.alpha[y0:y1, x0:x1] = 1 - (1 - self.alpha[y0:y1, x0:x1]) * (1 - a)

    def stroke(self, pts, widths, colours, alpha=1.0, light=0.15, soft=0.7, wobble=0.15):
        """Dabs along a polyline; widths/colours interpolate along it."""
        n = len(pts)
        for i in range(n - 1):
            (xa, ya), (xb, yb) = pts[i], pts[i + 1]
            seg = math.hypot(xb - xa, yb - ya)
            wa, wb = widths[i], widths[i + 1]
            steps = max(1, int(seg / max(0.6, min(wa, wb) * 0.35)))
            for s in range(steps):
                t = s / steps
                tt = (i + t) / (n - 1)
                w = wa * (1 - t) + wb * t
                col = np.asarray(colours[i]) * (1 - t) + np.asarray(colours[i + 1]) * t
                self.dab(xa + (xb - xa) * t, ya + (yb - ya) * t, w * 0.5, w * 0.5, 0.0, col, alpha, wobble, soft, light)

    def finish(self, blur=0.6, alpha_gamma=1.0):
        """Soft blur of colour and alpha; the alpha is then re-sharpened a little so the scissor edge reads painted, not fuzzy."""
        rgba = np.dstack([self.img, self.alpha[:, :, None]])
        im = Image.fromarray(to_u8(rgba), "RGBA").filter(ImageFilter.GaussianBlur(blur))
        arr = np.asarray(im).astype(np.float32) / 255.0
        rgb, a = arr[:, :, :3], arr[:, :, 3]
        # Bleed colour into transparent texels so mipmaps/filtering never show dark fringes.
        dil = Image.fromarray(to_u8(rgba[:, :, :3])).filter(ImageFilter.MaxFilter(9))
        dil = np.asarray(dil).astype(np.float32) / 255.0
        rgb = np.where(a[:, :, None] > 0.15, rgb, dil)
        a = np.clip(a, 0, 1) ** alpha_gamma
        return np.dstack([rgb, a[:, :, None]])


def lerp3(a, b, t):
    return np.asarray(a) * (1 - t) + np.asarray(b) * t


# ------------------------------------------------------------------------------------- textures

def paint_moss_curtain(rng):
    """Hanging moss: dense at the top (the canopy underside it hangs from), strands loosening and paling toward
    ragged tips. Olive -> yellow-green -> straw, like the willow curtains in the Wetlands references."""
    W, H = 256, 512
    c = AlphaCanvas(W, H, rng)
    # Khaki-olive, not lime: R1 curtains sit at the canopy's value and only the tips go straw.
    deep, olive, yel, straw = hex_rgb("444826"), hex_rgb("62643A"), hex_rgb("8E8A4C"), hex_rgb("BCAE7C")
    # Top mass: fat leaf dabs across the top fifth (keeps the card attached to the canopy visually).
    for _ in range(140):
        x, y = c.u(W * 0.08, W * 0.92), c.u(0, H * 0.16)
        col = [deep, olive, yel][rng.choice(3, p=[0.35, 0.45, 0.2])]
        c.dab(x, y, c.u(12, 22), c.u(7, 12), c.u(-0.5, 0.5), c.vary(col), 0.95, 0.3, 0.6, 0.25)
    # Strands: each a tapered, slightly wandering stroke with small leaf dabs budding off it.
    n_strands = 26
    xs = np.sort(rng.uniform(W * 0.12, W * 0.88, n_strands))
    for i, x0 in enumerate(xs):
        y0 = c.u(H * 0.05, H * 0.18)
        roll = c.u(0, 1)
        length = H * (c.u(0.30, 0.50) if roll < 0.3 else c.u(0.50, 0.80) if roll < 0.8 else c.u(0.85, 1.0))
        wscale = c.u(0.6, 1.4)
        pts, widths, cols = [], [], []
        k = 7
        drift = c.u(-14, 14)
        for j in range(k):
            t = j / (k - 1)
            x = min(max(x0 + drift * t + math.sin(t * 3.1 + i) * 5.0, W * 0.06), W * 0.94)
            y = y0 + length * t
            pts.append((x, y))
            widths.append((30.0 * (1 - t) + 7.0 * t) * wscale)
            if t < 0.45:
                cols.append(lerp3(olive, deep, c.u(0, 0.5)))
            elif t < 0.8:
                cols.append(lerp3(olive, yel, (t - 0.45) / 0.35))
            else:
                cols.append(lerp3(yel, straw, (t - 0.8) / 0.2))
        c.stroke(pts, widths, cols, alpha=0.95, light=0.18, soft=0.8, wobble=0.2)
        # Leaf buds along the strand at irregular spacing and size (no strings of beads).
        y = y0
        while y < y0 + length:
            t = (y - y0) / length
            x = min(max(x0 + drift * t + math.sin(t * 3.1 + i) * 5.0 + c.u(-7, 7), W * 0.06), W * 0.94)
            col = lerp3(olive, straw, t * 0.8)
            r = c.u(5, 16)
            c.dab(x, y, r, r * c.u(0.45, 0.7), c.u(-0.9, 0.9), c.vary(col), 0.85, 0.3, 0.55, 0.28, edge_dark=0.12)
            y += c.u(8, 22)
    # Thin straw hairs in the lower half so the hem frays instead of ending flat.
    for _ in range(30):
        x0, y0 = c.u(W * 0.1, W * 0.9), c.u(H * 0.45, H * 0.8)
        c.stroke([(x0, y0), (x0 + c.u(-6, 6), y0 + c.u(40, 110))], [3.0, 1.0], [straw, straw], alpha=0.8, light=0.1, soft=0.9, wobble=0.1)
    return c.finish(0.5, 1.1)


def paint_leaf_clump(rng):
    """One blob of canopy: ragged outline, leaves lit from the top-left with deep pockets lower-right."""
    S = 512
    c = AlphaCanvas(S, S, rng)
    # Khaki-olive-brown canopy (R1: #56472E tops, #8C7A3C highlights), lobed outline so cards never read as discs.
    deep, olive, mid, light, straw = hex_rgb("3A3A22"), hex_rgb("575630"), hex_rgb("7A7238"), hex_rgb("9C9048"), hex_rgb("BBA868")
    cx, cy = S * 0.5, S * 0.52
    phi, psi = c.u(0, 6.28), c.u(0, 6.28)
    def lobe(ang):
        return 1 + 0.22 * math.sin(3 * ang + phi) + 0.12 * math.sin(5 * ang + psi)
    # Underlying mass: big soft dabs inside an irregular lobed disc.
    for _ in range(220):
        ang = c.u(0, 2 * math.pi)
        rad = c.u(0, 1) ** 0.6 * S * 0.36 * lobe(ang)
        x, y = cx + math.cos(ang) * rad, cy + math.sin(ang) * rad * 0.9
        shade = (x - cx) * LIGHT[0] + (y - cy) * LIGHT[1]   # positive toward the light
        base = [deep, olive, mid][rng.choice(3, p=[0.4, 0.4, 0.2])]
        if shade > S * 0.08:
            base = [mid, light][rng.choice(2)]
        c.dab(x, y, c.u(22, 40), c.u(14, 24), c.u(0, math.pi), c.vary(base, 0.05), 0.9, 0.35, 0.7, 0.2)
    # Leaf dabs on top: smaller, firmer, with a painted outline; lit side lighter.
    for _ in range(220):
        ang = c.u(0, 2 * math.pi)
        rad = c.u(0, 1) ** 0.55 * S * 0.39 * lobe(ang)
        x, y = cx + math.cos(ang) * rad, cy + math.sin(ang) * rad * 0.9
        shade = ((x - cx) * LIGHT[0] + (y - cy) * LIGHT[1]) / (S * 0.4)
        p = np.clip(np.array([0.35, 0.3, 0.2, 0.1, 0.05]) + np.array([-0.3, -0.1, 0.1, 0.2, 0.1]) * shade, 0.01, 1)
        base = [deep, olive, mid, light, straw][rng.choice(5, p=p / p.sum())]
        c.dab(x, y, c.u(18, 34), c.u(10, 18), c.u(0, math.pi), c.vary(base, 0.06), 0.92, 0.3, 0.55, 0.45, edge_dark=0.28)
    # Warm highlight glaze on the top-left, blue-ish shade glaze lower-right (baked light).
    yy, xx = np.mgrid[0:S, 0:S]
    g = ((xx - cx) * LIGHT[0] + (yy - cy) * LIGHT[1]) / (S * 0.45)
    c.img *= (1.0 + 0.16 * np.clip(g, -1, 1))[:, :, None]
    c.img = c.img * (1 - 0.12 * np.clip(-g, 0, 1))[:, :, None] + np.array([0.0, 0.01, 0.04])[None, None, :] * np.clip(-g, 0, 1)[:, :, None] * 0.5
    return c.finish(0.5, 0.65)


def paint_reeds(rng):
    """Reed clump card: blades fan up from the bottom centre, olive at the root to straw at the tips."""
    W, H = 256, 512
    c = AlphaCanvas(W, H, rng)
    root, olive, yel, straw, head = hex_rgb("4A5430"), hex_rgb("5E6C3A"), hex_rgb("7E8A4C"), hex_rgb("A8A066"), hex_rgb("5A3E2C")
    cx = W * 0.5
    n = 70
    for i in range(n):
        lean = c.u(-0.2, 0.2)
        short = i % 4 == 0
        top = H * (c.u(0.45, 0.6) if short else c.u(0.04, 0.42))
        bx = cx + c.u(-70, 70)
        pts, widths, cols = [], [], []
        k = 6
        for j in range(k):
            t = j / (k - 1)
            y = H - (H - top) * t
            x = bx + lean * (H - y) + math.sin(t * 2.2 + i) * 4.0 * t
            pts.append((x, y))
            widths.append(14.0 * (1 - t) + 4.0 * t)
            cols.append(lerp3(lerp3(root, olive, min(1, t * 1.6)), straw if i % 3 else yel, max(0, t - 0.35) / 0.65))
        c.stroke(pts, widths, cols, alpha=0.95, light=0.2, soft=0.75, wobble=0.12)
        if i % 6 == 1 and not short:
            # Cattail head on a straight stem with a straw spike above it.
            x, y = pts[-1]
            c.stroke([(x, y + 40), (x, y - 30)], [4.0, 2.0], [olive, straw], alpha=0.95, light=0.1, soft=0.8, wobble=0.05)
            c.dab(x, y + 16, 5, 22, 0.0, c.vary(head, 0.05), 1.0, 0.1, 0.5, 0.25)
    # Dark root mass so the clump sits in the ground (the bottom tenth is near-opaque).
    for _ in range(70):
        c.dab(cx + c.u(-80, 80), H - c.u(0, 60), c.u(10, 20), c.u(6, 11), c.u(-0.4, 0.4), c.vary(root, 0.05), 0.9, 0.3, 0.8, 0.1)
    return c.finish(0.5, 1.1)


def paint_bark_pale(rng):
    """Pale tan-grey bark (the willow trunks in the references are almost sand-coloured at dusk). Torus canvas."""
    c = Canvas(256, hex_rgb("8C7E6A"), rng)
    c.k = 0.5
    light, mid, dark, crack = hex_rgb("B8A88C"), hex_rgb("968670"), hex_rgb("706252"), hex_rgb("4E4538")
    c.tint_by_noise(1.5, mid * 0.95, light, 0.5)
    # Vertical ridges: long thin strokes wandering up the tile.
    for _ in range(34):
        x = c.u(0, 256)
        y0 = c.u(0, 256)
        col = [light, mid, dark][rng.choice(3, p=[0.4, 0.35, 0.25])]
        c.stroke(x, y0, x + c.u(-8, 8), y0 + c.u(160, 320), c.u(10, 22), col, alpha=0.45, light=0.1, wobble=0.15, jitter=0.35, soft=1.0)
    for _ in range(7):
        x, y = c.pos()
        c.dab(x, y, c.u(1.2, 2.2), c.u(30, 80), c.rng.normal(0, 0.06), crack, alpha=0.35, wobble=0.3, soft=0.7, light=0.0)
    for _ in range(40):
        x, y = c.pos()
        c.dab(x, y, c.u(6, 12), c.u(3, 6), c.u(0, math.pi), c.vary(light, 0.04), alpha=0.35, wobble=0.3, soft=0.9, light=0.2)
    c.shade_by_noise(2.0, 0.08)
    c.soften(0.6)
    # Stack two tiles vertically so the trunk UV (v = height / 4 m) repeats less obviously.
    tile = c.img
    return np.concatenate([tile, tile], axis=0)


TEXTURES = {
    "moss_curtain": (11, paint_moss_curtain),
    "leaf_clump": (22, paint_leaf_clump),
    "reeds": (33, paint_reeds),
    "bark_pale": (44, paint_bark_pale),
}


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--only", default="")
    ap.add_argument("--previews", action="store_true")
    args = ap.parse_args()
    names = [s for s in args.only.split(",") if s] or list(TEXTURES)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for name in names:
        seed, fn = TEXTURES[name]
        arr = fn(np.random.default_rng(seed))
        mode = "RGBA" if arr.shape[2] == 4 else "RGB"
        im = Image.fromarray(to_u8(arr), mode)
        im.save(OUT_DIR / f"{name}.png", optimize=True)
        cov = f" alpha coverage={arr[:, :, 3].mean():.2f}" if mode == "RGBA" else ""
        print(f"wrote {OUT_DIR / (name + '.png')} {im.size}{cov}")
        if args.previews:
            PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
            bg = Image.new("RGBA", im.size, (120, 100, 140, 255))
            bg.alpha_composite(im) if mode == "RGBA" else bg.paste(im)
            bg.convert("RGB").save(PREVIEW_DIR / f"foliage_preview_{name}.png")


if __name__ == "__main__":
    main()
