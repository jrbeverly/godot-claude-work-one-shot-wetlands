# AGENTS.md

Godot 4.5 .NET project, C# by default (`src/`), GDScript only for third-party addons. Single-player Classic-WoW-feel fantasy environment (no MMO/networking). Keep it simple; `dotnet build` must pass; no gameplay/RPG systems unless asked. Full conventions: `CLAUDE.md` (source of truth).

## Reference library (read before relevant decisions)
Index: `references/README.md`. Code references are fetched by `tools/fetch-references.sh` (pinned in `references/references.yaml`, not committed). Consult selectively.
- **Visual benchmarks** (what the world should look/feel like): `references/visual/` — start with `README.md` (screenshot-evaluation checklist), `wow-classic/CLASSIC_LOOK.md`, then zone files (Elwynn Forest, Dun Morogh, Westfall, Wetlands). Links + notes only; no Blizzard images stored.
- **Placeholder asset sources + licences**: `references/visual/asset-packs.md`, `references/assets/`.
- **Godot technical references** (terrain, foliage, roads/rivers, props/villages, world editing, third-person, water, atmosphere, stylised rendering): ranked catalogues in `references/godot/candidates/<topic>.md`, cloned repos under `references/godot/<topic>/`.
- **Classic-WoW analysis only**: `docs/CLASSIC_TERRAIN_NOTES.md`, `docs/WORLD_EDITOR_REFERENCE.md`, `references/classic-terrain-analysis/` (no Blizzard formats/assets, no GPL Noggit code).
- **Scope warnings / future**: `references/environment-case-studies/` (godotwind = what not to build; skelerealms = future RPG).
- **Tooling**: `docs/MCP_COMPARISON.md`; agent-doc patterns in `references/ai-development/`.
Check licences (`THIRD_PARTY.md`) before importing anything and log imports there.
