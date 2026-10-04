# Stylized rendering, foliage/LOD and performance candidates

Scope: painterly / toon / low-poly shaders, vertex-colour lighting, baked-look materials, post-processing, hand-painted texture splat, billboards/impostors, and a brief performance note. Data gathered 2026-10-04 (GitHub metadata; Godot version from checkout files where noted).

Already stored in the manifest: `scatter`, `godot-forest-demo`, `Terrain3D` (foliage placement and terrain texture splatting are covered there).

Finding: no single open-source "Classic-WoW hand-painted look" package exists for Godot. The look is assembled from: unshaded-ish/low-contrast lighting, vertex-colour baked lighting, simple wind-animated foliage cards, fog, and a tone/colour-grade pass. The repos below provide parts. `wojtekpil/Godot-Octahedral-Impostors` (the well-known one) is **Godot 3** (`config_version=4`); use the Godot 4 plugin instead.

| Name | URL | Licence | Godot ver | Last push | Size (KB) | C# | Verdict |
|---|---|---|---|---|---|---|---|
| zhangjt93/godot-imposter | https://github.com/zhangjt93/godot-imposter | MIT | 4.5 (project features) | 2025-08-15 | 65287 | No (GDScript plugin) | CLONE |
| wojtekpil/Godot-Octahedral-Impostors | https://github.com/wojtekpil/Godot-Octahedral-Impostors | MIT | 3.x | 2021-06-24 | 389717 | No | SKIP (Godot 3, 390 MB) |
| belzecue/initialcon_Godot-Octahedral-Impostors | https://github.com/belzecue/initialcon_Godot-Octahedral-Impostors | MIT | 4.0 (old port) | 2023-09-25 | 349 | No | LINK-ONLY |
| 2Retr0/GodotGrass | https://github.com/2Retr0/GodotGrass | MIT | 4.3 | 2024-08-16 | 25562 | Yes | LINK-ONLY (best-looking grass; next if foliage budget allows) |
| grandmaneedsmorecake/vpainter | https://github.com/grandmaneedsmorecake/vpainter | MIT | 4.3-4.4 | 2025-03-09 | 668 | No | LINK-ONLY (vertex-colour painting, relevant to baked vertex lighting) |
| EMBYRDEV/godot-toon-outline | https://github.com/EMBYRDEV/godot-toon-outline | MIT | unknown (likely 4.x) | 2023-05-25 | 6 | No | LINK-ONLY (single post-process outline shader) |
| ArseniyMirniy/Godot-4-Color-Correction-and-Screen-Effects | https://github.com/ArseniyMirniy/Godot-4-Color-Correction-and-Screen-Effects | MIT | 4.3-4.7 | 2026-09-01 | 118358 | No | LINK-ONLY (grading shaders; large) |
| nekotogd/Godot_BoTW_Toon_Shader | https://github.com/nekotogd/Godot_BoTW_Toon_Shader | CC0-1.0 | unknown (2022) | 2022-07-16 | 78988 | No | LINK-ONLY |
| bramreth/Godot-4-3D-Stylized-Grass | https://github.com/bramreth/Godot-4-3D-Stylized-Grass | MIT | 4.0 | 2023-05-18 | 2557 | No | LINK-ONLY (simple tutorial-scale grass) |
| dreadpon/godot_spatial_gardener | https://github.com/dreadpon/godot_spatial_gardener | MIT | 4.4 | 2025-03-05 | 92061 | No | LINK-ONLY (foliage painting; overlaps `scatter`) |
| puchik/godot-extras | https://github.com/puchik/godot-extras | none declared (unlicensed) | 3 and 4 | 2025-07-01 | 91511 | No (C++) | SKIP (unlicensed, flag) |
| TheMIU/Stylized-Fluffy-Tree-Shader | https://github.com/TheMIU/Stylized-Fluffy-Tree-Shader | none declared (unlicensed) | 4.x | 2025-06-13 | 2121 | No | SKIP (unlicensed) |
| FaRu85/Godot-Foliage | https://github.com/FaRu85/Godot-Foliage | NOASSERTION ("other") | 4.x | 2023-03-03 | 10288 | No | SKIP (licence unclear) |
| EXPWorlds/Godot-Cel-Shader, CaptainProton42/FlexibleToonShaderGD, jocamar/Godot-Post-Process-Outlines | github.com/EXPWorlds/Godot-Cel-Shader etc. | MIT | 3.x era (2020-21) | 2020-2021 | 3108 / 1774 / 1161 | No | SKIP (Godot 3 era) |
| KorinDev/Godot-Post-Process-Plugin | https://github.com/KorinDev/Godot-Post-Process-Plugin | unknown | 4.2 | archived | unknown | No | SKIP (archived) |

## Notes

**zhangjt93/godot-imposter.** Octahedral-impostor baker + runtime shader for Godot 4.x (full/hemi sphere, normal/ORM/depth bakes, shadow variant); README states verified on 4.5. Relevant for distant trees in a large outdoor map (Classic WoW used billboard-like far LOD). 65 MB on GitHub (bundled guide images). Inspect: `addons/Imposter/imposter.gd`, `imposter/materials/shaders/ImpostorShader.gdshader`, `ImpostorShaderShadows.gdshader`, `imposter/materials/*_baker.gdshader`, `guide/`.

**2Retr0/GodotGrass.** Ghost-of-Tsushima style per-blade grass with compute culling, C#. Visually more modern than Classic WoW's grass-card look but a strong C# instancing/LOD/wind reference. Link for now.

**vpainter.** Vertex colour painter; vertex colours multiplied into an unshaded or lightly-lit material is the cheapest way to get baked-looking hand-painted shading.

**Performance basics (brief, no repo needed).** Use Godot built-ins first: `VisibilityRange` (HLOD) on `GeometryInstance3D` with fade, `OccluderInstance3D` (baked occlusion culling), `MultiMeshInstance3D` + visibility ranges for foliage, mesh LODs (auto-generated on import), shadow distance limits and `Environment` fog hiding the cull distance. No third-party occlusion/HLOD package found that beats these for Godot 4.5.
