# Foliage, grass, trees

Sizes = checkout size (no .git) measured from a shallow clone on 2026-10-04, not the GitHub API (API blocked for non-session repos). Last push = date of last commit on default branch. Stars: unknown (API unavailable). Godot ver = `config/features` of the repo project.godot where present, else unknown.

Already stored: scatter (ProtonScatter), godot-forest-demo.

| Name | URL | Licence | Godot ver | Last push | Size | Verdict |
|---|---|---|---|---|---|---|
| GodotGrass | https://github.com/2Retr0/GodotGrass | MIT | 4.3 | 2024-08-15 | 50 MB | CLONE |
| SimpleGrassTextured | https://github.com/IcterusGames/SimpleGrassTextured | MIT | 4.x (project: unknown) | 2026-09-17 | 1 MB | CLONE |
| godot-imposter | https://github.com/zhangjt93/godot-imposter | MIT | 4.5 | 2025-08-15 | 3.7 MB | CLONE |
| gdTree3D | https://github.com/JekSun97/gdTree3D | MIT | 4.6 (README: 4.5+) | 2026-09-14 | 32 MB | LINK-ONLY |
| godot-procedural-grass | https://github.com/ChaoticByte/godot-procedural-grass | MIT | 4.3 | 2024-09-06 | 44 MB | LINK-ONLY |
| Godot-4-3D-Stylized-Grass | https://github.com/bramreth/Godot-4-3D-Stylized-Grass | MIT | 4.0 | 2023-05-18 | 2.6 MB | LINK-ONLY |
| Godot-Octahedral-Impostors | https://github.com/wojtekpil/Godot-Octahedral-Impostors | MIT | Godot 3 era (2021) | 2021-06-03 | 0.6 MB | SKIP |
| Grass-Shader-Example (Malidos) | https://github.com/Malidos/Grass-Shader-Example | no licence file | 4.2 | 2024-07-19 | 0.8 MB | SKIP |
| GrassShader (kiwioGIT) | https://github.com/kiwioGIT/GrassShader | no licence file | unknown (2022) | 2022-04-08 | 1.7 MB | SKIP |
| godot_lowpoly_tree | https://github.com/MechanicalFlower/godot_lowpoly_tree | MIT | unknown | 2024-05-19 | 0.1 MB | LINK-ONLY |

## Notes
- **GodotGrass**: Ghost-of-Tsushima-style grass: MultiMesh chunks, compute/shader-driven wind and colour, LOD. Best technical reference for dense, wind-swayed grass. Look at: shaders/, the grass-chunk/clump scripts, README for technique notes.
- **SimpleGrassTextured**: editor brush plugin painting textured-card grass into MultiMesh; small, fits the painted Classic look. Look at: addons/ plugin scripts and its shader.
- **godot-imposter**: octahedral impostor baking for Godot 4 (distant trees/props to single quads). Classic WoW draws many distant trees; relevant to tree LOD. Look at: addons/ baker and shader.
- **gdTree3D**: procedural 3D tree plugin; link only (32 MB, trees look generic; Classic uses hand-modelled low-poly trees).
- Other grass repos are lower quality or unlicensed; unlicensed code must not be copied.
