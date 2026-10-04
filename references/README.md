# Reference library

Local, **pinned** copies of third-party repos for agents to inspect. Not dependencies; not git submodules; not committed.

```
tools/fetch-references.sh          # fetch/verify everything (shallow, ~2.6 GB, ~1 min)
tools/fetch-references.sh Terrain3D godotwind   # fetch only some ids
tools/fetch-references.sh --list   # id, path, url, pinned commit
```

Pins live in `references.yaml` (commit = default-branch HEAD on 2026-10-04). Godot ignores this dir (`.gdignore`); `dotnet build` excludes it.
**Consult selectively**: open the one entry you need, then Grep inside that directory. Licences: see `THIRD_PARTY.md`. Licence facts below were read from each checkout; "?" = not determined.

Roles: **REF** reference only · **DEP?** candidate dependency · **ASSET?** candidate asset source · **ANALYSIS** analysis only (never copy).

## Starters (`starters/`)
| Path | Role | Licence | Consult when / key files / caveats |
|---|---|---|---|
| `GoDotNet.BlankTemplate` | REF | CC0-1.0 | Minimal csproj/.vscode layout: `content/BlankTemplate.csproj`, `content/.vscode/`. Godot 4.1 / net6 – stale, don't inherit versions. |
| `GodotGame` (Chickensoft) | REF | MIT | Mature structure, tests, CI: `Chickensoft.GodotGame.csproj`, `global.json`, `src/Main.cs`, `test/`, `renovate.json`. Heavy infra (GoDotTest, introspection generators) – don't reproduce by default. |
| `CSharpGodotTools-Template` | REF | MIT | Alt. organisation: `Template/Template.csproj`, `Template/Framework/`. Do not inherit netcode/ModLoader/autoload framework; net10; bundled addon licences ?. |

## AI development (`ai-development/`)
| Path | Role | Licence | Notes |
|---|---|---|---|
| `godot-game-dev-studio` | REF | LGPL-3.0 (+ upstream notices in `NOTICE.md`) | `CLAUDE.md`/`AGENTS.md` (~20 lines) route to `SKILL_CATALOG.md`/`skills_index.json` → one `skills/<name>/SKILL.md` → only needed refs; never load all 189 skills. GDScript/4.7 oriented. Don't copy files (LGPL). |
| `godot-mcp-toolkit` | REF | MIT (logo/name not reusable) | `AGENTS.md` = numbered read-first doc map; `CLAUDE.md` = gotchas; `llms.txt` one-screen summary; `docs/dev/contract.md`. Also an MCP candidate (editor plugin only; server is a separate npm repo). |
| `godot-skill` | REF | **none (all rights reserved)** | `godot/SKILL.md` router table; `godot/csharp-reference.md` (.NET editor+SDK, sln/csproj generated at root & committed, rebuild after `[Export]`/`[Signal]` changes, partial classes, 4.5+ needs .NET 8). Read, summarise; do not copy. |
| `gai` | REF | MIT (fonts/DLL ?) | `AGENTS.md` repo map + inline style rules + completion checklist; skills in `.agents/skills/`, scripts in `.ai/`. Organisational reference only; GDScript, large agent UI – don't adopt. |

## Godot technical (`godot/`)
| Path | Role | Licence | Consult when / caveats |
|---|---|---|---|
| `terrain/Terrain3D` | **DEP?** (primary terrain candidate) | MIT (demo: ambientCG CC0 textures; demo models ?) | Terrain storage, sculpt/paint, clipmap LOD, instancer, regions, C# bindings. v`1.1.0-dev`, **needs Godot ≥ 4.5** (`project/addons/terrain_3d/terrain.gdextension`). C++ GDExtension: **no prebuilt binaries in this checkout, `godot-cpp` submodule empty** → get release binaries or build (`doc/docs/building_from_source.md`). C# wrappers in `project/addons/terrain_3d/csharp/` must be regenerated per build (`doc/docs/generating_csharp_bindings.md`). Docs: `doc/docs/`. Not integrated. Don't let its demo visuals set art direction. |
| `foliage/scatter` (ProtonScatter) | DEP? / REF | MIT (demo assets ?) | Procedural placement, exclusion shapes, modifier stack: `addons/proton_scatter/src/{scatter.gd,modifiers/,shapes/}`. v4.2.0, GDScript. |
| `foliage/godot-forest-demo` | REF (high value) | **CC-BY-4.0**; per-model CC-BY (`objects/*/license.txt`); music = Pixabay licence (not CC) | Biomes/grass/billboards/LOD: `addons/gnd_biomes/`, `levels/test_biomes.tscn`, `materials/Foliage_Master_Shader.tres`, `materials/tree_wind.gdshader`, `scenery/TerrainPatch3D.gd`. 682 MB, mostly assets – **do not reuse assets without attribution; project licence does not cover all bundled assets.** Godot 4.6. |
| `roads/godot-road-generator` | REF / DEP? | MIT (bundles GUT, MIT) | Spline roads, intersections: `addons/road-generator/{nodes,procgen}/`, `road_demos/`. v0.9.4, Godot 4.4+, "not feature-complete". Not integrated. |
| `mcp/mkdevkit-godot-mcp` | DEP? | MIT | See `docs/MCP_COMPARISON.md`. `addons/godot_mcp/`, `server/src`. |
| `mcp/slangwald-godot-mcp` | DEP? | MIT | See comparison. `mcp/godot_mcp_server.py`, `addons/mcp_bridge/`. |
| `mcp/claudot-pub` | DEP? | MIT | See comparison. `addons/claudot/`. |

## Classic terrain analysis (`classic-terrain-analysis/`) — ANALYSIS ONLY
Never copy code/assets; no Blizzard formats, data or assets. See `docs/CLASSIC_TERRAIN_NOTES.md`, `docs/WORLD_EDITOR_REFERENCE.md`.
| Path | Licence | Key files |
|---|---|---|
| `wow-1121-client-internals` | CC-BY-4.0 docs (no Blizzard material) | `docs/terrain.md`, `lighting.md`, `collision.md`, `minimap.md` |
| `warcraft-rs` | MIT OR Apache-2.0 | `docs/src/guides/adt-rendering.md`, `file-formats/world-data/wow-adt/` |
| `noggit3` | **GPL-3.0** – never import code | `src/noggit/{MapChunk,MapTile,alphamap,Brush,World}.cpp`, `src/noggit/ui/` (tools). Editor UX reference only. |

## Environment case studies (`environment-case-studies/`)
| Path | Role | Licence | Notes |
|---|---|---|---|
| `godotwind` | REF – learn what **not** to build | MIT (bundled: SunshineClouds2 licence ?, La Palma terrain data source ?) | 1.6 GB (incl. its `.git`; `lapalma_terrain/` 673 MB). Read `docs/ARCHITECTURE.md`, `docs/reference/terrain_research.md`, `docs/reference/streaming_rendering_bible.md`, `docs/systems/distance_rendering.md`. Heavy: cell streaming, 4-tier distance/HLOD/impostors, ocean (14k lines), horizon baking, weather/volumetric clouds, format importers, IK. Skip for a small environment. Includes Terrain3D v1.0.1 prebuilt binaries in `addons/terrain_3d/bin/` (old version). |
| `skelerealms` | REF (future RPG, **not now**) | MIT | GDScript RPG-framework addon: `scripts/{entities,components,ai,schedules}/`. No terrain/LOD. |

## Assets (`assets/`)
Links and licence notes only: `assets/README.md` (Quaternius Stylized Nature MegaKit, WizP Lowpoly Stylized Fantasy Environment Pack).
