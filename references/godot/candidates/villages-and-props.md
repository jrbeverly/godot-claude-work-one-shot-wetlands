# Villages, buildings, props

Sizes = checkout size (no .git) measured from a shallow clone on 2026-10-04, not the GitHub API (API blocked for non-session repos). Last push = date of last commit on default branch. Stars: unknown (API unavailable). Godot ver = `config/features` of the repo project.godot where present, else unknown.

Already stored: none directly (scatter is in foliage).

| Name | URL | Licence | Godot ver | Last push | Size | Verdict |
|---|---|---|---|---|---|---|
| wavefunctioncollapse-godot | https://github.com/Max-Beier/wavefunctioncollapse-godot | MIT | 4.2 | 2024-03-17 | 5.7 MB | LINK-ONLY |
| infinite_worlds | https://github.com/Lommix/infinite_worlds | MIT | 4.2 | 2023-11-30 | 1.8 MB | LINK-ONLY |
| WaveFunctionCollapse-GodotAddon | https://github.com/Rayogenesis/WaveFunctionCollapse-GodotAddon | MIT | 4.3 | 2025-03-25 | 125 MB | SKIP (2D tiles, large) |
| Procedural-City-Generator-Godot | https://github.com/Carl0sCheca/Procedural-City-Generator-Godot | GPL | 4.2 C# | 2024-01-12 | 21 MB | LINK-ONLY (GPL, flagged; modern city, not village) |
| godot-snappy | https://github.com/jgillich/godot-snappy | MIT | 4.x (project: unknown) | 2024-02-08 | 36 KB | LINK-ONLY |

## Notes
- No maintained Godot 4 procedural *village* generator with a permissive licence was found; hand-placed modular kits plus painting tools (see world-editing.md) fit Classic WoW better than generative layout.
- WFC repos are generic tile/module solvers; useful only if building-kit auto-assembly is ever wanted.
- Procedural-City-Generator is GPL: do not import code. Idea source only.
- godot-snappy: tiny vertex-snapping helper for assembling modular pieces; trivial to re-implement in C#.
- Asset kits (Kenney, Quaternius etc.) were not surveyed (hosted off GitHub).
