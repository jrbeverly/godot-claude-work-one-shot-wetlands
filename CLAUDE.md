# CLAUDE.md

Godot 4.5 .NET (C#) project: an original single-player fantasy zone that captures the environmental feel of Vanilla-era MMO worlds (see `docs/WORLD_BIBLE.md`). The whole world is generated at startup from `assets/world/layout.json` by small project-specific systems in `src/World/`. Do not add gameplay, RPG, networking or streaming systems unless asked.

## Conventions
- Project-owned code is **C#** (`src/`, namespace `GodotWarcraft`, file-scoped namespaces). GDScript only where a third-party addon requires it.
- Layout: `scenes/` (.tscn), `src/` (C#), `assets/`, `addons/` (empty; nothing integrated), `tools/` (scripts), `docs/`, `references/`.
- Keep it simple. Prefer small project-specific tools over generic frameworks. Do not overengineer.
- Keep it buildable: `dotnet build` must pass. Smoke test: `tools/run.sh` (needs `GODOT` env var pointing to the Godot 4.5 .NET binary).
- Look at the world, don't just reason about it: `tools/screenshot.sh` renders the layout's hero shots headlessly (Xvfb + lavapipe); view the PNGs and judge them with `references/visual/README.md`. Diagnostics: `EXTRA="--nofog --noshadow"`, `EXTRA="--debug-draw=unshaded"`.
- World authoring lives in `assets/world/layout.json` (schema: `src/World/WorldLayout.cs`; check with `tools/validate_layout.py`). Change the world by editing the layout or the builders, not by hand-placing nodes in scenes. Coordinates: metres from map centre, X east, Z south, Y up.
- Build order in `WorldRoot`: terrain → water → sites → foliage; `Atmosphere` owns sky/sun/fog (all mood knobs live in the layout's `palette`, `fog` and `sun` blocks: fog curve/ceiling, height fog, ambient energy, grading, clouds, stars, sun disc/halo). New assets need a headless import (`godot --headless --import`), which `tools/screenshot.sh` does for you; new PNGs get `mipmaps/generate=true`, lossless, `detect_3d/compress_to=0` in their `.import`.
- The zone is a **Wetlands-style bog at dusk** (see `references/visual/wow-classic/*.jpg`): the `marsh` block in the layout is one water sheet at `waterLevel` over the lowland, with authored `pools` carved below it and a noise field of peat `hummocks` rising out of it (`TerrainBuilder` step 2c, `WaterBuilder.BuildMarshMesh`). Roads inside the disc ride as causeways; peat/sedge/turf splat rules follow height above the sheet.
- Procedural models (`ProceduralFoliage`, usable by name anywhere a kit model is): `MossTree_1..3` (weeping moss willow: trunk + canopy cards + hanging curtains), `BogOak_1..3` (giant dark silhouette tree on a DeadTree trunk), `Reeds_1..3` (crossed reed cards). Their painted cards come from `tools/paint_foliage.py` (`assets/textures/foliage/`); ground tiles from `tools/paint_textures.py`. Moss trees and bog oaks may stand in the shallows; reeds crowd within ~7 m of water.
- Site pieces (`SiteBuilder`): cottage (footprint [2,2]/[2,4]/[3,4], style plaster/timber/stone, storeys 1-2), landmark (KayKit church/tavern/blacksmith/windmill/lumbermill/mine/well/tower; `scale` 1 means the kit default 8x; optional `tint`), fence (points), prop (wagon/crate/RockPath_*), ruinWall (`intact: true` for a standing wall stacked to `heightM`), standingStone (`heightM` = height above ground), tree (model, scale, leafTint), field (wheat/pumpkin/fallow), pier (stone; lengthM/widthM/heightM above water), boardwalk (timber planks on posts; same fields), brazier (heightM), windowRow (lit emissive windows: `count`, `spacingM`, `heightM`, faces local +Z). Piece x/z are site-local offsets before `rotationDeg` (clockwise from above).
- Visual iteration loop: change layout/builders → `tools/screenshot.sh` → view PNGs → judge with the checklist → fix. Curated shots for docs: `tools/curate_screenshots.py`.

## Reference library: look here before relevant decisions
These materials ground the project. Index: `references/README.md`. Fetch code refs once with `tools/fetch-references.sh` (pinned; committed source snapshots, assets omitted). Docs, catalogues and visual notes below are committed and available immediately.

| If you are… | Consult first | What it is for |
|---|---|---|
| Making or **judging how the world looks** (any screenshot, palette, fog, foliage, roads, architecture, scale) | `references/visual/README.md` (11-point screenshot checklist) → `visual/wow-classic/CLASSIC_LOOK.md` → the zone file (`elwynn-forest`, `dun-morogh`, `westfall`, `wetlands`) | **Visual benchmarks**: the qualities to match. Links + written analysis only; no Blizzard images stored. Never copy Blizzard assets. |
| Choosing placeholder art | `references/visual/asset-packs.md`, `references/assets/README.md` | Free stylised packs + licences (Quaternius, Kenney, KayKit CC0…). Selected models + previews under `references/assets/selected/`; index in `references/assets/README.md`. |
| Implementing terrain, foliage/grass, roads/rivers, props/villages, world-editing tools, third-person camera/controller, water, sky/weather, stylised rendering | `references/godot/candidates/<topic>.md` (ranked catalogue incl. link-only/skip + licences), then the cloned repo it names under `references/godot/<topic>/` | **Technical references**: possible implementations (Terrain3D, HTerrain, ProtonScatter, GodotGrass, Sky3D, …). They do not dictate our language or architecture. |
| Needing to know how classic WoW terrain worked / what an editor exposes | `docs/CLASSIC_TERRAIN_NOTES.md`, `docs/WORLD_EDITOR_REFERENCE.md` | Analysis only; no Blizzard formats/data, no Noggit (GPL) code. |
| Setting up Godot↔Claude tooling | `docs/MCP_COMPARISON.md` | MCP candidates; test mkdevkit first; none installed. |
| Scoping a system | `references/environment-case-studies/` (godotwind) | What **not** to build for a small environment. skelerealms = future RPG only. |
| Structuring C#/agent guidance | `references/starters/`, `references/ai-development/` | Project-layout and agent-doc patterns. |

Rules: consult selectively (open one catalogue/file, Grep inside one clone; never load wholesale). Prefer evaluating a rendered screenshot against the visual checklist over reasoning in the abstract. Tracks that are single-player only: ignore multiplayer/MMO material. Check licences (`THIRD_PARTY.md`, catalogue tables) before importing; flagged GPL/unlicensed/NC items must not be imported. Record any import in `THIRD_PARTY.md`.

## References: rules
- Visual references define desired *qualities*; technical references show *possible implementations*. Their language (GDScript, C++, Rust) does not dictate ours.
- Never copy external projects wholesale. Check licences (incl. bundled assets) before importing code or assets.
- Classic-WoW references are analysis only: no Blizzard formats, data or assets, no Noggit code (GPL).
- This is a **single-player** project; skip networking/MMO material.

## Docs
- `docs/MCP_COMPARISON.md` – Godot MCP candidates (none installed; test one at a time).
- `docs/WORLD_EDITOR_REFERENCE.md` – Noggit UX observations.
- `docs/CLASSIC_TERRAIN_NOTES.md` – classic terrain technique notes.
