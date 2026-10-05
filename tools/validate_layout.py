#!/usr/bin/env python3
"""Sanity-check a world layout JSON against what src/World/WorldLayout.cs and the builders expect.
Usage: tools/validate_layout.py [assets/world/layout.json]
Exit code 1 on errors; warnings are printed but do not fail."""
import json, math, sys

TREES = {f"CommonTree_{i}" for i in range(1, 6)} | {f"Pine_{i}" for i in range(1, 6)} | {"DeadTree_1", "DeadTree_3", "DeadTree_5", "TwistedTree_1", "TwistedTree_3", "TwistedTree_5"} | {f"MossTree_{i}" for i in range(1, 4)} | {f"BogOak_{i}" for i in range(1, 4)}
UNDER = {"Reeds_1", "Reeds_2", "Reeds_3", "Reeds_4", "Bush_Common", "Bush_Common_Flowers", "Fern_1", "Clover_1", "Flower_3_Group", "Flower_4_Group", "Grass_Common_Short", "Grass_Common_Tall", "Grass_Wispy_Short", "Grass_Wispy_Tall", "Mushroom_Common", "Mushroom_Laetiporus", "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3", "Pebble_Round_1", "Pebble_Round_3", "Pebble_Square_1", "Pebble_Square_3"}
LANDMARKS = {"church", "tavern", "blacksmith", "windmill", "lumbermill", "mine", "well", "tower"}
PROPS = {"wagon", "crate", "RockPath_Round_Thin", "RockPath_Square_Thin"}
GROUND = {"grass", "meadow", "forestFloor", "rock", "snow"}
PIECE_KINDS = {"cottage", "landmark", "fence", "prop", "ruinWall", "standingStone", "tree", "field", "pier", "brazier", "windowRow", "boardwalk"}
PALETTE_KEYS = ["grass", "meadow", "forestFloor", "road", "rock", "snow", "water", "waterDeep", "skyTop", "skyHorizon", "fog", "ambient", "shadowTint", "roofTile", "timber", "plaster", "stone", "leafWarm", "leafCool", "pine"]

errors, warnings = [], []
def err(m): errors.append(m)
def warn(m): warnings.append(m)
def num(v, where):
    if not isinstance(v, (int, float)) or isinstance(v, bool): err(f"{where}: expected number, got {v!r}"); return 0.0
    return float(v)
def pt(p, where):
    if not (isinstance(p, list) and len(p) == 2): err(f"{where}: expected [x, z], got {p!r}"); return (0.0, 0.0)
    return (num(p[0], where), num(p[1], where))
def hexcol(v, where):
    if not (isinstance(v, str) and v.startswith("#") and len(v) in (7, 9)): err(f"{where}: expected #RRGGBB, got {v!r}")

path = sys.argv[1] if len(sys.argv) > 1 else "assets/world/layout.json"
L = json.load(open(path))
half = num(L.get("mapSize", 1024), "mapSize") / 2
def inb(x, z, where, margin=0):
    if abs(x) > half - margin or abs(z) > half - margin: warn(f"{where}: ({x:.0f},{z:.0f}) is outside the map (+-{half - margin:.0f})")

for k in PALETTE_KEYS:
    if k not in L.get("palette", {}): warn(f"palette missing '{k}'")
    else: hexcol(L["palette"][k], f"palette.{k}")
sun = L.get("sun", {}); num(sun.get("azimuthDeg", 0), "sun.azimuthDeg"); el = num(sun.get("elevationDeg", 30), "sun.elevationDeg")
if not 8 <= el <= 70: warn(f"sun.elevationDeg {el} is unusual")
T = L.get("terrain", {})
base = num(T.get("baseHeight", 4), "terrain.baseHeight")
rim = T.get("rim", {}); num(rim.get("startRadius", 0), "rim.startRadius"); num(rim.get("edgeHeight", 0), "rim.edgeHeight")
for h in T.get("hills", []):
    inb(num(h["x"], "hill.x"), num(h["z"], "hill.z"), f"hill {h.get('name')}")
    if h.get("shape") not in ("dome", "plateau", "cliff"): err(f"hill {h.get('name')}: shape {h.get('shape')!r}")
for r in T.get("ridges", []):
    for p in r.get("points", []): pt(p, f"ridge {r.get('name')}")
for fl in T.get("flats", []): num(fl["height"], "flat.height"); inb(fl["x"], fl["z"], f"flat {fl.get('name')}")
marsh = L.get("marsh")
if marsh:
    mwl = num(marsh.get("waterLevel", 4), "marsh.waterLevel"); inb(marsh.get("x", 0), marsh.get("z", 0), "marsh")
    for pool in marsh.get("pools", []):
        inb(pool["x"], pool["z"], f"pool {pool.get('name')}")
        if math.hypot(pool["x"] - marsh.get("x", 0), pool["z"] - marsh.get("z", 0)) > marsh.get("radius", 300): warn(f"pool {pool.get('name')}: outside the marsh disc")
    for fl in T.get("flats", []):
        if math.hypot(fl["x"] - marsh.get("x", 0), fl["z"] - marsh.get("z", 0)) < marsh.get("radius", 300) and fl["height"] < mwl + 0.3: warn(f"flat {fl.get('name')}: at or below the marsh water level {mwl}")
lake = L.get("lake")
if lake:
    wl = num(lake["waterLevel"], "lake.waterLevel"); inb(lake["x"], lake["z"], "lake")
for rv in L.get("rivers", []):
    pts = [pt(p, f"river {rv.get('name')}") for p in rv.get("points", [])]
    if len(pts) < 2: err(f"river {rv.get('name')}: needs >= 2 points")
    s, e = num(rv["waterLevelStart"], "river.start"), num(rv["waterLevelEnd"], "river.end")
    if s <= e: err(f"river {rv.get('name')}: waterLevelStart ({s}) must be above waterLevelEnd ({e}) so it flows downhill")
    if lake and e != wl:
        # river ends at a map edge or at the lake; if it ends near the lake the level should match
        ex, ez = pts[-1]
        if math.hypot(ex - lake["x"], ez - lake["z"]) < 1.6 * max(lake["radiusX"], lake["radiusZ"]) and abs(e - wl) > 0.3:
            warn(f"river {rv.get('name')}: ends near the lake but waterLevelEnd {e} != lake waterLevel {wl}")
    ex, ez = pts[-1]
    if abs(ex) < half - 60 and abs(ez) < half - 60 and not (lake and math.hypot(ex - lake["x"], ez - lake["z"]) < 1.6 * max(lake["radiusX"], lake["radiusZ"])):
        warn(f"river {rv.get('name')}: does not reach a map edge or the lake (ends at {ex:.0f},{ez:.0f})")
for rd in L.get("roads", []):
    pts = [pt(p, f"road {rd.get('name')}") for p in rd.get("points", [])]
    if len(pts) < 2: err(f"road {rd.get('name')}: needs >= 2 points")
    if rd.get("kind") not in ("main", "path"): err(f"road {rd.get('name')}: kind {rd.get('kind')!r}")
    if lake:
        for (x, z) in pts:
            if math.hypot((x - lake["x"]) / lake["radiusX"], (z - lake["z"]) / lake["radiusZ"]) < 1.05: err(f"road {rd.get('name')}: control point ({x:.0f},{z:.0f}) is inside the lake")
    for i in range(1, len(pts)):
        if math.dist(pts[i - 1], pts[i]) > 220: warn(f"road {rd.get('name')}: segment {i} is {math.dist(pts[i-1], pts[i]):.0f} m long (will look straight); add a point")
trees_est = 0
for b in L.get("biomes", []):
    inb(b["x"], b["z"], f"biome {b.get('name')}")
    if b.get("groundTexture") not in GROUND: err(f"biome {b.get('name')}: groundTexture {b.get('groundTexture')!r}")
    for t in b.get("trees", []):
        if t["model"] not in TREES: err(f"biome {b.get('name')}: unknown tree model {t['model']!r}")
    for u in b.get("undergrowth", []):
        if u["model"] not in UNDER: err(f"biome {b.get('name')}: unknown undergrowth model {u['model']!r}")
    d = num(b.get("treeDensity", 0), "treeDensity")
    spacing = 25 - 20 * d
    area = math.pi * num(b["radius"], "radius") ** 2
    if b.get("trees"): trees_est += area / (spacing ** 2) * 0.55
flats = T.get("flats", [])
for s_ in L.get("sites", []):
    inb(s_["x"], s_["z"], f"site {s_.get('name')}")
    if s_.get("type") not in ("village", "farm", "ruin", "landmark", "camp", "dock", "grove"): err(f"site {s_.get('name')}: type {s_.get('type')!r}")
    if s_.get("type") in ("village", "farm") and not any(math.hypot(f["x"] - s_["x"], f["z"] - s_["z"]) < f["radius"] * 1.2 for f in flats):
        warn(f"site {s_.get('name')}: no terrain flat under it (buildings may sit on a slope)")
    for p in s_.get("pieces", []):
        k = p.get("kind")
        if k not in PIECE_KINDS: err(f"site {s_.get('name')}: piece kind {k!r}"); continue
        if k == "landmark" and p.get("model") not in LANDMARKS: err(f"site {s_.get('name')}: landmark model {p.get('model')!r}")
        if k == "prop" and p.get("model") not in PROPS: err(f"site {s_.get('name')}: prop model {p.get('model')!r}")
        if k == "tree" and p.get("model") not in TREES: err(f"site {s_.get('name')}: tree model {p.get('model')!r}")
        if k == "cottage":
            fp = p.get("footprint")
            if fp not in ([2, 2], [2, 4], [3, 4]): err(f"site {s_.get('name')}: cottage footprint {fp!r} (allowed [2,2] [2,4] [3,4])")
            if p.get("style") not in ("plaster", "timber", "stone"): err(f"site {s_.get('name')}: cottage style {p.get('style')!r}")
        if k == "fence" and len(p.get("points", [])) < 2: err(f"site {s_.get('name')}: fence needs >= 2 points")
for sh in L.get("shots", []):
    if len(sh.get("from", [])) != 3 or len(sh.get("lookAt", [])) != 3: err(f"shot {sh.get('name')}: from/lookAt need 3 numbers")
    if sh.get("fromAboveGround") is None: warn(f"shot {sh.get('name')}: no fromAboveGround; absolute y {sh.get('from', ['?','?','?'])[1]} must be above terrain")
print(f"{path}: {len(L.get('sites', []))} sites, {len(L.get('biomes', []))} biomes, {len(L.get('roads', []))} roads, {len(L.get('rivers', []))} rivers, {len(L.get('shots', []))} shots; rough tree estimate {trees_est:.0f}")
for w in warnings: print("WARN ", w)
for e in errors: print("ERROR", e)
sys.exit(1 if errors else 0)
