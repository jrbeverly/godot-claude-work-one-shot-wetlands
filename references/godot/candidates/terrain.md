# Terrain candidates

Sizes = checkout size (no .git) measured from a shallow clone on 2026-10-04, not the GitHub API (API blocked for non-session repos). Last push = date of last commit on default branch. Stars: unknown (API unavailable). Godot ver = `config/features` of the repo project.godot where present, else unknown.

Already stored: Terrain3D (TokisanGames), godotwind, noggit3 (analysis only).

| Name | URL | Licence | Godot ver | Last push | Size | Verdict |
|---|---|---|---|---|---|---|
| HTerrain | https://github.com/Zylann/godot_heightmap_plugin | MIT | 4.6 (project) | 2026-07-30 | 12 MB | CLONE |
| Gaea | https://github.com/gaea-godot/gaea | MIT | 4.7 (project) | 2026-07-27 | 6.5 MB | CLONE |
| godot_voxel | https://github.com/Zylann/godot_voxel | MIT | 4.4 | 2026-10-01 | 52 MB | LINK-ONLY |
| godot-mobile-terrain-editor | https://github.com/stfnlacio/godot-mobile-terrain-editor | none found | 4.7 | 2026-09-13 | 33 MB | LINK-ONLY (no licence) |
| godot-terraformation | https://github.com/rechutox/godot-terraformation | MIT | unknown (stale 2022) | 2022-12-30 | 24 MB | SKIP |
| godot-terrain-edit | https://github.com/BastiaanOlij/godot-terrain-edit | MIT | Godot 3 | 2018-05-17 | 34 MB | SKIP |

## Notes
- **HTerrain**: GDScript, heightmap + splatmap painting, holes, LOD, built-in grass layer, detail maps. Pure GDScript so no C# friction (use via scripting API/nodes). Look at: addons/zylann.hterrain/ (hterrain.gd, hterrain_detail_layer.gd, shaders/), doc/. Useful as a splatmap/detail-layer design reference next to Terrain3D.
- **Gaea**: graph-based procedural generation (noise, erosion-ish, biome/region nodes). Useful for macro-layout/biome heightmap generation that could be exported into Terrain3D. Look at: addons/gaea/ (graph nodes, generators).
- **godot_voxel**: C++ module/GDExtension, volumetric terrain; overkill for classic heightmap zones. Link only.
- **mobile-terrain-editor**: small pure-GDScript splat editor; no LICENSE file, so do not copy.
- godot-terraformation / godot-terrain-edit: stale/Godot 3.
