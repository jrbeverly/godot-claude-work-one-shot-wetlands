# World editing tools

Sizes = checkout size (no .git) measured from a shallow clone on 2026-10-04, not the GitHub API (API blocked for non-session repos). Last push = date of last commit on default branch. Stars: unknown (API unavailable). Godot ver = `config/features` of the repo project.godot where present, else unknown.

Already stored: scatter, Terrain3D.

| Name | URL | Licence | Godot ver | Last push | Size | Verdict |
|---|---|---|---|---|---|---|
| Spatial Gardener | https://github.com/dreadpon/godot_spatial_gardener | MIT | 4.4 | 2025-03-05 | 9.7 MB | CLONE |
| func_godot | https://github.com/func-godot/func_godot_plugin | MIT | 4.x (project: unknown) | 2026-09-23 | 0.7 MB | CLONE |
| Cyclops Level Builder | https://github.com/blackears/cyclopsLevelBuilder | MIT-style (LICENSE.txt) | 4.7 | 2026-09-21 | 155 MB | LINK-ONLY (too big) |
| Ultimate-Asset-Placer | https://github.com/nakulsehdev550-lab/Ultimate-Asset-Placer | MIT | 4.7 | 2026-10-01 | 8.5 MB | LINK-ONLY (very new, unproven) |
| godot-map-builder | https://github.com/callmemhz/godot-map-builder | MIT | 4.7 | 2026-07-16 | 4 MB | LINK-ONLY |
| GodotPlugin-MeshPainter | https://github.com/StrayEddy/GodotPlugin-MeshPainter | CC0 | 4.0 | 2022-09-30 | 156 MB | SKIP |
| godot-brushy | https://github.com/DarkKilauea/godot-brushy | MIT | Godot 3 era | 2021-12-30 | 0.9 MB | SKIP |

## Notes
- **Spatial Gardener**: GDScript brush painting of plants/props on arbitrary surfaces with MultiMesh, density/slope/scale/rotation rules. The model for a Classic-style prop/foliage paint workflow. Look at: addons/dreadpons.spatial_gardener/ (arrangement, painting, octree, brush logic).
- **func_godot**: Trenchbroom/Qodot successor; builds geometry and entities from .map files. Lets us block out buildings/interiors in Trenchbroom and place entities. Look at: addons/func_godot/ (map parsing, entity definitions, FGD export).
- **Cyclops**: in-editor convex brush modeling; good blocking tool but 155 MB checkout (likely demo assets).
- **Ultimate-Asset-Placer / godot-map-builder**: both target Godot 4.7 and are very recent; unproven. Not C#-specific; all of these are GDScript editor plugins used from the editor, which is fine for a C# project.
