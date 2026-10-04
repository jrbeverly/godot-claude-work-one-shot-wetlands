# Roads, paths, rivers

Sizes = checkout size (no .git) measured from a shallow clone on 2026-10-04, not the GitHub API (API blocked for non-session repos). Last push = date of last commit on default branch. Stars: unknown (API unavailable). Godot ver = `config/features` of the repo project.godot where present, else unknown.

Already stored: godot-road-generator (TheDuckCow), Terrain3D.

| Name | URL | Licence | Godot ver | Last push | Size | Verdict |
|---|---|---|---|---|---|---|
| waterways-net | https://github.com/Tshmofen/waterways-net | MIT | 4.3 (C#) | 2025-09-06 | 76 MB | CLONE |
| Waterways (Arnklit) | https://github.com/Arnklit/Waterways | MIT | Godot 3 on master; godot4_0 branch exists | 2022-08-22 | 45 MB | SKIP (superseded by the .NET port) |
| GodotRoadTool | https://github.com/Nathan-Mrx/GodotRoadTool | none found | 4.3 (C++ GDExtension) | 2026-04-18 | 91 MB | LINK-ONLY (no licence, too big) |
| Goshapes | https://godotassetlibrary.com/asset/fsqRkA/goshapes:-curve-based-modelling-and-level-building | unknown | Godot 4 | unknown | unknown | LINK-ONLY (repo owner not verified) |

## Notes
- **waterways-net**: C# port of Waterways: Bezier river meshes with flow/foam maps. It is the only C#-native item found, so closest to our codebase conventions. Look at: addons/waterways_net/ (river curve, mesh generation, shaders).
- **GodotRoadTool**: claims Terrain3D integration with terrain flattening under roads and embankment smoothing, which is exactly the road-carving problem. No LICENSE found, so read-only reference at most; not cloned.
- Terrain carving for roads is best studied via Terrain3D's own API (already stored) plus godot-road-generator.
