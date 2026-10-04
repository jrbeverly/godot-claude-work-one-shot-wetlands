# Water candidates

Scope: stylized lakes/rivers/ponds, shorelines/foam, flow, ocean shaders, low-cost options. Data gathered 2026-10-04 (GitHub metadata; Godot version from checkout files where noted).

Already stored in the manifest: `Terrain3D`, `godot-forest-demo`, `godotwind` (these may contain their own water; not re-evaluated here).

Key finding: `Arnklit/Waterways` (1333 stars, MIT) is **Godot 3 only** (`config_version=4`), despite the repo being active. The Godot 4 / .NET port is `Tshmofen/waterways-net`.

| Name | URL | Licence | Godot ver | Last push | Size (KB) | C# | Verdict |
|---|---|---|---|---|---|---|---|
| 2Retr0/GodotOceanWaves | https://github.com/2Retr0/GodotOceanWaves | MIT | 4.3 (Forward+) | 2025-04-29 | 37255 | Yes | CLONE |
| Chrisknyfe/boujie_water_shader | https://github.com/Chrisknyfe/boujie_water_shader | MIT | 4.1+ (tested 4.1.1) | 2026-04-16 | 11185 | No (GDScript+shader) | CLONE |
| Tshmofen/waterways-net | https://github.com/Tshmofen/waterways-net | MIT | 4.3 / .NET 8 | 2025-09-06 | unknown (shallow checkout ~149 MB) | Yes | LINK-ONLY (size; see note) |
| Arnklit/Waterways | https://github.com/Arnklit/Waterways | MIT | 3.x | 2025-12-17 | 188914 | No | SKIP (Godot 3) |
| tessarakkt/godot4-oceanfft | https://github.com/tessarakkt/godot4-oceanfft | MIT | 4.x | 2025-11-19 | 97216 | No | LINK-ONLY (large, redundant with GodotOceanWaves) |
| antzGames/Godot-Foam-Water | https://github.com/antzGames/Godot-Foam-Water | MIT | 4.5-4.6 (project 4.6, Compatibility) | 2026-02-25 | 22745 | No | LINK-ONLY (shore-foam trick; fetch the shader) |
| Namey5/godot-interactive-water | https://github.com/Namey5/godot-interactive-water | MIT | 4.3 | 2025-02-06 | 10989 | No | LINK-ONLY |
| Malidos/Stylized-Water-Shader | https://github.com/Malidos/Stylized-Water-Shader | CC0-1.0 | 4.x | 2024-12-11 | 21 | No | LINK-ONLY (single `ocean.gdshader`, depth absorption/refraction) |
| bramreth/Godot4WaterShader | https://github.com/bramreth/Godot4WaterShader | MIT | 4.0 era | 2022-12-01 | 2736 | No | LINK-ONLY |
| paddy-exe/Godot-3D-Stylized-Water | https://github.com/paddy-exe/Godot-3D-Stylized-Water | MIT | unknown (Godot 3 era, 2021-22) | 2022-08-08 | 29214 | No | SKIP |
| hgouveia/godot-toon-water-shader-port | https://github.com/hgouveia/godot-toon-water-shader-port | MIT | unknown | 2024-09-30 | 37818 | No | SKIP |
| LesusX/Water-Shader | https://github.com/LesusX/Water-Shader | MIT | 4.x | 2025-09-29 | 31251 | No | SKIP (8 stars, Gerstner demo) |

## Notes

**2Retr0/GodotOceanWaves** (3298 stars). FFT ocean on compute shaders with clipmap LOD meshes, foam, sea spray. C#, MIT. It is a heavy, realistic open-sea solution: more than Classic-WoW lakes need, but the clipmap water plane + `water.gdshader` (depth fade, foam, fresnel) are the best-quality reference, and it is the only C# one. Requires Forward+ and compute (not Compatibility). Inspect: `assets/shaders/spatial/water.gdshader`, `assets/water/water.gd`, `wave_generator.gd`, `wave_cascade_parameters.gd`, `assets/shaders/compute/*.glsl`. Ignore `addons/imgui-godot`.

**Chrisknyfe/boujie_water_shader** (187 stars). Single water shader plugin: ocean/ice albedo + foam textures, depth-based colour, Mobile renderer friendly. Cheap and tweakable; the better starting point for ponds/lakes. Inspect: `addons/boujie_water_shader/shader/water.gdshader`, `boujie_water_shader.gd`, `prefabs/`, `example/`.

**Tshmofen/waterways-net.** River mesh from Bezier curve with correct flow direction, foam, waterfall; Godot 4 / .NET 8 port of Waterways, C# (`RiverManager.cs`, `BaseMeshGenerator.cs`, `RiverFloatSystem.cs`, `Shaders/river.gdshader`). Best fit for river/stream authoring in C#. Not cloned only because the repo exceeds the 80 MB budget (test images); a sparse checkout of `addons/waterways_net` would be small, but the fetch script is shallow-full. Recommend raising this with the manifest owner (sparse path support) or cloning anyway if the size cap is relaxed. Flow baking was removed from this port (see its README, branch `original-net` keeps the old features but is unsupported).

**antzGames/Godot-Foam-Water.** Edge/shore foam from depth buffer, Compatibility renderer. Take the technique, not the repo.

**Malidos/Stylized-Water-Shader.** 21 KB CC0 shader with colour absorption by depth and refraction; trivial to copy-adapt (CC0, still record in THIRD_PARTY.md if used).
