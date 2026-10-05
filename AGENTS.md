# AGENTS.md

Godot 4.5 .NET project, C# by default (`src/`), GDScript only for third-party addons. Single-player Classic-WoW-feel fantasy environment (no MMO/networking): the zone **Fallowmere** is generated at startup from `assets/world/layout.json` (design: `docs/WORLD_BIBLE.md`). Keep it simple; `dotnet build` must pass; no gameplay/RPG systems unless asked. Full conventions: `CLAUDE.md` (source of truth). Look at the world with `tools/screenshot.sh` before and after visual changes (`tools/shot_stats.py` for value structure; `EXTRA="--palette=res://assets/world/palette_night.json"` for the night mood).

## Reference library (read before relevant decisions)
Index: `references/README.md`. Code references are fetched by `tools/fetch-references.sh` (pinned in `references/references.yaml`, source snapshots committed). Consult selectively.
- **Visual benchmarks** (what the world should look/feel like): `references/visual/` — start with `README.md` (screenshot-evaluation checklist), `wow-classic/CLASSIC_LOOK.md`, then zone files (Elwynn Forest, Dun Morogh, Westfall, Wetlands). The three reference frames in `references/visual/wow-classic/*.jpg` (dawn willows and cliff keep, purple dusk giant tree, night road through reeds) are what the zone is tuned against: view them, judge renders against them, never copy pixels. Everything else is links + notes.
- **Placeholder asset sources + licences**: `references/visual/asset-packs.md`, `references/assets/`.
- **Godot technical references** (terrain, foliage, roads/rivers, props/villages, world editing, third-person, water, atmosphere, stylised rendering): ranked catalogues in `references/godot/candidates/<topic>.md`, cloned repos under `references/godot/<topic>/`.
- **Classic-WoW analysis only**: `docs/CLASSIC_TERRAIN_NOTES.md`, `docs/WORLD_EDITOR_REFERENCE.md`, `references/classic-terrain-analysis/` (no Blizzard formats/assets, no GPL Noggit code).
- **Scope warnings / future**: `references/environment-case-studies/` (godotwind = what not to build; skelerealms = future RPG).
- **Tooling**: `docs/MCP_COMPARISON.md`; agent-doc patterns in `references/ai-development/`.
Check licences (`THIRD_PARTY.md`) before importing anything and log imports there.
