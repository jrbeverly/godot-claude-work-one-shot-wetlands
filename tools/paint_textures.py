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

    def _dab_mask(self, cx, cy, rx, ry, angle, wobble, power=2.0):
        """Wrapped index arrays plus (h, px, py): h = 1 at centre -> 0 at the wobbly rim; p = offset / radius.
        power: superellipse exponent. 2 = ellipse (soft blob); 4-6 = rounded slab with flat sides (rock strata)."""
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
        if power == 2.0:
            d = np.sqrt(u * u + v * v) / edge
        else:
            d = (np.abs(u) ** power + np.abs(v) ** power) ** (1.0 / power) / edge
        h = np.clip(1.0 - d, 0.0, 1.0).astype(np.float32)
        rr = max(rx, ry)
        return ys, xs, h, (dx / rr).astype(np.float32), (dy / rr).astype(np.float32)

    def dab(self, cx, cy, rx, ry, angle, colour, alpha=1.0, wobble=0.25, soft=0.6,
            light=0.12, shadow=0.0, shadow_offset=0.35, edge_dark=0.0, power=2.0):
        """One soft brush dab with baked lighting.

        soft:      width of the soft rim as a fraction of the radius (1 = fully soft dome, 0.2 = firm edge).
        light:     strength of the lit upper-left / shaded lower-right gradient across the dab.
        shadow:    alpha of a soft dark pocket cast to the lower-right before the dab is painted.
        edge_dark: darken the rim (reads as a painted outline on pebbles / leaves).
        power:     2 = elliptical dab; higher = flat-sided slab (see _dab_mask).
        """
        if shadow > 0:
            off = shadow_offset * max(rx, ry)
            sys_, sxs, sh, _, _ = self._dab_mask(cx - LIGHT_DIR[0] * off, cy - LIGHT_DIR[1] * off, rx * 1.1, ry * 1.1, angle, wobble, power)
            a = (self._alpha(sh, 1.0) * shadow)[:, :, None]
            region = self.img[np.ix_(sys_, sxs)]
            self.img[np.ix_(sys_, sxs)] = region * (1 - a) + region * 0.6 * a
        ys, xs, h, px, py = self._dab_mask(cx, cy, rx, ry, angle, wobble, power)
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
    # Palette deliberately dull: in-engine sun + sky + the vertex tint push saturation UP, and the first
    # pass (light 8DB04A, mute 0.14) rendered neon lime. Olive-leaning greens with blue kept near half of green.
    # Wetlands turf: olive, yellow-leaning and dull; the dusk light and the violet ambient sit on top of it.
    base, light, deep, warm = hex_rgb("5C6A36"), hex_rgb("7C8842"), hex_rgb("3F4D2A"), hex_rgb("7E7F44")
    mid = (base + light) * 0.5
    c.tint_by_noise(1.5, base * 0.92, warm, 0.6)
    c.underpaint([base, mid, deep, warm], [0.4, 0.2, 0.2, 0.2], int(300 * area), (24, 40), (18, 30), alpha=0.6, light=0.12)
    # Deep pockets where turf clumps meet.
    for _ in range(int(30 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(26, 46), c.px(18, 30), c.u(0, math.pi), c.vary(deep, 0.04), alpha=0.55, wobble=0.35, soft=1.0, light=0.04)
    # Turf: wide, low, soft tufts lying roughly horizontal (upright pointed ovals read as leaves / clover
    # at 2 m). Still mostly opaque so the value steps stay painterly rather than smeared.
    for _ in range(int(150 * area)):
        x, y = c.pos()
        col = c.pick([base, mid, warm, light, deep], [0.34, 0.26, 0.16, 0.12, 0.12])
        c.dab(x, y, c.px(17, 30), c.px(8, 14), c.rng.normal(0, 0.35), c.vary(col, 0.06),
              alpha=0.84, wobble=0.3, soft=0.75, light=0.25, shadow=0.3, shadow_offset=0.3)
    # Blade strokes: thin lit blades fanning from a root, a few per tuft, so close-ups read as turf.
    for _ in range(int(40 * area)):
        x, y = c.pos()
        col = c.pick([light, mid], [0.6, 0.4])
        for j in range(c.rng.integers(3, 6)):
            ang = math.pi / 2 + (j - 1.5) * 0.3 + c.rng.normal(0, 0.12)
            c.dab(x + (j - 1.5) * 3.5 * c.k, y - 6 * c.k, c.px(2.2, 3.2), c.px(9, 15), ang, c.vary(col, 0.04),
                  alpha=0.75, wobble=0.12, soft=0.8, light=0.2)
    c.shade_by_noise(2.5, 0.10)
    c.mute(0.28, 0.90)
    c.soften(0.7)


def paint_meadow(c: Canvas):
    area = c.k * c.k
    # Sedge band around the pools: straw-olive, greyer than a hay meadow, wind-combed.
    base, straw, deep, green = hex_rgb("7E803E"), hex_rgb("A89C58"), hex_rgb("596038"), hex_rgb("6E7A3C")
    c.tint_by_noise(1.5, base * 0.95, straw * 0.95, 0.6)
    c.underpaint([base, straw, deep, green], [0.35, 0.3, 0.15, 0.2], int(300 * area), (24, 42), (16, 28), alpha=0.6, light=0.12)
    for _ in range(int(28 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(28, 48), c.px(18, 30), c.u(0, math.pi), c.vary(deep, 0.04), alpha=0.5, wobble=0.35, soft=1.0, light=0.04)
    # Soft low clumps of the base tones (no fat brown blobs: those read as fallen leaves on green).
    for _ in range(int(110 * area)):
        x, y = c.pos()
        col = c.pick([base, green, deep], [0.5, 0.3, 0.2])
        c.dab(x, y, c.px(16, 28), c.px(8, 13), c.rng.normal(0, 0.35), c.vary(col, 0.05),
              alpha=0.7, wobble=0.3, soft=0.8, light=0.2, shadow=0.2, shadow_offset=0.3)
    # Dry grass: long thin pale-straw blades all leaning the same general way, like wind has combed them.
    lean = c.u(-0.35, 0.35)
    pale = hex_rgb("BFB270")
    for _ in range(int(70 * area)):
        x, y = c.pos()
        col = c.pick([straw, pale, straw * 0.92], [0.45, 0.35, 0.2])
        for j in range(c.rng.integers(3, 7)):
            ang = math.pi / 2 + lean + (j - 2.5) * 0.14 + c.rng.normal(0, 0.1)
            c.dab(x + (j - 2.5) * 3.2 * c.k, y - 8 * c.k, c.px(2.0, 3.0), c.px(16, 30), ang, c.vary(col, 0.05),
                  alpha=0.72, wobble=0.1, soft=0.8, light=0.18)
    # Sparse flower specks in little groups (white, butter yellow, a touch of pink); 3-4.5 px so they
    # survive the final soften instead of vanishing.
    flowers = [hex_rgb("E8E2CC"), hex_rgb("D8C66A"), hex_rgb("8C86C8"), hex_rgb("E0D8A8")]   # cream, straw, bog-blue
    for _ in range(int(7 * area)):
        gx, gy = c.pos()
        col = c.pick(flowers, [0.4, 0.35, 0.1, 0.15])
        for _ in range(c.rng.integers(2, 5)):
            r = c.px(3.0, 4.5)
            c.dab(gx + c.rng.normal(0, 9) * c.k, gy + c.rng.normal(0, 9) * c.k, r, r * 0.8, 0, c.vary(col, 0.04),
                  alpha=0.95, wobble=0.2, soft=0.5, light=0.15, shadow=0.35, shadow_offset=0.5)
    c.shade_by_noise(2.5, 0.09)
    c.mute(0.18, 0.94)
    c.soften(0.7)


def paint_forest_floor(c: Canvas):
    area = c.k * c.k
    # Peat: dark red-brown earth (the hummocks of the references) with olive moss cushions and straw litter.
    base, litter, litter_l, moss, root = hex_rgb("463428"), hex_rgb("5E4A36"), hex_rgb("72603E"), hex_rgb("55602F"), hex_rgb("2C221C")
    soil = hex_rgb("3E302A")
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
    # Two very large, very soft bare-soil pools: the macro sample needs something big to break the grid.
    for _ in range(2):
        x, y = c.pos()
        c.dab(x, y, c.px(120, 180), c.px(70, 110), c.u(0, math.pi), c.vary(soil * 1.08, 0.03), alpha=0.5, wobble=0.35, soft=1.0, light=0.04)
    # Leaf litter: ochre ovals in loose drifts, lit on one side with a soft outline. Drift count, spread
    # and leaf count vary a lot so no two drifts look alike and they do not read as an even grid.
    for _ in range(int(13 * area)):
        gx, gy = c.pos()
        spread = c.u(0.6, 1.7)
        # A soft soil patch under the drift so the leaves sit in something.
        c.dab(gx, gy, c.px(26, 44) * spread, c.px(18, 30) * spread, c.u(0, math.pi), c.vary(soil * 1.1, 0.04), alpha=0.55, wobble=0.35, soft=1.0, light=0.06)
        for _ in range(c.rng.integers(4, int(10 + 12 * spread))):
            col = c.pick([litter, litter_l, hex_rgb("6A5A3A")], [0.45, 0.3, 0.25])
            c.dab(gx + c.rng.normal(0, 16 * spread) * c.k, gy + c.rng.normal(0, 12 * spread) * c.k, c.px(7, 13), c.px(4.5, 7.5), c.u(0, math.pi), c.vary(col, 0.07),
                  alpha=0.8, wobble=0.25, soft=0.6, light=0.3, shadow=0.3, shadow_offset=0.4, edge_dark=0.12)
    c.shade_by_noise(2.0, 0.12)
    c.soften(0.7)


def paint_dirt_road(c: Canvas):
    area = c.k * c.k
    # Pebbles keep the tan's hue AND saturation (greyer stones turn grey-violet confetti by simultaneous
    # contrast); only value, the lit top and the shadow pocket make them read.
    # Bog track: trodden grey-green mud, paler where it is worn, never tan.
    base, worn, dark, pebble = hex_rgb("7A7E62"), hex_rgb("8F9272"), hex_rgb("5E6248"), hex_rgb("8A8C74")
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
    for _ in range(int(10 * area)):
        gx, gy = c.pos()
        for _ in range(c.rng.integers(1, 3)):
            # Pebble tones sit close to the dirt value: only the lit top and the shadow pocket make them read.
            col = c.pick([pebble, hex_rgb("9A9C84"), hex_rgb("6E7058")], [0.4, 0.35, 0.25])
            r = c.px(8, 14)
            c.dab(gx + c.rng.normal(0, 22) * c.k, gy + c.rng.normal(0, 18) * c.k, r, r * c.u(0.65, 0.95), c.u(0, math.pi), c.vary(col, 0.05),
                  alpha=0.75, wobble=0.25, soft=0.6, light=0.2, shadow=0.3, shadow_offset=0.4, edge_dark=0.1)
    c.shade_by_noise(2.5, 0.07)
    c.mute(0.08, 0.97)
    c.soften(0.8)


def paint_rock(c: Canvas):
    area = c.k * c.k
    # Warm (yellow-leaning) stone: cool greys took a mauve cast next to the grass in-engine. Wide value
    # range between the lit slab faces and the cracks so the strata read as angular cliff, not cobbles.
    # Pale violet-grey stone: the cliffs must sit close to the fog colour so they read as looming painted walls.
    light, mid, dark, crack = hex_rgb("8E8288"), hex_rgb("746A70"), hex_rgb("564D54"), hex_rgb("3A3238")
    top = hex_rgb("A0949A")
    c.tint_by_noise(1.5, mid * 0.95, light, 0.6)
    c.underpaint([light, mid, dark], [0.3, 0.4, 0.3], int(200 * area), (30, 60), (12, 22), alpha=0.5, light=0.08, soft=1.0, wobble=0.2)
    # Strata: rows of flat, hard-edged slabs. Each slab gets a hard dark crack painted under its lip first,
    # then the slab face (firm rim, almost no wobble), then a thin lit top edge.
    # Band thicknesses vary (natural bedding, not courses of masonry); they sum to the tile height.
    rows = 9
    weights = c.rng.uniform(0.6, 1.5, rows)
    band_hs = weights / weights.sum() * c.n
    y_base = 0.0
    for band_h in band_hs:
        x = c.u(0, c.n)
        x_end = x + c.n  # walk one full wrap so the whole row is covered (the old loop left [band_h, x0) bare)
        tilt = c.rng.normal(0, 0.07)
        while x < x_end + band_h * 0.5:
            w = c.px(60, 320)
            h = band_h * c.u(0.7, 1.1)
            cx, cy = x + w * 0.5, y_base + band_h * 0.5 + c.rng.normal(0, 0.08) * band_h
            ang = tilt + c.rng.normal(0, 0.04)
            col = c.pick([light, mid, dark], [0.35, 0.4, 0.25])
            # Underside shadow: a crack-tone slab offset down-right, hard edged.
            c.dab(cx + w * 0.04, cy + h * 0.2, w * 0.5, h * 0.45, ang, c.vary(crack, 0.04), alpha=0.5, wobble=0.04, soft=0.2, light=0.0, power=4.5)
            # Slab face: flat-sided, firm rim, lit top-left flank.
            c.dab(cx, cy, w * 0.5, h * 0.5, ang, c.vary(col, 0.06), alpha=0.7, wobble=0.06, soft=0.12, light=0.2, power=4.0)
            # Thin lit top edge.
            c.dab(cx - w * 0.03, cy - h * 0.38, w * 0.44, h * 0.09, ang, c.vary(top, 0.03), alpha=0.6, wobble=0.1, soft=0.6, light=0.08, power=4.0)
            x += w * c.u(0.8, 1.0)  # slabs abut with a thin crack between them
        y_base += band_h
    # Fissures: tall vertical joints cutting through two or three beds, hard and dark.
    for _ in range(int(14 * area)):
        x, y = c.pos()
        c.dab(x, y, c.px(2.5, 5), c.px(40, 90), math.pi / 2 + c.rng.normal(0, 0.12), crack, alpha=0.75, wobble=0.3, soft=0.5, light=0.0)
    # Warm/cool drift so cliffs do not read as one flat tone (kept on the warm side).
    c.tint_by_noise(2.0, hex_rgb("665A62"), hex_rgb("847A7E"), 0.2)
    c.shade_by_noise(2.0, 0.12)
    c.soften(0.6)


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
    base, silt, dark, wet = hex_rgb("4E4C3E"), hex_rgb("6A6852"), hex_rgb("36362A"), hex_rgb("444838")
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
    "grass": (101, "5C6A36", paint_grass),
    "meadow": (202, "7E803E", paint_meadow),
    "forest_floor": (303, "463428", paint_forest_floor),
    "dirt_road": (404, "7A7E62", paint_dirt_road),
    "rock": (505, "746A70", paint_rock),
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
    unknown = [n for n in names if n not in MATERIALS]
    if unknown:
        ap.error(f"unknown material(s) {', '.join(unknown)}; valid: {', '.join(MATERIALS)}")
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
