# Third-person exploration candidates

Scope: single-player third-person character controller, orbit camera (collision, zoom), WoW-style mouse-look / click-to-move, animation blending. Data gathered 2026-10-04 via GitHub search (stars/licence/size/push date from the GitHub repo metadata; Godot version read from `project.godot`/README of a shallow checkout where noted, otherwise "unknown").

Already stored in the manifest (not repeated): `GodotGame`, `GoDotNet.BlankTemplate`, `CSharpGodotTools-Template`, `godotwind`, `skelerealms` (see `references/README.md`).

**Gap found:** no maintained open-source Godot 4 repo implements a true WoW-style control scheme (RMB mouse-look steering, LMB orbit, both-buttons run, click-to-move) in C#. `WilliamPas/GodotFighter3D3PRPG` claims an "MMO-style 3rd-person camera and targeting" but is a 1 KB repo containing only LICENSE+README on the default branch (no code). Plan to write the WoW scheme ourselves on top of SpringArm3D; the references below cover the building blocks.

| Name | URL | Licence | Godot ver | Last push | Size (KB) | C# | Verdict |
|---|---|---|---|---|---|---|---|
| ramokz/phantom-camera | https://github.com/ramokz/phantom-camera | MIT | 4.4+ (README badge) | 2026-09-21 | 29664 | GDScript + C# wrappers (`PhantomCamera3D.cs`) | CLONE |
| chickensoft-games/GameDemo | https://github.com/chickensoft-games/GameDemo | MIT | 4.7 (project.godot features) | 2026-09-29 | 80888 | Yes (C#) | CLONE |
| JeanKouss/godot-third-person-camera | https://github.com/JeanKouss/godot-third-person-camera | MIT | 4.5 | 2025-12-15 | 119986 | GDScript | LINK-ONLY (too big) |
| gdquest-demos/godot-4-3d-third-person-controller | https://github.com/gdquest-demos/godot-4-3d-third-person-controller | NOASSERTION (GDQuest: code MIT, art CC-BY-NC-SA; verify before use) | 4.x | 2026-08-15 | 398411 | GDScript | LINK-ONLY (too big, mixed licence) |
| selgesel/godot4-third-person-controller | https://github.com/selgesel/godot4-third-person-controller | MIT | 4.x (exact unknown) | 2024-07-23 | 107656 | GDScript | LINK-ONLY |
| Snaiel/Godot4ThirdPersonCombatPrototype | https://github.com/Snaiel/Godot4ThirdPersonCombatPrototype | MIT | 4.x (exact unknown) | 2024-11-11 | 175568 | GDScript | SKIP (combat focus, large) |
| gururise/godot-csharp-character-controller | https://github.com/gururise/godot-csharp-character-controller | MIT | 3.x (2021) | 2021-07-06 | 139 | Yes | SKIP (Godot 3) |
| WilliamPas/GodotFighter3D3PRPG | https://github.com/WilliamPas/GodotFighter3D3PRPG | MIT | unknown | 2023-05-11 | 1 | C# (but no code present) | SKIP (empty) |
| NeonfireStudio/godot-advanced-third-person-controller | https://github.com/NeonfireStudio/godot-advanced-third-person-controller | unknown | 4.x | unknown | unknown | GDScript | SKIP (multiplayer-oriented) |

## Notes

**ramokz/phantom-camera** (3588 stars). Cinemachine-style camera addon; has `PhantomCamera3D` with follow modes (the examples include `player_controller_third_person.gd` and reference SpringArm). Ships a C# API (`PhantomCamera.csproj`, `scripts/phantom_camera/PhantomCamera3D.cs`). Useful for camera priority/transition/damping and noise (camera shake); likely more than we need for a plain orbit camera, so treat as REF. Inspect: `addons/phantom_camera/scripts/phantom_camera/phantom_camera_3d.gd`, `PhantomCamera3D.cs`, `addons/phantom_camera/examples/scripts/3D/player_controller_third_person.gd`.

**chickensoft-games/GameDemo** (627 stars). Fully tested third-person C# game: `src/player/` (state-machine `PlayerLogic.State.Alive.Grounded/Airborne.*`), `src/player_camera/PlayerCamera.cs` (+ `PlayerCameraLogic`, `PlayerCameraSettings`; uses a `SpringArmTarget` node). Closest thing to an idiomatic C# controller+camera. Caveats: project is on Godot 4.7 (we target 4.5), and it uses Chickensoft LogicBlocks/introspection infra that CLAUDE.md says not to reproduce by default; read for structure only. GitHub size 80.9 MB (history), shallow checkout is ~18 MB. Inspect: `src/player/PlayerModel.cs`, `src/player/PlayerLogic*.cs`, `src/player_camera/PlayerCamera.cs`.

**JeanKouss/godot-third-person-camera.** Addon `ThirdPersonCamera.gd`: SpringArm3D rig (`RotationPivot/OffsetPivot/CameraSpringArm`), exports for collision mask, zoom, camera shake. Small code, but the repo is 120 MB because of demo assets. If the camera logic is wanted, fetch the single file via raw URL rather than cloning. Inspect (if fetched): `addons/third-person-camera/third_person_camera/ThirdPersonCamera.gd`.

**gdquest-demos/godot-4-3d-third-person-controller.** Polished TPS controller (camera, aim, animations), 1030 stars, but 398 MB and licence is split (GDQuest convention: MIT code, CC-BY-NC-SA art). Link only.

**selgesel/godot4-third-person-controller.** Basic controller, MIT, 108 MB. Redundant with the above.

Not repeated: Godot official demo `godotengine/godot-demo-projects` (3d/platformer, 3d/third_person_shooter style demos) is a large monorepo; link only if animation-blend examples are needed.
