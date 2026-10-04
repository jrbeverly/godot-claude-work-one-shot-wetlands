#!/usr/bin/env python3
"""Paint the hand-painted, seamlessly tileable ground textures used by shaders/terrain.gdshader.

Style target: a 2004 hand-painted MMO ground tile. Soft overlapping brush dabs from a 3-5 tone
palette, light baked in from the top-left (each dab is a little dome: lighter on its upper-left
flank, darker on its lower-right, with a soft shadow pocket cast beside it), a low-frequency colour
drift to hide repetition, and no high-frequency grain or photographic detail. Everything is drawn on
a torus (coordinates wrap), so every tile is seamless by construction rather than by edge blending.

Usage: python3 tools/paint_textures.py [--size 512] [--only grass,rock] [--previews]
Outputs assets/textures/ground/<name>.png and, with --previews, screenshots/texture_preview_<name>.png
(2x2 tiled sheets) plus screenshots/texture_preview_sheet.png (all materials side by side).
Deterministic: every material has a fixed seed.
"""
from __future__ import annotations

import argparse
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
OUT_DIR = ROOT / "assets" / "textures" / "ground"
PREVIEW_DIR = ROOT / "screenshots"
LIGHT_DIR = np.array([-0.707, -0.707])  # texture-space direction TOWARDS the light (top-left)


def hex_rgb(h: str) -> np.ndarray:
    h = h.lstrip("#")
    return np.array([int(h[i : i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


class Canvas:
    """Float RGB canvas with wrap-around (toroidal) painting. All sizes are in pixels at 512; `k` rescales."""

    def __init__(self, size: int, base: np.ndarray, rng: np.random.Generator):
        self.n = size
        self.k = size / 512.0
        self.rng = rng
        self.img = np.ones((size, size, 3), dtype=np.float32) * base[None, None, :]

    # -- helpers ------------------------------------------------------------------------------
    def u(self, lo: float, hi: float) -> float:
        return float(self.rng.uniform(lo, hi))

    def px(self, lo: float, hi: float) -> float:
        """Uniform random size given in 512-px units, scaled to the canvas."""
        return float(self.rng.uniform(lo, hi)) * self.k

    def pos(self):
        return float(self.rng.uniform(0, self.n)), float(self.rng.uniform(0, self.n))

    def pick(self, palette, weights=None):
        return palette[self.rng.choice(len(palette), p=weights)]

    def vary(self, colour, val=0.06):
        """Slight per-dab value variation (and a whisper of hue) so no two dabs are identical.
        Kept small on purpose: random per-channel jitter reads as pink/teal confetti."""
        c = np.asarray(colour, dtype=np.float32) * (1.0 + self.rng.normal(0, val))
        return np.clip(c + self.rng.normal(0, 0.006, 3), 0, 1)

    # -- low-frequency tileable noise via a band-limited random spectrum (periodic by construction)
    def noise(self, cells: float, octaves: int = 3, falloff: float = 1.6) -> np.ndarray:
        n = self.n
        fy = np.fft.fftfreq(n)[:, None] * n
        fx = np.fft.fftfreq(n)[None, :] * n
        f = np.sqrt(fx * fx + fy * fy)
        spec = np.zeros((n, n), dtype=np.complex128)
        for o in range(octaves):
            cutoff = cells * (2**o)
            band = np.exp(-((f / cutoff) ** 2) * 2.0) / (falloff**o)
            phase = self.rng.uniform(0, 2 * np.pi, (n, n))
            spec += band * np.exp(1j * phase) * self.rng.normal(1.0, 0.3, (n, n))
        spec[0, 0] = 0
        out = np.real(np.fft.ifft2(spec))
        out /= max(1e-6, np.abs(out).max())
        return out.astype(np.float32)  # roughly -1..1, mean 0

    def tint_by_noise(self, cells: float, colour_a, colour_b, strength: float):
        """Low-frequency drift between two tones across the tile (hides repetition at distance)."""
        t = (self.noise(cells) * 0.5 + 0.5)[:, :, None]
        target = np.asarray(colour_a)[None, None, :] * (1 - t) + np.asarray(colour_b)[None, None, :] * t
        self.img = self.img * (1 - strength) + target * strength

    def shade_by_noise(self, cells: float, amount: float):
        """Big soft light/dark pools: painted ambient occlusion between clumps."""
        self.img *= 1.0 + self.noise(cells)[:, :, None] * amount

    def _dab_mask(self, cx, cy, rx, ry, angle, wobble):
        """Wrapped index arrays plus (h, px, py): h = 1 at centre -> 0 at the wobbly rim; p = offset / radius."""
        r = int(math.ceil(max(rx, ry) * (1.0 + wobble) + 2))
        icx, icy = int(round(cx)), int(round(cy))
        ys = (np.arange(-r, r + 1) + icy) % self.n
        xs = (np.arange(-r, r + 1) + icx) % self.n
        dy = np.arange(-r, r + 1)[:, None] + (icy - cy)
        dx = np.arange(-r, r + 1)[None, :] + (icx - cx)
        ca, sa = math.cos(angle), math.sin(angle)
        u = (dx * ca + dy * sa) / rx
        v = (-dx * sa + dy * ca) / ry
        th = np.arctan2(v, u)
        # Brush wobble: a few low harmonics make the outline blobby instead of geometric.
        ph = self.rng.uniform(0, 2 * np.pi, 3)
        edge = 1.0 + wobble * (0.6 * np.sin(2 * th + ph[0]) + 0.3 * np.sin(3 * th + ph[1]) + 0.2 * np.sin(5 * th + ph[2]))
        d = np.sqrt(u * u + v * v) / edge
        h = np.clip(1.0 - d, 0.0, 1.0).astype(np.float32)
        rr = max(rx, ry)
        return ys, xs, h, (dx / rr).astype(np.float32), (dy / rr).astype(np.float32)

    def dab(self, cx, cy, rx, ry, angle, colour, alpha=1.0, wobble=0.25, soft=0.6,
            light=0.12, shadow=0.0, shadow_offset=0.35, edge_dark=0.0):
        """One soft brush dab with baked lighting.

        soft:      width of the soft rim as a fraction of the radius (1 = fully soft dome, 0.2 = firm edge).
        light:     strength of the lit upper-left / shaded lower-right gradient across the dab.
        shadow:    alpha of a soft dark pocket cast to the lower-right before the dab is painted.
        edge_dark: darken the rim (reads as a painted outline on pebbles / leaves).
        """
        if shadow > 0:
            off = shadow_offset * max(rx, ry)
            sys_, sxs, sh, _, _ = self._dab_mask(cx - LIGHT_DIR[0] * off, cy - LIGHT_DIR[1] * off, rx * 1.1, ry * 1.1, angle, wobble)
            a = (self._alpha(sh, 1.0) * shadow)[:, :, None]
            region = self.img[np.ix_(sys_, sxs)]
            self.img[np.ix_(sys_, sxs)] = region * (1 - a) + region * 0.6 * a
        ys, xs, h, px, py = self._dab_mask(cx, cy, rx, ry, angle, wobble)
        shade = np.clip(px * LIGHT_DIR[0] + py * LIGHT_DIR[1], -1.0, 1.0)  # >0 on the side facing the light
        col = np.asarray(colour, dtype=np.float32)[None, None, :] * (1.0 + light * shade)[:, :, None]
        if edge_dark > 0:
            rim = np.clip(1.0 - h * 3.0, 0.0, 1.0) * (h > 0)
            col = col * (1.0 - edge_dark * rim)[:, :, None]
        a = (self._alpha(h, soft) * alpha)[:, :, None]
        region = self.img[np.ix_(ys, xs)]
        self.img[np.ix_(ys, xs)] = region * (1 - a) + col * a

    @staticmethod
    def _alpha(h, soft):
        a = np.clip(h / max(1e-3, soft), 0.0, 1.0)
        return a * a * (3.0 - 2.0 * a)  # smoothstep rim

    def stroke(self, x0, y0, x1, y1, width, colour, alpha=1.0, light=0.1, wobble=0.15, jitter=0.0, soft=0.8):
        """A soft stroke made of overlapping round dabs along a gently wandering line."""
        length = math.hypot(x1 - x0, y1 - y0)
        steps = max(2, int(length / (width * 0.3)))
        nx, ny = -(y1 - y0) / max(1e-6, length), (x1 - x0) / max(1e-6, length)
        wob = self.noise_1d(steps) * jitter * width
        for i in range(steps):
            t = i / (steps - 1)
            self.dab(x0 + (x1 - x0) * t + nx * wob[i], y0 + (y1 - y0) * t + ny * wob[i],
                     width * 0.5, width * 0.5, 0.0, colour, alpha, wobble, soft, light)

    def noise_1d(self, steps: int) -> np.ndarray:
        ph = self.rng.uniform(0, 2 * np.pi, 2)
        t = np.linspace(0, 1, steps)
        return np.sin(t * 2 * np.pi + ph[0]) * 0.6 + np.sin(t * 4 * np.pi + ph[1]) * 0.4

    def underpaint(self, palette, weights, count, rx, ry, alpha=0.5, light=0.1, soft=0.9, wobble=0.3):
        """Big, very soft overlapping dabs: the mottled 'turf' every material sits on."""
        for _ in range(count):
            x, y = self.pos()
            self.dab(x, y, self.px(*rx), self.px(*ry), self.u(0, math.pi), self.vary(self.pick(palette, weights), 0.04),
                     alpha=alpha, wobble=wobble, soft=soft, light=light)

    def mute(self, desaturate: float, value: float = 1.0):
        """Pull saturation toward luminance and scale value. The terrain is lit by sun + sky on top of
        this albedo, so the tile itself must stay a notch duller than the colour we want to see."""
        lum = (self.img * np.array([0.3, 0.59, 0.11], dtype=np.float32)[None, None, :]).sum(2, keepdims=True)
        self.img = (self.img * (1 - desaturate) + lum * desaturate) * value

    def soften(self, radius: float):
        """Final soft blur; tileable because we blur a 3x3 wrap of the tile and crop the centre."""
        if radius <= 0:
            return
        n = self.n
        big = np.tile(self.img, (3, 3, 1))
        im = Image.fromarray(to_u8(big)).filter(ImageFilter.GaussianBlur(radius * self.k))
        self.img = (np.asarray(im).astype(np.float32) / 255.0)[n : 2 * n, n : 2 * n]


def to_u8(img: np.ndarray) -> np.ndarray:
    return (np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)


# ----------------------------------------------------------------------------- materials
# Counts are per 512x512 tile and scale with area.

def paint_grass(c: Canvas):
    area = c.k * c.k
    base, light, deep, warm = hex_rgb("5E8C3A"), hex_rgb("8DB04A"), hex_rgb("3F6B2E"), hex_rgb("7F9A3C")
    mid = (base + light) * 0.5
    c.tint_by_noise(1.5, base * 0.92, warm, 0.6)
    c.underpaint([base, mid, deep, warm], [0.4, 0.2, 0.2, 0.2], int(300 * area), (24, 40), (18, 30), alpha=0.6, light=0.12)
    # Deep pockets where turf clumps meet.
    for _ in range(int(30 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(26, 46), c.px(18, 30), c.u(0, math.pi), c.vary(deep, 0.04), alpha=0.55, wobble=0.35, soft=1.0, light=0.04)
    # Turf clumps: fat, slightly upright blobs with baked light and a shadow pocket. Mostly opaque so
    # they read as distinct painted blobs with clear value steps rather than translucent smears.
    for _ in range(int(130 * area)):
        x, y = c.pos()
        col = c.pick([base, mid, warm, light, deep], [0.34, 0.26, 0.16, 0.12, 0.12])
        c.dab(x, y, c.px(11, 19), c.px(15, 26), math.pi / 2 + c.rng.normal(0, 0.5), c.vary(col, 0.06),
              alpha=0.86, wobble=0.3, soft=0.55, light=0.3, shadow=0.35, shadow_offset=0.3)
    # A few bright tufts: three lit blades fanning from one root.
    for _ in range(int(26 * area)):
        x, y = c.pos()
        for j in range(3):
            ang = math.pi / 2 + (j - 1) * 0.35 + c.rng.normal(0, 0.1)
            c.dab(x + (j - 1) * 4 * c.k, y - 5 * c.k, c.px(3.5, 5), c.px(10, 16), ang, c.vary(light * 1.08, 0.04),
                  alpha=0.8, wobble=0.15, soft=0.7, light=0.2)
    c.shade_by_noise(2.5, 0.10)
    c.mute(0.14, 0.94)
    c.soften(0.7)


def paint_meadow(c: Canvas):
    area = c.k * c.k
    base, straw, deep, green = hex_rgb("8FA048"), hex_rgb("B8A050"), hex_rgb("6B7A36"), hex_rgb("7E9A42")
    c.tint_by_noise(1.5, base * 0.95, straw * 0.95, 0.6)
    c.underpaint([base, straw, deep, green], [0.35, 0.3, 0.15, 0.2], int(300 * area), (24, 42), (16, 28), alpha=0.6, light=0.12)
    for _ in range(int(28 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(28, 48), c.px(18, 30), c.u(0, math.pi), c.vary(deep, 0.04), alpha=0.5, wobble=0.35, soft=1.0, light=0.04)
    # Dry grass leaning the same general way, like wind has combed it.
    lean = c.u(-0.3, 0.3)
    for _ in range(int(150 * area)):
        x, y = c.pos()
        col = c.pick([base, straw, green, deep], [0.36, 0.32, 0.2, 0.12])
        c.dab(x, y, c.px(8, 14), c.px(15, 26), math.pi / 2 + lean + c.rng.normal(0, 0.35), c.vary(col, 0.06),
              alpha=0.86, wobble=0.3, soft=0.55, light=0.28, shadow=0.3, shadow_offset=0.3)
    for _ in range(int(22 * area)):
        x, y = c.pos()
        for j in range(3):
            ang = math.pi / 2 + lean + (j - 1) * 0.3 + c.rng.normal(0, 0.1)
            c.dab(x + (j - 1) * 4 * c.k, y - 5 * c.k, c.px(3, 4.5), c.px(10, 16), ang, c.vary(straw * 1.12, 0.04),
                  alpha=0.8, wobble=0.15, soft=0.7, light=0.2)
    # Sparse tiny flower specks in little groups (white, butter yellow, a touch of pink).
    flowers = [hex_rgb("F2EEDC"), hex_rgb("E9D36A"), hex_rgb("D98FA0"), hex_rgb("F0E6B0")]
    for _ in range(int(7 * area)):
        gx, gy = c.pos()
        col = c.pick(flowers, [0.4, 0.35, 0.1, 0.15])
        for _ in range(c.rng.integers(2, 5)):
            r = c.px(1.8, 2.8)
            c.dab(gx + c.rng.normal(0, 8) * c.k, gy + c.rng.normal(0, 8) * c.k, r, r, 0, c.vary(col, 0.04),
                  alpha=0.9, wobble=0.2, soft=0.7, light=0.15, shadow=0.35, shadow_offset=0.5)
    c.shade_by_noise(2.5, 0.09)
    c.mute(0.12, 0.95)
    c.soften(0.7)


def paint_forest_floor(c: Canvas):
    area = c.k * c.k
    base, litter, litter_l, moss, root = hex_rgb("4A5630"), hex_rgb("6B5A3A"), hex_rgb("7E6B44"), hex_rgb("57683A"), hex_rgb("343622")
    soil = hex_rgb("4F4A32")
    c.tint_by_noise(1.5, base * 0.9, litter * 0.85, 0.55)
    c.underpaint([base, moss, soil, litter * 0.9], [0.4, 0.25, 0.2, 0.15], int(420 * area), (16, 30), (12, 22), alpha=0.55, light=0.12)
    # Root shadows: broad, soft, dark meanders under everything (a few, not a web).
    for _ in range(3):
        x0, y0 = c.pos()
        ang = c.u(0, 2 * math.pi)
        L = c.u(0.3, 0.5) * c.n
        c.stroke(x0, y0, x0 + math.cos(ang) * L, y0 + math.sin(ang) * L, c.px(30, 44), root, alpha=0.28, light=0.0, wobble=0.3, jitter=1.4, soft=1.0)
    # Moss cushions: lit green blobs.
    for _ in range(int(60 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(10, 18), c.px(8, 14), c.u(0, math.pi), c.vary(moss, 0.06), alpha=0.7, wobble=0.35, soft=0.7, light=0.25, shadow=0.25)
    # Leaf litter: ochre ovals in loose drifts, lit on one side with a soft outline.
    for _ in range(int(18 * area)):
        gx, gy = c.pos()
        # A soft soil patch under the drift so the leaves sit in something.
        c.dab(gx, gy, c.px(30, 50), c.px(20, 34), c.u(0, math.pi), c.vary(soil * 1.1, 0.04), alpha=0.55, wobble=0.35, soft=1.0, light=0.06)
        for _ in range(c.rng.integers(8, 16)):
            col = c.pick([litter, litter_l, hex_rgb("5E5236")], [0.45, 0.3, 0.25])
            c.dab(gx + c.rng.normal(0, 18) * c.k, gy + c.rng.normal(0, 13) * c.k, c.px(8, 13), c.px(5, 7.5), c.u(0, math.pi), c.vary(col, 0.07),
                  alpha=0.8, wobble=0.25, soft=0.6, light=0.3, shadow=0.3, shadow_offset=0.4, edge_dark=0.12)
    c.shade_by_noise(2.0, 0.12)
    c.soften(0.7)


def paint_dirt_road(c: Canvas):
    area = c.k * c.k
    base, worn, dark, pebble = hex_rgb("B8955E"), hex_rgb("CBAA72"), hex_rgb("9A7A4C"), hex_rgb("A8927A")
    c.tint_by_noise(1.5, base * 0.96, worn, 0.6)
    c.underpaint([base, worn, dark * 1.05], [0.45, 0.35, 0.2], int(380 * area), (22, 40), (16, 30), alpha=0.5, light=0.08, soft=1.0, wobble=0.25)
    # Worn, lighter pools (the trodden centre) and soft darker pockets around them.
    for _ in range(int(10 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(70, 120), c.px(40, 70), c.u(0, math.pi), c.vary(worn * 1.04, 0.02), alpha=0.4, wobble=0.25, soft=1.0, light=0.03)
    for _ in range(int(30 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(14, 26), c.px(10, 18), c.u(0, math.pi), c.vary(dark, 0.04), alpha=0.35, wobble=0.35, soft=1.0, light=0.06)
    # Faint ruts: two long soft darker grooves with a lit lip on the light side.
    x0, y0 = c.pos()
    ang = c.u(0, 2 * math.pi)
    L = 0.5 * c.n
    x1, y1 = x0 + math.cos(ang) * L, y0 + math.sin(ang) * L
    c.stroke(x0, y0, x1, y1, c.px(26, 30), dark, alpha=0.14, light=0.03, jitter=0.4, soft=1.0)
    c.stroke(x0 - 10 * c.k, y0 - 10 * c.k, x1 - 10 * c.k, y1 - 10 * c.k, c.px(12, 14), worn * 1.06, alpha=0.14, light=0.03, jitter=0.4, soft=1.0)
    # Soft pebbles: fat lit domes with a soft darker rim and a shadow pocket, in small scatters.
    for _ in range(int(18 * area)):
        gx, gy = c.pos()
        for _ in range(c.rng.integers(1, 3)):
            # Pebble tones sit close to the dirt value: only the lit top and the shadow pocket make them read.
            col = c.pick([pebble, hex_rgb("B39C80"), hex_rgb("9C8A74")], [0.4, 0.35, 0.25])
            r = c.px(8, 14)
            c.dab(gx + c.rng.normal(0, 22) * c.k, gy + c.rng.normal(0, 18) * c.k, r, r * c.u(0.65, 0.95), c.u(0, math.pi), c.vary(col, 0.05),
                  alpha=0.8, wobble=0.25, soft=0.6, light=0.3, shadow=0.3, shadow_offset=0.4, edge_dark=0.1)
    c.shade_by_noise(2.5, 0.07)
    c.mute(0.08, 0.97)
    c.soften(0.8)


def paint_rock(c: Canvas):
    area = c.k * c.k
    light, mid, dark, crack = hex_rgb("8A7A6A"), hex_rgb("7A6D5F"), hex_rgb("6F6358"), hex_rgb("4E463E")
    top = hex_rgb("A09080")
    c.tint_by_noise(1.5, mid * 0.95, light, 0.6)
    c.underpaint([light, mid, dark], [0.3, 0.4, 0.3], int(200 * area), (30, 60), (12, 22), alpha=0.5, light=0.08, soft=1.0, wobble=0.2)
    # Strata: rows of horizontal-ish facet slabs, each lit along its top edge with a strong shadow under its lip.
    rows = 9
    band_h = c.n / rows
    for row in range(rows):
        y_base = row * band_h
        x = c.u(0, c.n)
        tilt = c.rng.normal(0, 0.05)
        while x < c.n + band_h:
            w = c.px(90, 200)
            h = band_h * c.u(0.8, 1.1)
            cx, cy = x + w * 0.5, y_base + band_h * 0.5 + c.rng.normal(0, 3) * c.k
            col = c.pick([light, mid, dark], [0.35, 0.4, 0.25])
            c.dab(cx, cy, w * 0.5, h * 0.5, tilt + c.rng.normal(0, 0.03), c.vary(col, 0.05),
                  alpha=0.92, wobble=0.1, soft=0.3, light=0.2, shadow=0.65, shadow_offset=0.3)
            c.dab(cx - w * 0.03, cy - h * 0.34, w * 0.42, h * 0.12, tilt, c.vary(top, 0.03), alpha=0.6, wobble=0.2, soft=0.8, light=0.08)
            x += w * c.u(0.7, 0.9)  # overlap so the band reads as one continuous ledge
    # Fissures: a few short, broad, soft dark pockets (vertical joints breaking the bands).
    for _ in range(int(16 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(5, 8), c.px(14, 30), math.pi / 2 + c.rng.normal(0, 0.15), crack, alpha=0.5, wobble=0.3, soft=1.0, light=0.0)
    # Cool/warm drift so cliffs do not read as one flat grey.
    c.tint_by_noise(2.0, hex_rgb("786A5E"), hex_rgb("8A8076"), 0.2)
    c.shade_by_noise(2.0, 0.12)
    c.soften(0.7)


def paint_snow(c: Canvas):
    area = c.k * c.k
    base, hollow, bright = hex_rgb("E8EEF4"), hex_rgb("B6C6DA"), hex_rgb("F6F8FB")
    c.tint_by_noise(1.5, base, (base + hollow) * 0.5, 0.5)
    for _ in range(int(22 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(40, 80), c.px(22, 40), c.u(0, math.pi), c.vary(hollow, 0.02), alpha=0.5, wobble=0.35, soft=1.0, light=0.04)
    for _ in range(int(44 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(30, 60), c.px(16, 30), c.u(0, math.pi), c.vary(base, 0.015), alpha=0.7, wobble=0.3, soft=0.9, light=0.09, shadow=0.2, shadow_offset=0.3)
    for _ in range(int(10 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(16, 30), c.px(8, 14), c.u(0, math.pi), bright, alpha=0.45, wobble=0.3, soft=1.0, light=0.05)
    c.shade_by_noise(2.0, 0.04)
    c.soften(1.8)


def paint_mud(c: Canvas):
    area = c.k * c.k
    base, silt, dark, wet = hex_rgb("5E5A48"), hex_rgb("7A735A"), hex_rgb("46442F"), hex_rgb("51523E")
    c.tint_by_noise(1.5, base * 0.95, wet, 0.6)
    c.underpaint([base, wet, dark, silt * 0.9], [0.4, 0.25, 0.2, 0.15], int(380 * area), (22, 40), (14, 26), alpha=0.5, light=0.1, soft=1.0)
    # Silt streaks: a handful of long soft strokes laid along the current, each with a lit lip.
    flow = c.u(0, math.pi)
    for _ in range(int(7 * area)):
        x0, y0 = c.pos()
        ang = flow + c.rng.normal(0, 0.15)
        L = c.u(0.35, 0.7) * c.n
        w = c.px(34, 56)
        x1, y1 = x0 + math.cos(ang) * L, y0 + math.sin(ang) * L
        c.stroke(x0, y0, x1, y1, w, c.vary(silt, 0.05), alpha=0.26, light=0.08, jitter=0.6, soft=1.0)
        c.stroke(x0 - 6 * c.k, y0 - 6 * c.k, x1 - 6 * c.k, y1 - 6 * c.k, w * 0.35, c.vary(silt * 1.08, 0.04), alpha=0.18, light=0.08, jitter=0.6, soft=1.0)
    # Dark wet pits and a few soft pebbles.
    for _ in range(int(30 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(14, 26), c.px(10, 18), c.u(0, math.pi), c.vary(dark, 0.04), alpha=0.4, wobble=0.35, soft=1.0, light=0.06)
    for _ in range(int(14 * area)):
        x, y = c.pos()
        r = c.px(4, 7)
        c.dab(x, y, r, r * 0.8, c.u(0, math.pi), c.vary(silt * 0.95, 0.05), alpha=0.85, wobble=0.25, soft=0.6, light=0.3, shadow=0.4, shadow_offset=0.45, edge_dark=0.2)
    c.shade_by_noise(2.0, 0.08)
    c.soften(0.9)


MATERIALS = {
    # name: (seed, base colour, painter)
    "grass": (101, "5E8C3A", paint_grass),
    "meadow": (202, "8FA048", paint_meadow),
    "forest_floor": (303, "4A5630", paint_forest_floor),
    "dirt_road": (404, "B8955E", paint_dirt_road),
    "rock": (505, "7A6D5F", paint_rock),
    "snow": (606, "E8EEF4", paint_snow),
    "mud": (707, "5E5A48", paint_mud),
}


def paint(name: str, size: int) -> np.ndarray:
    seed, base, fn = MATERIALS[name]
    c = Canvas(size, hex_rgb(base), np.random.default_rng(seed))
    fn(c)
    return to_u8(c.img)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--size", type=int, default=512)
    ap.add_argument("--only", type=str, default="", help="comma-separated material names")
    ap.add_argument("--previews", action="store_true", help="also write 2x2 tiled preview sheets to screenshots/")
    args = ap.parse_args()
    names = [s for s in args.only.split(",") if s] or list(MATERIALS)
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    tiles = {}
    for name in names:
        img = paint(name, args.size)
        Image.fromarray(img).save(OUT_DIR / f"{name}.png", optimize=True)
        tiles[name] = img
        print(f"wrote {OUT_DIR / (name + '.png')}  mean rgb={img.reshape(-1, 3).mean(0).round(1)}")
    if args.previews:
        PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
        for name, img in tiles.items():
            Image.fromarray(np.tile(img, (2, 2, 1))).save(PREVIEW_DIR / f"texture_preview_{name}.png")
        # Contact sheet: each material once at half size, in a row, to compare value/saturation.
        half = args.size // 2
        row = np.concatenate([np.asarray(Image.fromarray(t).resize((half, half), Image.LANCZOS)) for t in tiles.values()], axis=1)
        Image.fromarray(row).save(PREVIEW_DIR / "texture_preview_sheet.png")
        print(f"wrote previews to {PREVIEW_DIR}")


if __name__ == "__main__":
    main()
