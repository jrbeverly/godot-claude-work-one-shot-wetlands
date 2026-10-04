# Third-party tracking

## 1. Code integrated into the application
None. (`src/` is project-owned. Build uses only the Godot .NET SDK `Godot.NET.Sdk/4.5.1` and the .NET 8 SDK.)

## 2. Assets integrated into the application
None. (`scenes/Main.tscn` uses built-in primitives; `icon.svg` is project-authored.)

## 3. Addons integrated into the Godot project
None. (`addons/` is empty.)

## 4. Reference material only (`references/`, committed source snapshots, NOT runtime dependencies)
Source snapshots downloaded from upstream archives at the commits in `references/references.yaml`. Upstream licence/notice files are retained. `SNAPSHOT.json` records omitted assets and binaries; the unlicensed `godot-skill` contains metadata only. Full archives are local in `.git/reference-archives/` for later review. Storing a reference does not grant permission to reuse it: check the licence (and bundled-asset licences) before importing anything, then move the entry to sections 1-3.

| Reference | Licence | Reuse notes |
|---|---|---|
| GoDotNet.BlankTemplate | CC0-1.0 | Free to reuse. |
| GodotGame (Chickensoft) | MIT | Attribution/notice if code copied. |
| CSharpGodotTools Template | MIT | Bundled addon/theme/font licences not determined. |
| godot-game-dev-studio | LGPL-3.0 (+ upstream notices) | Reference only; don't copy files. |
| godot-mcp-toolkit | MIT | Logo/banner/name not licensed for reuse. |
| godot-skill | **No licence found** | All rights reserved; read only. |
| gai | MIT | Bundled fonts / prebuilt DLL licences not checked. |
| Terrain3D | MIT | Demo textures CC0 (ambientCG); demo model licences not checked. Candidate dependency (not integrated). |
| ProtonScatter | MIT | Demo asset provenance not checked. Candidate dependency (not integrated). |
| godot-road-generator | MIT | Bundles GUT (MIT); demo asset provenance not checked. |
| godot-forest-demo | CC-BY-4.0 | Attribution required; per-model CC-BY; music under Pixabay licence; addon `debug_menu`/`SimpleGrassTextured` MIT. |
| mkdevkit / slangwald / claudot MCP | MIT each | Not installed. |
| wow-1121-client-internals | CC-BY-4.0 (docs) | Analysis only. Blizzard trademarks; no Blizzard material in repo. |
| warcraft-rs | MIT OR Apache-2.0 | Analysis only; do not implement Blizzard formats. |
| noggit3 | **GPL-3.0** | UX study only; never import code. |
| godotwind | MIT | Bundled SunshineClouds2 (licence ?), terrain data source (?), older Terrain3D binaries. |
| godot_heightmap_plugin (HTerrain), gaea, GodotGrass, SimpleGrassTextured, godot-imposter, waterways-net, godot_spatial_gardener, func_godot_plugin | MIT each | Reference/candidate only; not integrated. |
| phantom-camera, GameDemo (Chickensoft), GodotOceanWaves, boujie_water_shader, Sky3D, GodotWeatherSystem | MIT each | Not integrated. GameDemo targets Godot 4.7 and Chickensoft infrastructure. Sky3D's GitHub licence field says NOASSERTION but the LICENSE text is MIT. |
| godot-4-stylized-sky | Code MIT; **art CC-BY-NC-SA** | Shader ideas only; never import its art (non-commercial). |
| KayKit Medieval Hexagon Pack 1.0 (Kay Lousberg) | CC0-1.0 | Candidate asset source; source metadata committed; asset files omitted, not imported. |
| skelerealms | MIT | Future RPG reference only. |

## 5. Candidate asset sources (links only, nothing downloaded)
Quaternius Stylized Nature MegaKit; WizP Lowpoly Stylized Fantasy Environment Pack. See `references/assets/README.md`. Verify licences at import time.

## Tooling used (not distributed)
Godot Engine 4.5.1 .NET (MIT), .NET SDK 8.

## Visual references and catalogues (tracked in git)
`references/visual/` and `references/godot/candidates/` are original notes written for this project, with links to third-party material. Selected third-party reference assets are listed below. No Blizzard screenshots or art are stored. Link-only candidates that were flagged GPL/unlicensed in the catalogues (e.g. a GPL C# city generator, unlicensed grass/sky shader repos) must not be imported.

## Selected reference assets (committed; no application integration)

| Selection | Licence / attribution | Stored changes |
|---|---|---|
| [Quaternius Stylized Nature](references/assets/selected/quaternius-stylized-nature/README.md) | CC0; Quaternius; notice retained in selection. | PNG textures capped at 1024 px; glTF geometry/materials unchanged. |

Source download paths, archive hashes and individual file hashes are recorded in each `selection.json`. Selection previews were rendered in Godot 4.5.1; the same asset attribution applies to these images. Application code/assets/addons in sections 1–3 remain unchanged.
