# Third-party tracking

## 1. Code integrated into the application
None. (`src/` is project-owned. Build uses only the Godot .NET SDK `Godot.NET.Sdk/4.5.1` and the .NET 8 SDK.)

## 2. Assets integrated into the application
None. (`scenes/Main.tscn` uses built-in primitives; `icon.svg` is project-authored.)

## 3. Addons integrated into the Godot project
None. (`addons/` is empty.)

## 4. Reference material only (`references/`, NOT committed, NOT runtime dependencies)
Fetched by `tools/fetch-references.sh` at the commits in `references/references.yaml`. Storing a reference does not grant permission to reuse it: check the licence (and bundled-asset licences) before importing anything, then move the entry to sections 1-3.

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
| skelerealms | MIT | Future RPG reference only. |

## 5. Candidate asset sources (links only, nothing downloaded)
Quaternius Stylized Nature MegaKit; WizP Lowpoly Stylized Fantasy Environment Pack. See `references/assets/README.md`. Verify licences at import time.

## Tooling used (not distributed)
Godot Engine 4.5.1 .NET (MIT), .NET SDK 8.
