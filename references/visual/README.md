# Visual references

Benchmarks of **composition, palette, terrain shape, scale, roads, foliage, architecture, fog and feel** for the Classic-WoW-evoking environment.
They are **not assets**. No Blizzard screenshots or art are stored here (copyright); we keep links plus original written analysis. No image files are stored.

## Contents
| File | Use |
|---|---|
| `wow-classic/CLASSIC_LOOK.md` | Cross-zone synthesis and how to approximate it in modern Godot. Read first. |
| `wow-classic/elwynn-forest.md` | Temperate green forest, farms, Goldshire, Northshire Abbey. |
| `wow-classic/dun-morogh.md` | Snowy pine mountains, Kharanos, Coldridge Valley. |
| `wow-classic/westfall.md` | Dry golden rolling farmland, Sentinel Hill, Moonbrook. |
| `wow-classic/wetlands.md` | Grey fog swamp, Menethil Harbor, Dun Algaz. |
| `concept-art-and-style.md` | Concept art, hand-painted texture and stylised-environment links. |
| `asset-packs.md` | Free stylised packs and licences (also `../assets/README.md`). |

## Honesty about sources
Search tooling returned page titles/snippets only; wiki hosts (wowpedia, warcraft.wiki.gg) are **blocked by the egress proxy**, and no images were viewed. Palette hexes are *approximate impressions from general knowledge of the game*, not sampled values. Each zone file marks which statements are search-backed. Treat hexes as starting swatches and validate against your own memory or a lawful in-person look at reference footage.
Link marks: **verified-by-search** = URL appeared in a WebSearch result; **unverified** = not returned by search / from memory.

## How to evaluate a Godot screenshot against these refs
Take the screenshot at gameplay camera height (third-person, ~3-6 m behind a 1.8 m character) and at one wide establishing view. Check:
1. **Silhouette**: Can you read hills, tree clumps and buildings as 3-4 big shapes at thumbnail size (squint test)? Chunky, simple outlines, no noisy micro-detail.
2. **Palette**: Few hue families per zone, mid-saturation, warm-leaning greens; no pure black or pure white; shadows tinted (blue/purple), not grey-black.
3. **Value structure**: Three clear bands (dark foreground trees/rocks, mid ground, pale hazy distance). Convert to greyscale and check.
4. **Fog / depth layers**: At least 3 readable depth planes; distant ridges lose contrast and shift toward the sky colour. Fog colour matches the horizon sky, and is a deliberate design tool (hides draw distance).
5. **Road readability**: Dirt road is a lighter, warmer ribbon that leads the eye to a destination; soft painted edges; follows terrain contours with gentle curves, not ruler-straight.
6. **Scale cues**: Doors, fences, wagons, trees against a character. Classic scale is slightly oversized/chunky props, big trees, broad valleys; check no "tiny-world" feel.
7. **Foliage density**: Clumped, not uniform. Open clearings between stands; trunk-heavy trees with a few big canopy masses; grass sparse tufts, not a carpet (unless the zone says so).
8. **Texture painterliness**: Low-res, soft, hand-painted look with baked highlights/dirt; no PBR micro-detail, no visible photographic noise, tiling hidden by colour variation/blending.
9. **Lighting**: Soft ambient-dominated with gentle sun; baked-looking vertex tint variation; avoid harsh high-contrast shadows and bloom-heavy HDR.
10. **Restraint**: Few prop types per scene; each placed with purpose (fence line, haystack, signpost). Empty space is fine.
11. **Zone identity**: Cover the "Classic feel" bullets in the relevant zone file; fail if it reads as generic "modern stylised forest".
Report findings per item as pass/weak/fail with one concrete fix each.
