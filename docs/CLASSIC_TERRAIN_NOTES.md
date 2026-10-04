# Classic Terrain Notes (analysis only)

High-level observations about how a 2004-era tiled terrain system is organised. Sources are pinned clones under
`references/classic-terrain-analysis/`.

## Licences and scope

- `warcraft-rs` (commit `76bedfd8`): dual **MIT OR Apache-2.0** (`LICENSE-MIT`, `LICENSE-APACHE`).
- `wow-1121-client-internals` (commit `d7e639fa`): documentation under **CC BY 4.0** (`LICENSE`, `NOTICE`). `NOTICE` states it contains no Blizzard code, assets or binaries, and that Blizzard names are trademarks used nominatively. Its addresses refer to a client copy the reader must obtain legally.
- Both are used here only as reading material. **Analysis only**: do not implement Blizzard file formats, reproduce proprietary data tables or constants wholesale, or import Blizzard assets. Reuse of text would need CC BY attribution; this note paraphrases and cites only.

## Sources read

- `warcraft-rs/docs/src/guides/adt-rendering.md` (read in full, 855 lines): a how-to guide with Rust and GLSL examples. Illustrative, not authoritative; some snippets are inconsistent (e.g. indexing by `chunk.height_map` as if heights were absolute, mixed `Adt` APIs).
- `wow-1121-client-internals/docs/terrain.md`: reverse-engineered 1.12.1 client behaviour (classes, loader, mesh build, alpha decode, liquids, draw order). Also skimmed: `docs/lighting.md`, `docs/minimap.md`, `docs/collision.md`.

## Tile and chunk organisation

- World = 64x64 grid of tiles; a presence table (a "WDT" file) says which tiles exist; some maps are object-only with no terrain (`adt-rendering.md`; `terrain.md` "WDT").
- Tile = 16x16 = 256 chunks. Tile size about 533.33 yards, chunk about 33.33 (`adt-rendering.md` "Overview", `terrain.md` constant 33.33).
- Chunk heightfield = 9x9 outer vertices plus 8x8 inner (cell-centre) vertices, 145 heights total, stored relative to a chunk base height (`terrain.md` "MCVT"). The guide's simplified renderer only uses the 9x9 grid; the client builds a 4-triangle fan around each cell centre.
- Normals are stored per vertex as signed bytes (145x3) rather than recomputed.
- A coarse far-distance tile (low-detail heightmap, 17x17 + 16x16 samples) is drawn beyond loaded tiles; fog hulls are used for distant haze (`terrain.md` "WDL", far-band pass).
- Terrain query (height at point, ray cast, liquid status) is done on the chunk grid by DDA walk plus per-cell triangle test.

## Texture layering and alpha blending

- **Limited layers: up to 4 textures per chunk** (`adt-rendering.md` "Understanding ADT Files"); layer 0 is the opaque base, layers 1-3 each have an alpha map (`terrain.md` MCLY/MCAL; the decoded texel has channels shadow, layer1, layer2, layer3).
- Alpha maps are 64x64 per chunk: 4 bits per texel (2048 bytes) or full 8-bit; compressed variants exist in later versions. 4-bit data blends coarsely; the client expands it to RGBA4444 for the GPU (`terrain.md` "Textures and alpha").
- Blend model is a simple sequential `mix(base, layerN, alphaN)`; tiling detail textures are repeated several times across the chunk while alpha is sampled once (`adt-rendering.md` step 5, fragment shader).
- Per-layer flags and an effect id (footstep/ground effects) ride along with each layer.
- A low-resolution 2-bit-per-cell "which layer dominates" map is used for ground-effect lookup, separate from the visual alpha.
- Texture filenames live in a tile-level list; chunks reference by index. Later versions split textures and objects into separate files (`_tex0`, `_obj0`).

## Vertex colours and baked lighting

- Per-vertex colour (named MCCV in the guide's field list, "Vertex colors: Lighting and shading information") is multiplied into the albedo, with a x2 overbright factor in the example shader (`adt-rendering.md`).
- A 1-bit 64x64 baked shadow mask per chunk (512 bytes) is merged into the alpha texel's shadow channel (`terrain.md` MCSH).
- Runtime lighting is basic: a diffuse term plus CPU vertex lighting and fog from a global day/night state (`terrain.md` "Per-pass setup", `docs/lighting.md`). The 1.12.1 doc reports no per-zone water colour table; tint comes from lighting and fog.
- MCLV (a later-expansion light-volume vertex layer) is not covered by these two sources; nothing is asserted about it here.

## Holes

- Per-chunk hole mask: 4x4 quad bits in the 1.12 client (coarse, each hole = 2x2 cells), while the guide example treats it as 8x8 bits. Holes drop triangles from the index list (`terrain.md` header `+0x3c`; `adt-rendering.md` step 3).

## Water and liquids

- Liquids are a separate surface per chunk, up to 4 slots, own 9x9 height grid plus 8x8 cell flags (type nibble, 0xF = none) (`terrain.md` MCLQ).
- Types: river, ocean, magma, slime; each maps to an animated 30-frame texture (1.25 s loop) and a depth-to-colour/alpha ramp that is a fixed static curve (`terrain.md` "Liquid type" and "LUTs").
- Rendered in separate passes after terrain, alpha blended (magma opaque, full-bright). Z-fighting at shoreline handled by small bias or polygon offset (`adt-rendering.md` "Z-Fighting with Water").
- Gameplay queries (swimming, fishable, underwater) read the same liquid data, so visuals and logic share one source.

## Objects: doodads and large structures

- Terrain files carry placement lists only: small models ("doodads", fixed-size entries with position, rotation, scale) and large buildings (WMOs, with bounds). Each chunk keeps reference lists to the placements overlapping it (`terrain.md` MDDF/MODF/MCRF).
- Scale is stored as fixed-point (1024 = 1.0); rotation as degrees; positions use a map-centre-relative axis-swapped system, so a coordinate convention layer is essential.
- Models are drawn in their own passes with distance-fade alpha for doodads; detail doodads unlink beyond about 70 units (`terrain.md` render passes).
- Objects are culled by frustum then optional occlusion, then appended to per-kind queues in a fixed order (terrain, WMO, liquid, doodad, M2, far last).

## Fixed-function-era rendering simplicity

- Draw order is deterministic: scene walk over 32 cells in fixed kind order, FIFO per-kind buckets, no sort key (`terrain.md` "Render-list buckets").
- Each chunk is one small mesh with at most 4 blended layers: a handful of draw calls and tiny textures, no per-pixel lighting, no normal or specular maps.
- Everything expensive was baked offline (shadow, vertex colour, 8-bit alpha), then streamed per tile with fog hiding the load radius.
- Precision quirks (f32 intermediates) matter only for bit-exact reproduction, not for a new system.

## Cautions for readers

- The guide is an unverified example set; the internals doc is reverse-engineered and flags several inferred fields (liquid header bits, some labels).
- Constants and layouts above are cited for understanding scale and organisation, not as a specification to adopt.
