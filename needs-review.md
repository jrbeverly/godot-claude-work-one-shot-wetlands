# Assets needing review

Use the Decision cells to write **include selected paths**, **reference only**, **defer**, or **skip**. Selected reference copies are summarized below; none has been integrated into the application.

## Selected and pushed assets

This pass selects 88 glTF scenes, 33.95 MiB including previews, with one model format per selection. Full archives and unused formats/colourways remain local.

| Selection / preview sheets | Scenes | Stored size | Choice |
|---|---|---|---|
| [Quaternius Stylized Nature](references/assets/selected/quaternius-stylized-nature/README.md) | 37 | 18.64 MiB | Broadleaf/pine/dead trees, bushes, tufts, ferns, flowers, mushrooms and rocks. Red twisted-tree/bush defaults are palette references; recolour for Elwynn. |
| [Quaternius Medieval Village](references/assets/selected/quaternius-medieval-village/README.md) | 51 | 15.31 MiB | Timber/plaster cottage kit, roofs, stone walls, stairs, windows, fences, chimneys and wagon. |

Selected models were imported and rendered in Godot 4.5.1. Dependencies and retained-file hashes were checked. Quaternius textures are capped at 1024 px; other selections are unchanged. These are reference assets, with application integration left for world work.

## Downloaded archives

All 35 pinned upstream archives are local in `.git/reference-archives/` (about 1.27 GiB compressed). They are not committed or available in a fresh clone. Each **inventory** link lists every omitted path and reason, plus a pinned `archive_url` for downloading the same archive elsewhere. Sizes below are compressed archive sizes, not the size of assets you would include.

Committed snapshots omit images, models, audio, fonts, binaries, LFS pointers and files over 1 MiB. An omitted file is not necessarily an art asset. LFS pointers do not contain the actual asset, and submodule contents were not downloaded. Code licences do not automatically cover bundled art.

List an archive before choosing paths; entries have an upstream root-directory prefix:

```sh
tar -tzf .git/reference-archives/KayKit-Medieval-Hexagon-Pack.tar.gz
```

### Environment and demo assets

| Reference / omitted-file inventory | Local archive / MiB | What to judge | Recorded licence notes | Decision |
|---|---|---|---|---|
| [KayKit-Medieval-Hexagon-Pack](https://github.com/KayKit-Game-Assets/KayKit-Medieval-Hexagon-Pack-1.0) · [inventory](references/assets/KayKit-Medieval-Hexagon-Pack/SNAPSHOT.json) | [KayKit-Medieval-Hexagon-Pack.tar.gz](.git/reference-archives/KayKit-Medieval-Hexagon-Pack.tar.gz) / 7.5 | Buildings, hex tiles, colour variants and texture atlas. Decide whether to keep buildings only and one model format. | CC0; LICENSE.txt retained. | Pending |
| [godot-forest-demo](https://github.com/GamesNotDeveloped/godot-forest-demo) · [inventory](references/godot/foliage/godot-forest-demo/SNAPSHOT.json) | [godot-forest-demo.tar.gz](.git/reference-archives/godot-forest-demo.tar.gz) / 328.1 | Trees, plants, old house, textures, sky HDRI, LUTs and generated data. Select individual objects rather than the whole demo. | CC-BY-4.0; per-model notices; music has separate Pixabay terms. | Pending |
| [godotwind](https://github.com/lihogloglo/godotwind) · [inventory](references/environment-case-studies/godotwind/SNAPSHOT.json) | [godotwind.tar.gz](.git/reference-archives/godotwind.tar.gz) / 728.1 | La Palma terrain data, sky/cloud textures, demo assets and addon binaries. Decide separately on terrain data, textures and binaries. | MIT project; terrain-data provenance and SunshineClouds2 terms unresolved. | Pending |
| [Terrain3D](https://github.com/TokisanGames/Terrain3D) · [inventory](references/godot/terrain/Terrain3D/SNAPSHOT.json) | [Terrain3D.tar.gz](.git/reference-archives/Terrain3D.tar.gz) / 38.6 | Demo terrain regions, ground textures, models and documentation images. Decide whether any demo assets are useful. | MIT code; ambientCG textures recorded as CC0; model terms unresolved. | Pending |
| [scatter](https://github.com/HungryProton/scatter) · [inventory](references/godot/foliage/scatter/SNAPSHOT.json) | [scatter.tar.gz](.git/reference-archives/scatter.tar.gz) / 9.5 | Demo foliage, ground textures, sky texture and Blender source. Decide whether to keep demo art or only plugin source. | MIT code; demo-asset provenance unresolved. | Pending |
| [GodotGrass](https://github.com/2Retr0/GodotGrass) · [inventory](references/godot/foliage/GodotGrass/SNAPSHOT.json) | [GodotGrass.tar.gz](.git/reference-archives/GodotGrass.tar.gz) / 24.4 | Skybox, demo scene and native ImGui binaries. Decide separately on visuals and files needed to run the demo. | MIT project; check bundled assets and addon notices. | Pending |
| [SimpleGrassTextured](https://github.com/IcterusGames/SimpleGrassTextured) · [inventory](references/godot/foliage/SimpleGrassTextured/SNAPSHOT.json) | [SimpleGrassTextured.tar.gz](.git/reference-archives/SimpleGrassTextured.tar.gz) / 0.3 | Grass cards, wind pattern, brush images and editor icons. Decide which textures are useful. | MIT project; check individual texture provenance. | Pending |
| [godot-road-generator](https://github.com/TheDuckCow/godot-road-generator) · [inventory](references/godot/roads/godot-road-generator/SNAPSHOT.json) | [godot-road-generator.tar.gz](.git/reference-archives/godot-road-generator.tar.gz) / 15.6 | Intersection Blender source, road demos, demo images and animations. Decide whether source models are useful. | MIT code; demo-asset provenance unresolved. | Pending |
| [waterways-net](https://github.com/Tshmofen/waterways-net) · [inventory](references/godot/roads/waterways-net/SNAPSHOT.json) | [waterways-net.tar.gz](.git/reference-archives/waterways-net.tar.gz) / 1.6 | Water-normal, foam and flow-noise textures plus editor icons. Decide whether to restore textures alongside reference code. | MIT project; check texture notices. | Pending |
| [GodotOceanWaves](https://github.com/2Retr0/GodotOceanWaves) · [inventory](references/godot/water/GodotOceanWaves/SNAPSHOT.json) | [GodotOceanWaves.tar.gz](.git/reference-archives/GodotOceanWaves.tar.gz) / 29.2 | Water clipmap OBJ, skybox and native ImGui binaries. Decide separately on demo assets and binaries. | MIT project; check bundled assets and addon notices. | Pending |
| [boujie_water_shader](https://github.com/Chrisknyfe/boujie_water_shader) · [inventory](references/godot/water/boujie_water_shader/SNAPSHOT.json) | [boujie_water_shader.tar.gz](.git/reference-archives/boujie_water_shader.tar.gz) / 10.1 | Water texture inputs and obelisk/pedestal demo models and textures. Decide whether to keep water inputs only. | MIT project; check bundled demo-asset notices. | Pending |
| [Sky3D](https://github.com/TokisanGames/Sky3D) · [inventory](references/godot/atmosphere/Sky3D/SNAPSHOT.json) | [Sky3D.tar.gz](.git/reference-archives/Sky3D.tar.gz) / 11.7 | Moon and Milky Way textures, noise textures, demo assets and screenshots. Review third-party texture terms separately. | MIT code; third-party textures need their own notice review. | Pending |
| [godot-4-stylized-sky](https://github.com/gdquest-demos/godot-4-stylized-sky) · [inventory](references/godot/atmosphere/godot-4-stylized-sky/SNAPSHOT.json) | [godot-4-stylized-sky.tar.gz](.git/reference-archives/godot-4-stylized-sky.tar.gz) / 0.7 | Shooting-star texture and day/night preview images. Decide whether to retain previews for reference only. | MIT code; art CC-BY-NC-SA. Existing project guidance excludes importing this art. | Pending |
| [GodotWeatherSystem](https://github.com/mlavik1/GodotWeatherSystem) · [inventory](references/godot/atmosphere/GodotWeatherSystem/SNAPSHOT.json) | [GodotWeatherSystem.tar.gz](.git/reference-archives/GodotWeatherSystem.tar.gz) / 0.3 | Sample-scene GLB and weather screenshots. Decide whether to keep the sample model. | MIT project; check sample-model provenance. | Pending |
| [GameDemo](https://github.com/chickensoft-games/GameDemo) · [inventory](references/godot/controllers/GameDemo/SNAPSHOT.json) | [GameDemo.tar.gz](.git/reference-archives/GameDemo.tar.gz) / 32.6 | Player model, music, coin textures, demo art and screenshots. Decide on individual assets; some large .tres files were also omitted. | MIT project; check individual art/audio notices before reuse. | Pending |
| [phantom-camera](https://github.com/ramokz/phantom-camera) · [inventory](references/godot/controllers/phantom-camera/SNAPSHOT.json) | [phantom-camera.tar.gz](.git/reference-archives/phantom-camera.tar.gz) / 0.7 | Example textures, Nunito fonts and plugin/editor icons. Decide whether to restore example assets or just needed icons. | MIT code; font terms require separate review. | Pending |
| [godot_spatial_gardener](https://github.com/dreadpon/godot_spatial_gardener) · [inventory](references/godot/editing/godot_spatial_gardener/SNAPSHOT.json) | [godot_spatial_gardener.tar.gz](.git/reference-archives/godot_spatial_gardener.tar.gz) / 2.3 | Test landscape and Suzanne meshes, console/UI fonts and plugin icon. Decide whether test assets are useful. | MIT code; fonts and test meshes need individual notice review. | Pending |

### Other reference material

These archives mainly contain tooling, editor images, test fixtures or documentation rather than environment art.

| Reference / omitted-file inventory | Local archive / MiB | What to judge | Recorded licence notes | Decision |
|---|---|---|---|---|
| [GoDotNet.BlankTemplate](https://github.com/Mikeware/GoDotNet.BlankTemplate) · [inventory](references/starters/GoDotNet.BlankTemplate/SNAPSHOT.json) | [GoDotNet.BlankTemplate.tar.gz](.git/reference-archives/GoDotNet.BlankTemplate.tar.gz) / 0.0 | Godot icon. | CC0 template; check icon terms separately. | Pending |
| [GodotGame](https://github.com/chickensoft-games/GodotGame) · [inventory](references/starters/GodotGame/SNAPSHOT.json) | [GodotGame.tar.gz](.git/reference-archives/GodotGame.tar.gz) / 0.0 | Badges, documentation images and icon. | MIT project; reference images, not environment art. | Pending |
| [CSharpGodotTools-Template](https://github.com/CSharpGodotTools/Template) · [inventory](references/starters/CSharpGodotTools-Template/SNAPSHOT.json) | [CSharpGodotTools-Template.tar.gz](.git/reference-archives/CSharpGodotTools-Template.tar.gz) / 0.4 | Stair GLB, console image, example DLL and translation resources. | MIT code; bundled addon/theme/font terms unresolved. | Pending |
| [godot-game-dev-studio](https://github.com/bgrenat/godot-game-dev-studio) · [inventory](references/ai-development/godot-game-dev-studio/SNAPSHOT.json) | [godot-game-dev-studio.tar.gz](.git/reference-archives/godot-game-dev-studio.tar.gz) / 3.7 | Banners and a skill diagram. | LGPL-3.0 with upstream notices; reference only. | Pending |
| [godot-mcp-toolkit](https://github.com/NPGameDev/godot-mcp-toolkit) · [inventory](references/ai-development/godot-mcp-toolkit/SNAPSHOT.json) | [godot-mcp-toolkit.tar.gz](.git/reference-archives/godot-mcp-toolkit.tar.gz) / 0.5 | Plugin icon. | MIT code; logo/name not licensed for reuse. | Pending |
| [godot-skill](https://github.com/BangRocket/godot-skill) · [inventory](references/ai-development/godot-skill/SNAPSHOT.json) | [godot-skill.tar.gz](.git/reference-archives/godot-skill.tar.gz) / 0.1 | No asset selection: all upstream files withheld from the commit. | No redistribution licence; metadata only. | Pending |
| [gai](https://github.com/godot-fun/gai) · [inventory](references/ai-development/gai/SNAPSHOT.json) | [gai.tar.gz](.git/reference-archives/gai.tar.gz) / 34.5 | Test music, sprite sheets, videos, fonts and binaries. Decide whether any are relevant to this environment. | MIT code; bundled fonts/DLL and media terms unresolved. | Pending |
| [mkdevkit-godot-mcp](https://github.com/mkdevkit/godot-mcp) · [inventory](references/godot/mcp/mkdevkit-godot-mcp/SNAPSHOT.json) | [mkdevkit-godot-mcp.tar.gz](.git/reference-archives/mkdevkit-godot-mcp.tar.gz) / 0.1 | No omitted files. | MIT project. | Pending |
| [slangwald-godot-mcp](https://github.com/slangwald/godot-mcp) · [inventory](references/godot/mcp/slangwald-godot-mcp/SNAPSHOT.json) | [slangwald-godot-mcp.tar.gz](.git/reference-archives/slangwald-godot-mcp.tar.gz) / 0.2 | Demo video, screenshot and icon. | MIT project; reference images, not environment art. | Pending |
| [claudot-pub](https://github.com/Claudot-Code/claudot-pub) · [inventory](references/godot/mcp/claudot-pub/SNAPSHOT.json) | [claudot-pub.tar.gz](.git/reference-archives/claudot-pub.tar.gz) / 0.4 | Editor screenshots. | MIT project; reference images, not environment art. | Pending |
| [wow-1121-client-internals](https://github.com/samwhosung/wow-1121-client-internals) · [inventory](references/classic-terrain-analysis/wow-1121-client-internals/SNAPSHOT.json) | [wow-1121-client-internals.tar.gz](.git/reference-archives/wow-1121-client-internals.tar.gz) / 0.2 | No omitted files; documentation only. | CC-BY-4.0 documentation. | Pending |
| [warcraft-rs](https://github.com/wowemulation-dev/warcraft-rs) · [inventory](references/classic-terrain-analysis/warcraft-rs/SNAPSHOT.json) | [warcraft-rs.tar.gz](.git/reference-archives/warcraft-rs.tar.gz) / 1.6 | Format-test binaries and BLP fixtures. | MIT OR Apache-2.0 code; fixtures are analysis only, not environment assets. | Pending |
| [noggit3](https://github.com/wowdev/noggit3) · [inventory](references/classic-terrain-analysis/noggit3/SNAPSHOT.json) | [noggit3.tar.gz](.git/reference-archives/noggit3.tar.gz) / 1.3 | Application icons and bundled font. | GPL-3.0; UX analysis only, no import under current guidance. | Pending |
| [skelerealms](https://github.com/SlashScreen/skelerealms) · [inventory](references/environment-case-studies/skelerealms/SNAPSHOT.json) | [skelerealms.tar.gz](.git/reference-archives/skelerealms.tar.gz) / 0.2 | Octahedron OBJ/MTL and project logos. | MIT; future RPG reference, outside current environment scope. | Pending |
| [godot_heightmap_plugin](https://github.com/Zylann/godot_heightmap_plugin) · [inventory](references/godot/terrain/godot_heightmap_plugin/SNAPSHOT.json) | [godot_heightmap_plugin.tar.gz](.git/reference-archives/godot_heightmap_plugin.tar.gz) / 9.7 | Documentation screenshots, plugin icons and editor images. Decide whether offline documentation needs its images. | MIT project; these are mostly reference/editor images. | Pending |
| [gaea](https://github.com/gaea-godot/gaea) · [inventory](references/godot/terrain/gaea/SNAPSHOT.json) | [gaea.tar.gz](.git/reference-archives/gaea.tar.gz) / 0.1 | Editor icons and logo. Decide whether to restore them if evaluating the addon. | MIT project. | Pending |
| [godot-imposter](https://github.com/zhangjt93/godot-imposter) · [inventory](references/godot/foliage/godot-imposter/SNAPSHOT.json) | [godot-imposter.tar.gz](.git/reference-archives/godot-imposter.tar.gz) / 3.0 | Guide screenshots and editor icons. Decide whether images help explain the source. | MIT project; check image notices. | Pending |
| [func_godot_plugin](https://github.com/func-godot/func_godot_plugin) · [inventory](references/godot/editing/func_godot_plugin/SNAPSHOT.json) | [func_godot_plugin.tar.gz](.git/reference-archives/func_godot_plugin.tar.gz) / 0.1 | Editor icons. Decide whether to restore them if evaluating the addon. | MIT project. | Pending |

## Asset pages only — not downloaded

These links come from the existing asset catalogue. Selected downloads are now noted in their Decision cells. Selected downloads are now noted in their Decision cells. Licence notes are recorded claims, not a new verification of current download terms. Check the actual pack/version you choose.

| Asset page | What to judge | Recorded licence notes | Decision |
|---|---|---|---|
| [Quaternius Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html) · [itch.io](https://quaternius.itch.io/stylized-nature-megakit) · [Godot store](https://store.godotengine.org/asset/quaternius/stylized-nature-megakit/) | Trees, plants, rocks and bushes; select a small foliage set. | CC0 recorded; check download tier/version. | Selected 37 glTF models + dependencies; PNG cap 1024 px. |
| [Quaternius Ultimate Stylized Nature](https://quaternius.com/packs/ultimatestylizednature.html) | Alternative nature set. | Verify pack terms. | Pending |
| [Quaternius Medieval Village Pack](https://quaternius.com/packs/medievalvillage.html) · [MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | Cottages, modular buildings and props. | Pack recorded as CC0; verify MegaKit/version terms separately. | Selected 51 MegaKit glTF pieces + dependencies; older Pack download quota-blocked. User ZIP matches official archive. |
| [Quaternius Ultimate Fantasy RTS](https://quaternius.com/packs/ultimatefantasyrts.html) | Buildings and nature; choose useful environment pieces. | CC0 recorded. | Pending |
| [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) | Simple trees, rocks and terrain pieces. | CC0 recorded. | Pending |
| [Kenney asset catalogue](https://kenney.nl/assets) | Find the Fantasy Town Kit or other building packs; no specific additional download chosen. | Verify the selected pack. | Pending |
| [KayKit Forest Nature Pack](https://kaylousberg.itch.io/kaykit-forest) | Trees, rocks, grass and recolours; decide which tier and variants you need. | CC0 recorded; verify selected download. | Pending |
| [WizP Lowpoly Stylized Fantasy Environment Pack](https://wizp.itch.io/lowpoly-stylized-fantasy-enviroment-pack) | Fantasy buildings and props; assess silhouette and palette. | CC0 claim remains unverified. | Pending |
| [KayKit City Builder Bits](https://github.com/KayKit-Game-Assets/KayKit-City-Builder-Bits-1.0) · [Dungeon Remastered](https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0) | Other building/prop sets mentioned in the catalogue. | Licence and size not checked. | Pending |
| [ambientCG](https://ambientcg.com/) · [licence](https://docs.ambientcg.com/license/) | Choose specific terrain textures or HDRIs; no items selected. | CC0 recorded. | Pending |
| [Poly Haven](https://polyhaven.com/) · [licence](https://polyhaven.com/license) | Choose specific HDRIs, textures or models; no items selected. | CC0 recorded. | Pending |
| [Poly Pizza nature bundle](https://poly.pizza/bundle/Stylized-Nature-MegaKit-T34GZFA0fm) · [village bundle](https://poly.pizza/bundle/Medieval-Village-Pack-NsHhjhlrfY) | Preview existing Quaternius candidates; avoid duplicate imports. | Check each model's terms. | Pending |
| [OpenGameArt nature MegaKit](https://opengameart.org/content/stylized-nature-megakit) · [Nature Kit](https://opengameart.org/content/nature-kit) | Mirrors of already-listed candidates. | Check each download's attribution and licence. | Pending |
| [Sketchfab](https://sketchfab.com/) | Browse only; no model vetted or selected. | Per-model terms. | Pending |

## Record your selections

For a downloaded archive, write exact paths or folders in its Decision cell. For a web pack, record the download tier/version and desired models. Specify whether files belong in the reference library or should be imported into application `assets/`; these are separate decisions.

If including an asset, retain its required licence/attribution and record the import in [THIRD_PARTY.md](THIRD_PARTY.md). Existing guidance keeps Blizzard assets out of the repository and treats Classic-format fixtures and Noggit as analysis only. Compare chosen art against [the visual checklist](references/visual/README.md) and [Classic look notes](references/visual/wow-classic/CLASSIC_LOOK.md).
