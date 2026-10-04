# CLAUDE.md

Godot 4.5 .NET (C#) project, bootstrapped as a clean runnable base for later environment/world work.
Nothing game-specific exists yet. Do not add gameplay, RPG, streaming, or world-generation systems unless asked.

## Conventions
- Project-owned code is **C#** (`src/`, namespace `GodotWarcraft`, file-scoped namespaces). GDScript only where a third-party addon requires it.
- Layout: `scenes/` (.tscn), `src/` (C#), `assets/`, `addons/` (empty; nothing integrated), `tools/` (scripts), `docs/`, `references/`.
- Keep it simple. Prefer small project-specific tools over generic frameworks. Do not overengineer.
- Keep it buildable: `dotnet build` must pass. Smoke test: `tools/run.sh` (needs `GODOT` env var pointing to the Godot 4.5 .NET binary).
- Once visual work begins, run the project and look at it frequently; do not rely on code review alone.

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
