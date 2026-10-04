# Fallowmere — world bible

Machine-readable twin: `assets/world/layout.json` (authored as `world_layout.json`). Coordinates everywhere: metres from map centre, **X east, Z south** (Godot −Z = north), Y = height. Every number below was checked against a numpy port of `TerrainBuilder.cs` (noise = 0, rim mass 0.85); real heights wander ±4 m with the base noise, so site heights are pinned with `flats`.

## 1. Identity
Fallowmere is a sheltered upland vale closed on every side by grey fells, cupping one long lake. A slate-roofed village sits on the south-west shore; across the water a lordship quietly ended a generation ago: the lord's hall on Harrowfold Head burned and was given back to three copper-leaved elder trees, the lord's bridge fell and the villagers laid planks beside it, and one brazier still burns on the Candle, the tower the whole road points to. The story is told through restraint: one road, one lake, one village, one dark wood, one lit tower, a cold white rim to the north that nobody goes to, and a fogged gorge to the south-east where road and river leave together.

**Three acts of the walk:** spawn with one destination (windmill sails) → the lake withheld behind Hob's Rise → saddle reveal of the entire remaining route → cosy village → broken bridge and the dark wood → coil up Candlehill → look back into the low sun.

## 2. Map overview (1024 × 1024 m, one char ≈ 20 × 40 m)
```
 x: -500        -300        -100         +100        +300        +500
    ^^^^^^^^^^^^^^^^^^^^  HOARFELLS (snow, N edge 215 m) ^^^^^^^^^^^^^^   z -500
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^  ~ spring 34 m  ^^^^^^^^^^^^^^^^^^^^^^^
    ^^^^^ Westfell ^^^^^ pines  pines  ~Beck~  pines  Eastfell pines ^^^   z -400
    ^^^^^^^^   Greyhame Shoulder (45)   ~   pines   pines   pines  ^^^^^
    ^^^^^           pines  pines    D  ~Fallow~   pines         ^^^^^^^   z -220  D Greyhame Delve
    ^^^                               S~  (Sawyer's Reach, wheel)   ^^^^
    ^^                   parkland   ~~~~~~~~~~~~~~~~~~                ^^   z -120
    ^                           ~~~~~  THE MERE  ~~~~~~~~
    ^   Gap Meadow        ~~~~~~~~~~  (wl 4)   L   ~~~~~~~~    pines    ^   z  -40  L Ladyholm elder
    ^ G o  road ~~~  M crest  c ~~~~~~~~~~~~~~~~~~~~~~  H  . . .         ^   z  +20  G Gap  o Oak  M Mill
    ^==road====> Hob's Rise ==T== = = road = = + ==(=)## W ## . C .    ^   z  +90  c church  T Thistlebury
    ^                 fields  |            |      ### * ## . coil .  ^^   z +160  + Thistle Cross  (=) Fallen Span
    ^^      Shepherd's Bank    |  Thistle Moor   |  ##Hollin##  Candlehill ^^   z +220  W wood  * Nine Sisters
    ^^^                        |                 R (Rook Fold)  ~   . . ^^^   z +300  H Harrowfold Hall  C the Candle
    ^^^^^^   Southfell pines   |                        ~~ Throat ~ road ^^^   z +380
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ gorge ~~exit ^^^   z +500
```
Rim foot ≈ 400 m from centre (squircle); rim crest heights N 215 / E 145 / S 130 / W 150 (+ Hoarfells mass). Snow line auto = 6 + 155·0.62 ≈ 102 m, full above ~121 m: effectively only the Hoarfells are white; the other rims get dusted crests.

## 3. Geography (numbers)
**Base** 6 m, chunky (noise ≥ 60 m wavelength, ±4 m). **Rim** starts at 400 m, roughness 0.35, two automatic terrace bands on slopes > 26°.

| Hill | Centre | r | h (summit) | Shape / role |
|---|---|---|---|---|
| Hoarfells | (−40, −560) | 340 | +55 (N rim → 215–240) | dome; north snow mass, river spring on its skirt |
| Greyhame Shoulder | (−170, −300) | 140 | 36 (45) | dome; pine shoulder, mine at its SE foot |
| Sawyer's Rise | (−20, −225) | 110 | 5 (11) | low swell that keeps the upper river below its banks |
| Hob's Rise | (−225, 50) | 115 | 18 (24) | dome; **hides the lake from spawn**, windmill on the crest |
| Harrowfold Head | (165, 20) | 60 | 14 (20) | steep knoll on the SE shore; the ruined hall on top |
| Candlehill | (300, 150) | 130 | 50 (56) | bald dome; the coil road and the Candle |
| Rook Knoll | (90, 300) | 110 | 18 (24) | open sheep knoll; hides the gorge mouth from the lake |

**Basins:** Pilgrim's Gap notch (−512, 125) r 125, depth 90 → notch floor 9 m at spawn rising to ~20 m at x −470 and ~38 m at x −500 (into fog; walls ~70 m at 60 m aside). The Throat notch (400, 470) r 140, depth 80 → gorge walls ramp 7 → 36 → 60 → 90 m above the river from mouth to edge.
**Ridge:** Shepherd's Bank (−200,200)→(−90,255)→(20,310), width 55, +12: separates the village fields from Thistle Moor.
**Flats (levelled sites):** Gap apron (−432,122) 9.5; Hob's Mill (−225,50) 24; Thistlebury (−105,75) r 45 → 7.5; church terrace (−60,15) r 18 → 7.5 (a promontory into the water); Ladyholm (40,−50) r 10 → 6.5; Sawyer's Reach (−38,−160) 9; Delve apron (−120,−215) 13.5; Harrowfold Hall (165,20) r 20 → 20; Nine Sisters clearing (215,160) r 24 → 14; the Candle (300,150) r 18 → 56; Rook Fold (90,300) 23.5.

**The Mere:** centre (25, −35), radii 100 × 85 m, water level 4.0, bed −3, shore ring 4.6–6.1 over 25–30 m (readable shallows). East edge x 125, west x −75, north z −120, south z 50.

**Rivers (water level, downhill, through the lake):**
- Fallow Beck, the Whitestep cascade: (−75,−405) 34 → (−48,−290) 12, width 5, 19 % (white water between pines; the "thin white line" seen up the lake).
- The Fallow (upper): (−48,−290) 12 → (5,−122) 4, width 7, 4 %, past Sawyer's Reach into the north shore. Banks 1–2.5 m below the floor everywhere.
- The Fallow (lower): leaves the SE shore at (100,25) 4 → Fallen Span (140,80) → (175,150) → (215,235) → gorge mouth (255,320) −1 → (310,400) → (380,470) → edge (440,512) −6. Width 10, wraps Candlehill's west and south foot and bends twice in the Throat so the exit is never seen.

**Roads (lighter painted ribbon, flattened ±0.3 m, soft 2 m edge):**
- *Fallow Road* (main, 5 m, 1240 m): Pilgrim's Stair (−472,135) 19 → spawn (−432,122) 9 → Gap Meadow (−385..−330, 100–110) 6 → swings **north round Hob's Rise** (−316,34) 8 → (−270,−4) 13 → (−225,2) 18 → crest beside the mill (−199,35) 20.5 → saddle bend (−184,57) 19 → (−181,87) 15 → (−187,116) 11 → (−160,110) 8.5 → Thistlebury (−140,96)…(−65,72) 7.5 → shore road (−20,84) → Thistle Cross (40,92) → (90,92) → Fallen Span ford (140,80) → wood (182,98) (215,78) (245,62) → **Candlehill coil** 1.06 turns clockwise, 9 control points from r 104 to r 12, 11–16 % → the Candle (297,138) 56. Only the two Stair legs behind the spawn exceed 15 % (22–28 %, into fog, never walked).
- *Throat Road* (main, 5 m, 564 m): forks from the coil at (332,78) 23 → unwinds down the E flank (368,92) (392,122) (394,165) (378,210) (352,255) (322,295) → hugs the river's NE bank 12 m off the centreline (290,350) (316,387) (345,418) (377,450) (411,477) (441,498) → edge (452,512). Grades ≤ 13 % except local bank wobble.
- *Sawyer's Path* (4 m): village north end (−95,42) → west shore → Sawyer's Reach (−52,−150) → Greyhame Delve (−120,−215). 303 m, ≤ 14 %.
- *Rook Track* (3 m): Thistle Cross → (48,140) → spirals once round Rook Knoll to the fold. 258 m, ≤ 15 %.
- *Sisters' Spur* (3 m, 63 m): (222,84) → (213,108) → (221,132) → (216,143). *Hall Path* (3 m, 141 m): (160,88) → one counter-clockwise turn round Harrowfold Head to the hall.

## 4. Landmarks and settlements
Nine landmarks; five pieces of minor dressing. Kit = Quaternius Medieval Village (2 m modules, stone boxes for walls/stones), KayKit landmarks ×8–9, Quaternius Nature.

1. **Pilgrim's Gap** (spawn) — (−440,124), h ≈ 10, 12 × 12 m. Three leaning waystones 3.4–3.6 m in a half ring, RockPath paving, one crate, two boulders. Behind: the Stair rising into fog between 70 m walls. *Silhouette:* three dark fingers against pale notch walls. *Purpose:* the way in; from here the only thing to look at is the road and the sails.
2. **Hob's Mill** — (−225,50) on the crest, h 24, 40 × 30 m. KayKit windmill ×8 (hub ~10 m, sails to ~41 m, 0.1 rad/s), one 4 × 4 plaster cottage with chimney, 4 fence spans along the road, wagon, two crates, two boulders. *Silhouette:* cross of sails on the skyline, 215 m from spawn, 540 m from the Candle. *Purpose:* first destination; the rise that hides the lake.
3. **Thistlebury** — (−105,75), 110 × 90 m, flat 7.5 m. Church ×8 on the shore promontory (−60,15) facing east over the water; stone pier "the Hythe" 16 × 2 m into the Mere (top 1.4 m above water); tavern ×8 (−82,92) facing the square; blacksmith (−128,96); well (−103,68) ringed by RockPath; four cottages: 4 × 8 timber two-storey (−130,55), 4 × 4 plaster (−88,110), 6 × 8 stone-base two-storey (−62,98), 4 × 4 stone (−80,45); orchard of 12 CommonTree_2 ×0.7 with copper leaves behind the stone house; two fields south (wheat 26 × 16, fallow 22 × 14); fences on the approach road, one yard, orchard road-side, field edge; 2 wagons, 6 crates, 2 shade trees. All roofs #5E7079. *Silhouette:* bell tower on the promontory, long tavern roof behind, read against water from the saddle. *Purpose:* the hub.
4. **Ladyholm** — islet (40,−50), r 10, top 6.5; one TwistedTree_3 (dark green #45603A, 16 m) and four waterline boulders. *Silhouette:* a single huge tree standing in the lake, reflected. *Purpose:* focal point of every lake view; nothing to do there, which is the point.
5. **The Fallen Span** — (140,80) across the lower Fallow where the road fords it (site rotated −36° so local x runs across the river). Two broken abutments (4 × 3.2 m stone boxes) and a mid-river pier (2.5 × 2.4 m) 8 m downstream of the road; a fallen arch section (5 × 1.1 m) in the shallows; six RockPath_Square stepping stones on the road line; two boulders, a fence stub. Ford depth ~1 m. *Silhouette:* broken arch stubs mirrored in slow water with the black wall of Hollin Wood behind. *Purpose:* threshold between bright shore and dark wood; the story's first line.
6. **The Nine Sisters** — (215,160), clearing r 24 at 14 m inside Hollin Wood, 63 m down a worn spur. Eight standing stones 4.2–4.5 m on an 11 m ring plus one fallen (4.4 × 0.8 m), a RockPath disc at the centre, four TwistedTree elders (cool #3F6A38) at r 25. *Silhouette:* nine verticals in a shaft of light. *Purpose:* the wood's secret; dread turns to wonder.
7. **Harrowfold Hall** — (165,20) on the Head, flat 20 m, 30 × 22 m, rotated 15°. Roofless Wall_UnevenBrick teeth 2.5–6 m high in a broken rhythm, one surviving corner tower (KayKit tower ×7, ~13 m, tinted toward soot), floor remnants of RockPath, three **copper** TwistedTree elders (#B5783A, the zone's only warm leaf accent besides the orchard) grown through the walls, two dead trees on the lake lip, fallen masonry. *Silhouette:* tower stub and wall teeth under three huge copper crowns on a steep banded knoll above the water, seen across the Mere from the village and the saddle. *Purpose:* the zone icon; the lordship that ended.
8. **The Candle** — (300,150), summit 56, 20 × 20 m. KayKit tower ×9 (~17 m) with an **emissive brazier box #F0A050 on top** (the only warm light in the zone, reads through fog at 480 m), 1 m parapet walls on a 13 m square, six boulders, one crate. *Silhouette:* a single round finger on a bald dome, the pale coil road wrapped beneath it. *Purpose:* the road's end and the look-back; from the parapet the gorge opens to the south-east.
9. **Sawyer's Reach** — (−36,−162) on the west bank, flat 9, 30 × 20 m. KayKit lumbermill ×8 with its wheel wall toward the river (east), log-stack crates ×4, wagon, 8 fence spans, the first two pines. 130 m upstream the Whitestep cascade falls 22 m through the pines. *Silhouette:* wheel and long roof beside white water under the Hoarfells. *Purpose:* end of the inhabited world going north.

Minor dressing: **The Hollow Oak** (−372,142): one 16 m TwistedTree_1 (green #5E7A3A) in a flowered clearing the road bends round, with a 2-span fence stub and a crate as a wayside shrine. **Thistle Cross** (40,92): 2.6 m waystone at the crossroads where the road east, the road west and the Rook Track meet, with mill, Candle and the wood all in view. **The Warden** (226,90): one dark elder leaning over the road at the Sisters' fork. **Rook Fold** (90,300): dry-stone sheepfold 14 × 14 × 1 m, one oversized CommonTree_3 ×1.6, a 40 m fence line down the north slope, three sunk boulders; the moor look-back. **Greyhame Delve** (−120,−215): KayKit mine ×8 facing SE at the shoulder's foot, a **still-loaded wagon** (3 crates), 6 fence spans, one dead tree, three boulders; nowhere further to go.

Hero-tree ledger (hard rule): copper elders 3 (hall only); green elders: Ladyholm 1, Sisters 4, Hollow Oak 1, Warden 1 = 10 TwistedTrees total. Dead trees only at the Head lip (2), the Delve (1), Hollin Wood (sparse), Thistle Moor (very sparse) and the Throat.

## 5. Foliage plan (biomes are soft-edged circles; higher priority wins)
Calibration target: `treeDensity 1.0 ≈ one tree per 30 m²` (5.5 m spacing); estimated totals below use that rule after removing water, slopes > 36°, roads ±5 m and site clearings. **Estimated total ≈ 3,200 trees** (3,180 biome + 30 site trees), inside the 2,500–3,500 budget.

| Region (centre, r, prio) | Density / clump | Trees | Undergrowth | Ground | ≈ count |
|---|---|---|---|---|---|
| Hoarfell Pines (−40,−430) 240, p3 | 0.45 / 0.5 | Pine_1–5 ×1.2, tint #35543E | Rock_Medium 1/3, wispy short, Laetiporus | grass → snow above 102 m | 750 |
| Greyhame Pines (−190,−300) 160, p3 | 0.50 / 0.5 | Pines + DeadTree_1 ×0.55 (w 0.3) | Rock_Medium_2, wispy short | grass | 470 |
| Eastfell Pines (300,−320) 210, p3 | 0.30 / 0.6 | Pines 1.2 + CommonTree_2/4 (w 0.7) | Rock_Medium_1, wispy | grass | 580 |
| North Shore (−30,−190) 110, p4 | 0.15 / 0.7 | CommonTree_1/3/5 + Pine_2/4 | Clover, wispy, pebbles | grass | 120 |
| **Hollin Wood** (205,100) 110, p5 | **0.85 / 0.25** | CommonTree_1–5 ×1.25, leaves #3F6A38 (darker, cooler) + DeadTree_3 ×0.6 | Fern_1 0.3, Mushroom_Common 0.08, Laetiporus, Bush_Common edge | **forestFloor** | 380 |
| Candlehill Flank (300,150) 135, p4 | 0.22 / 0.7 | Pine_1/3/5 + CommonTree_2/4 | wispy, Rock_Medium_2 | grass | 120 |
| Candlehill Top (300,150) 72, p7 | 0 | none (bald) | Grass_Wispy_Tall, Rock_Medium_1 | grass | 0 |
| Harrowfold Head (165,20) 62, p6 | 0.10 | CommonTree_4 + DeadTree_5 ×0.6 | Rock_Medium_1, wispy tall | grass (rock by slope) | 16 |
| Gap Meadow (−385,130) 95, p3 | 0.07 / 0.85 | CommonTree_1–5 ×1.35 (big lone trees) | Flower_3/4_Group 0.12, Bush_Common_Flowers, Grass_Common_Tall | **meadow** | 37 |
| Hob's Rise (−225,50) 115, p3 | 0.04 / 0.9 | CommonTree_3/4 ×1.3 | wispy short/tall | grass | 31 |
| Mere Parkland (25,−35) 230, p2 | 0.12 / 0.75 | CommonTree_1–5 ×1.2 | Grass_Common_Short 0.3, Clover, Bush_Common, Flower_4, pebbles | grass | 210 |
| Thistle Moor (−70,250) 210, p2 | 0.035 / 0.9 | CommonTree_3 ×1.45, CommonTree_5 ×1.4, DeadTree_1 (w 0.2) | wispy tall/short, Rock_Medium_3 (sunk boulders) | **meadow** (straw) | 100 |
| The Throat (390,440) 150, p5 | 0.08 / 0.6 | DeadTree_1/3/5 ×0.6 + Pine_2/4 | Rock_Medium_1/2/3 0.06 each, Pebble_Square | **rock** | 35 |
| Southfell / Westfell / Eastfell rims, p2 | 0.18–0.20 / 0.7 | Pine + CommonTree mix ×1.1 | Rock_Medium, wispy | grass | 330 |

Rules: clumps of 5–12 with 30–70 m glades everywhere except Hollin Wood (one uniform dark wood by design); bare crests on Hob's Rise and Candlehill; the lake shores and the village surrounds open; grass tufts only within 60 m of camera; rocks only at cliff bands, shores, the Throat and as sunk moor boulders; ferns and mushrooms only in Hollin Wood; flowers only in the Gap Meadow and around the village. Prop types per area ≤ 4: meadow = fence · flowers · wagon · oak; village = fence · crate · wagon · well; wood = fern · mushroom · dead tree · stones; north = crate · wagon · rock; moor = fence · boulder · lone tree; Throat = dead tree · boulder.

## 6. Palette and lighting
One warm-leaning palette, three hue families (warm green, tan/timber, slate blue-grey) plus one warm accent family (copper elders + orchard + brazier) and one cold accent (Hoarfells snow).

| Role | Hex | Role | Hex |
|---|---|---|---|
| grass | #6F9A3F | meadow (straw tint) | #94AE52 |
| forest floor | #4D5530 | dirt road | #B99862 |
| rock | #857C70 | snow | #E4EAEE |
| water / deep | #4F7A86 / #3E6470 | sky top | #6F9BC8 |
| sky horizon = fog | #CDD3C4 | ambient | #9AA8B8 |
| shadow tint | #5A6A8C | roof (all buildings) | #5E7079 |
| timber / plaster / stone | #7E5A3A / #E6D9B8 / #8C8A80 | broadleaf warm / cool | #7FA448 / #4D7A3C |
| pine | #35543E | copper accent (elders, orchard) | #B5783A |
| Hollin Wood leaves | #3F6A38 | brazier light | #F0A050 |

Sun azimuth **250°** (from the WSW, at the player's back on the eastward walk), elevation **28°**, colour #FFE3B4, energy 1.1, shadows ~45 % strength, ambient sky-dominant. Depth fog 130 → 820 m (three ridge planes: Hob's Rise, Candlehill, Hoarfells), colour = horizon exactly; height fog band below 7 m for mist on the Mere and in the Throat. No bloom, SSAO or SSR. No per-area fog volume: the dark wood is made with canopy density, cooler-darker leaves, forest-floor texture and shadow.

## 7. Hero shots (all in `shots`; eye = ground + 1.7 unless noted)
1. **Spawn** — cam (−428,121) eye ~10 → mill hub (−225,50)+10. Road leaves bottom centre, S-curves through the flowered meadow past the Hollow Oak and climbs left round the Rise to the sails on the skyline; waystones frame the left edge; the lake does not exist yet; the Candle's tip is a tease far right.
2. **The reveal** (saddle) — cam (−184,57) eye 20.7 → (60,30)+6, FOV 68. The road drops left to the church promontory, pier and tavern roof; the whole Mere with Ladyholm's elder; Harrowfold's copper crowns on their knoll centre-right; the Candle a pale finger beyond; pines and the white Hoarfells dissolving into fog. Three depth planes, four big shapes.
3. **Village** — cam (−130,100) → (−75,60)+3. Well and wanderer centre, tavern roof right, timber jetty cottage left, church tower over the roofs, Ladyholm on the water beyond. Front-lit, blue shade under eaves.
4. **Harrowfold across the water** — cam (−48,14) on the church terrace → (165,20)+16, FOV 60. Pier and reeds dark in the foreground, the Mere carrying the sky's grey-green, the banded knoll with wall teeth and three copper crowns at the focal point, the Candle on its dome 22° to the right, the east rim pale behind.
5. **The Candle** (establishing wide) — cam (300,150)+18 (parapet) → (−20,0)+6, FOV 62. Into the sun: the Mere a sheet of light with Ladyholm black on it, Thistlebury on the far shore, the sails a speck on Hob's Rise, Hollin Wood a dark pool below, the coil road under the camera, the west rim gone to fog.
Secondary: **Wood** (172,94)→(222,82): the Warden's trunk fills the left third, ferns and mushrooms, the road bending into green dark with a pale gap uphill. **Throat** (352,255)→(318,400)+26: dead trees and boulders replace grass, the river curls into the gorge, walls close, fog swallows the bend. **Postcard** (−52,22)→(−50,−290)+10: up the lake from the terrace to the mill wheel, the Whitestep's white line, black pines, white fells.

## 8. The walk
Spawn beside the three waystones; behind, the Stair climbs into grey fog. Ahead the road runs east into a flowered meadow and climbs to a windmill whose sails turn on the skyline 215 m away. One destination. *Beat: a place to go.*
Gap Meadow: fence lines lead the road; the road bends round a 16 m gnarled oak with a crate shrine at its roots; three broadleaf clumps, the Candle's tip far east. *Beat: first wonder.*
Hob's Rise: the road swings north round the shoulder, the hill always between you and whatever lies east, then climbs to the crest beside the mill; sails loom, a cottage, a wagon. First look back over the meadow to the notch. *Beat: height.*
The saddle: ten steps past the mill the ground falls away and the vale opens in one picture — the Mere glittering, a church on a promontory, a pier, slate roofs, one elder on an islet, copper crowns over a ruin across the water, a tower on a bare hill, white fells over black pines. *Beat: the reveal; the whole remaining walk in one frame.*
Thistlebury: switchback descent along a fence line to the square — well, tavern, smithy, orchard glowing copper, wheat and fallow fields; out on the Hythe, Ladyholm 90 m offshore and Harrowfold straight ahead. *Beat: home.*
Shore road to Thistle Cross: open parkland, water left; at the waystone three ways: back, the sheep track south to Rook Knoll, the road east. *Beat: choice.*
The Fallen Span: the lord's bridge is two stubs and a pier; you wade the planks beside it; beyond, a solid wall of trees. *Beat: threshold.*
Hollin Wood: light drops, ferns brush the road, mushrooms on dead trunks, the Warden leans over the way; a worn spur south, and 60 m on, sunlight on nine stones in a ring, one fallen. *Beat: quiet fear, then wonder.*
Candlehill: the road climbs out of the trees and coils a full turn round the bald dome; the lake reappears at every quarter turn, then the gorge and the fogged south-east. At the fork a second road unwinds down toward the Throat. *Beat: climb.*
The Candle: from the parapet, looking back west into the low sun, everything walked is in one frame; the brazier burns beside you. *Beat: payoff.*
Codas: Sawyer's Path north to the wheel, the cascade and the loaded wagon at the Delve under the snow; the Throat Road down beside the river into the gorge, walls closing, fog to white — the world continues.

## 9. Classic checklist → design choices
1. **Silhouette:** every landmark is one primitive (sails on a dome, bell tower on a promontory, one tree in a lake, broken arch stubs, nine verticals, wall teeth under three crowns, a finger on a bald hill); hills are seven smooth domes plus a banded rim; no terrain noise under 60 m.
2. **Palette:** three hue families, mid saturation, warm-leaning greens, shadows tinted #5A6A8C, no black or white; copper is confined to the hall and orchard, snow to the north rim.
3. **Value structure:** each hero shot has a dark near band (waystones, Warden trunk, pier, parapet), a mid band (village, lake) and a pale far band (rim in fog); WSW sun behind the walker keeps mid tones readable; only the Candle look-back is into the light.
4. **Fog / depth:** fog = horizon #CDD3C4; 130–820 m gives three ridge planes; low height fog on the Mere and in the Throat; both notches bend so no sky-hole or map edge shows.
5. **Road readability:** one warm ribbon from a real entry to a real tower with real destinations on every branch; S-curves follow contours; fences line the approach; grade ≤ 15 % except the fogged Stair.
6. **Scale cues:** 1.8 m wanderer against 3.5 m waystones, 2 m fence spans, 4 m wagons, 16 m elders, 17 m tower, a 10 m river waded on planks, 70 m notch walls, a 1 km vale.
7. **Foliage density:** clumps with glades, bare crests and shores, one dense wood by design, tufts only near the camera; ~3,200 trees.
8. **Painterliness:** procedural soft tiles (grass, meadow, forest floor, road, rock, snow), ≤ 4 blends per area, vertex hue tint (warm SW faces, cool hollows, straw on the moor), hand-painted kit textures, no PBR maps.
9. **Lighting:** ambient-dominant, soft 28° sun, 45 % blue shade, no bloom/SSAO; one emissive brazier.
10. **Restraint:** nine landmarks on 1 km², ≤ 4 prop types per view, an empty moor and parkland, a lake with a single tree in it, ten hero trees, dead trees only where they mean something.
11. **Zone identity:** Elwynn-register pastoral cosiness (lake village, mill, church, fences, fields) with a Dun Morogh cold rim as accent, one Wetlands-grade dark passage and a fogged exit gorge; the quiet story of a lordship that ended and a village that stayed makes it Fallowmere, not a generic stylised forest.

## 10. Build notes / integrator requests
- `TerrainBuilder`: after `flats`, recompute `WaterDepth = max(0, waterLevel − h)` inside the lake ellipse (Ladyholm and the church promontory are flats inside the lake and must not read as wet).
- `SiteBuilder`: pieces over water (the Hythe pier `ruinWall`, Fallen Span pier/arch, ford `RockPath` planks) sit on max(ground, waterLevel + 0.1); `tower` at the Candle gets a 1 m emissive box (#F0A050) on top; the hall tower is tinted toward soot; `rotationDeg` convention: 0 = local +x along world +x, clockwise seen from above.
- Throat Road: snap the profile to the river bank (level + 1.6) where it is within 14 m of the lower Fallow so it reads as a towpath, not a ledge.
- Water: the Whitestep reach (19 %) wants a pale foam tint; nothing else.
- Validator warnings expected and accepted: Hoarfells hill centre is off-map (it is the north rim mass); Fallow Beck ends where the upper Fallow begins (one river split in two for the grade kink).
