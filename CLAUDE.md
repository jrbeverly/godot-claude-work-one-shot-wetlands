# CLAUDE.md

Godot 4.5 .NET (C#) project, bootstrapped as a clean runnable base for later environment/world work.
Nothing game-specific exists yet. Do not add gameplay, RPG, streaming, or world-generation systems unless asked.

## Conventions
- Project-owned code is **C#** (`src/`, namespace `GodotWarcraft`, file-scoped namespaces). GDScript only where a third-party addon requires it.
- Layout: `scenes/` (.tscn), `src/` (C#), `assets/`, `addons/` (empty; nothing integrated), `tools/` (scripts), `docs/`, `references/`.
- Keep it simple. Prefer small project-specific tools over generic frameworks. Do not overengineer.
- Keep it buildable: `dotnet build` must pass. Smoke test: `tools/run.sh` (needs `GODOT` env var pointing to the Godot 4.5 .NET binary).
- Once visual work begins, run the project and look at it frequently; do not rely on code review alone.

## References (`references/`)
- Local library of third-party repos, **not committed**. Get it with `tools/fetch-references.sh` (pinned in `references/references.yaml`).
- Start at `references/README.md` (index: role, when to consult, licence, key paths). Read only the reference relevant to the task; never load them wholesale (they are ~2.6 GB; search with Grep/Glob inside one directory).
- Visual references define desired *qualities*. Technical references show *possible implementations*; their language (GDScript, C++, Rust) does not dictate ours.
- Never copy external projects wholesale into the codebase. Check the licence (`THIRD_PARTY.md`, then the repo's LICENSE incl. bundled assets) before importing any code or asset, and record anything imported in `THIRD_PARTY.md`.
- Classic-WoW references (`classic-terrain-analysis/`) are analysis only: no Blizzard formats, data, or assets, and no Noggit code (GPL).

## Docs
- `docs/MCP_COMPARISON.md` – Godot MCP candidates (none installed; test one at a time).
- `docs/WORLD_EDITOR_REFERENCE.md` – Noggit UX observations.
- `docs/CLASSIC_TERRAIN_NOTES.md` – classic terrain technique notes.
