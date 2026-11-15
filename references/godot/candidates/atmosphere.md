# Atmosphere candidates

Scope: sky, day-night cycle, fog / distance fog, volumetric fog, clouds, god rays, weather. Data gathered 2026-10-04 (GitHub metadata; Godot version from checkout files where noted).

Already stored: none of these; `godot-forest-demo` / `godotwind` may include environment setups but are not atmosphere systems.

Performance note: Godot's built-in `Environment` fog + `FogVolume`s cover distance fog and cheap volumetric fog; the repos below are for the sky/time-of-day/weather logic around them.

| Name | URL | Licence | Godot ver | Last push | Size (KB) | C# | Verdict |
|---|---|---|---|---|---|---|---|
| TokisanGames/Sky3D | https://github.com/TokisanGames/Sky3D | MIT (GitHub shows NOASSERTION because LICENSE lists two copyright lines; text is MIT) | 4.5 | 2026-05-19 | 14434 | No (GDScript) | CLONE |
| gdquest-demos/godot-4-stylized-sky | https://github.com/gdquest-demos/godot-4-stylized-sky | Code MIT; art assets CC-BY-NC-SA 4.0 (per README) | 4.2 | 2026-05-16 | 859 | No (shader) | CLONE (shader only; do not import art) |
| mlavik1/GodotWeatherSystem | https://github.com/mlavik1/GodotWeatherSystem | MIT | 4.2 (C#) | 2024-02-14 | 366 | Yes | CLONE |
| kS222138/TCA_Weather_System | https://github.com/kS222138/TCA_Weather_System | MIT | 4.6+ (README) | 2026-04-24 | 89 | No | LINK-ONLY (needs 4.6; very new, unproven) |
| krzmig/godot-simple-sky-project | https://github.com/krzmig/godot-simple-sky-project | MIT | 4.1 | 2024-01-07 | 299 | No | LINK-ONLY |
| paddy-exe/GodotStylizedSkyShader | https://github.com/paddy-exe/GodotStylizedSkyShader | none declared (unlicensed) | 4.x | 2023-05-02 | 9197 | No | SKIP (no licence) |
| RPicster/godot4-demo-desert-light | https://github.com/RPicster/godot4-demo-desert-light | none declared (unlicensed) | 4.x | 2023-05-26 | 42652 | No | LINK-ONLY (volumetric-fog look reference; do not copy) |
| Zokerus/Godot-Dynamic-Weather | https://github.com/Zokerus/Godot-Dynamic-Weather | MIT | 4.4 | 2025-09-20 | 2117 | No | SKIP (18 open issues, redundant) |
| SIsilicon/Godot-God-Rays-Plugin | https://github.com/SIsilicon/Godot-God-Rays-Plugin | MIT | 3.x | 2020-11-27 | 7543 | No | SKIP (Godot 3) |
| ARez2/compositor-effect-lens-effects | https://github.com/ARez2/compositor-effect-lens-effects | MIT | 4.x (CompositorEffect; exact unknown) | 2026-07-05 | 40495 | No | LINK-ONLY (god rays + lens flare as CompositorEffect) |

## Notes

**TokisanGames/Sky3D** (905 stars; same author as Terrain3D, already in the manifest). Atmospheric scattering sky dome, sun/moon with orbital elements, clouds, stars, `TimeOfDay` clock, fog/light ramp. GDScript addon; callable from C#. The obvious day-night backbone. Inspect: `addons/sky_3d/src/Sky3D.gd`, `TimeOfDay.gd`, `SkyDome.gd`, `ScatterLib.gd`, `OrbitalElements.gd`, `assets/shaders` (see `ThirdParty.md` in the addon for bundled asset licences before importing).

**gdquest-demos/godot-4-stylized-sky.** Small stylized sky shader (gradient, clouds, stars). Closer to a painterly Classic look than physical scattering. Shader/code MIT; the demo's art is non-commercial (CC-BY-NC-SA), so take only `sky/stylized_sky.gdshader`. Inspect: `sky/stylized_sky.gdshader`, `sky/examples`.

**mlavik1/GodotWeatherSystem.** C# WeatherController with weighted random weather transitions, per-weather cloud coverage/colour, rain/snow particles, fog, seasons with day-length curves, sky shader. Small, readable, C#, MIT; good shape for a single-player weather state machine. Inspect: `addons/GodotWeatherSystem/scripts/WeatherController.cs`, `WeatherResource.cs`, `shaders/weather_system_sky.gdshader`, `particles/`.

**kS222138/TCA_Weather_System.** Feature-rich (volumetric clouds, wetness, wind, performance controller) but requires Godot 4.6+ and is a ~7-month-old repo; revisit if we upgrade.

**RPicster/godot4-demo-desert-light.** Good visual reference for volumetric fog + SDFGI mood; no licence, so reference by looking only.
