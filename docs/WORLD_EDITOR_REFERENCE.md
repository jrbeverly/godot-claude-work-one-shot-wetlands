# World Editor Reference (Noggit3 UX observations)

Analysis only. Observations about what a mature terrain/world editor exposes, taken from the pinned clone
`references/classic-terrain-analysis/noggit3` (commit `59e58add`). Paths below are relative to that clone.

## Licence and import rule

- Noggit3 is **GPL-3.0**: `COPYING` is the GNU GPL v3 text; `README.md` says "licensed under GPL3, as found in the COPYING file"; source files carry "licensed under GNU General Public License (version 3)" headers.
- **No Noggit code is to be imported, copied, translated or closely paraphrased** into this project. Only UX/feature observations are recorded here.
- Noggit is built to load Blizzard MPQ data; this project must not use Blizzard formats or assets (see `docs/CLASSIC_TERRAIN_NOTES.md`).

## Editing modes (the top-level mental model)

- One active "mode" at a time, selected by number keys 1-9 or toolbar (`src/noggit/tool_enums.hpp`, `ui/Help.cpp`, `ui/Toolbar.cpp`).
- Modes: ground (raise/lower), flatten/blur, paint (texture), holes, area ID, flags (impassible), water, vertex colour (MCCV), object, scripting (Lua, optional), clearing, chunk mover, shadows.
- Each mode owns a docked property panel with sliders (`ui/slider_spinbox.*`) plus modifier-key shortcuts; panels can be undocked (`MapView.cpp`).
- Modifier convention is consistent across modes: Shift+LMB = add/apply, Ctrl+LMB = remove/inverse, Alt+drag = brush size, Space+drag = speed/pressure (`ui/Help.cpp`).

## Sculpt (`ui/terrain_tool.cpp`, `ui/FlattenTool.cpp`, `tool_enums.hpp`)

- Raise/lower brush falloff types: Flat, Linear, Smooth, Polynomial, Trigonometric, Quadratic, Gaussian, plus a Vertex (per-vertex selection) mode.
- Parameters: outer radius (0-1000), inner radius ratio (0-1, a flat core inside the falloff), speed (0-10).
- Edit filter: normal / only above cursor / only below cursor (`terrain_edit_mode`).
- Vertex mode: select/deselect vertices, flatten selection, orient selection toward cursor, set angle/orientation, centre-of-selection or cursor as pivot.
- Flatten/blur is a separate mode: Flatten types Flat, Linear, Smooth, Origin, Smooth-inner; Blur on Ctrl+LMB; independent lock-up / lock-down toggles; angled flatten plane; "relative point" (F) sets the target height from the cursor.
- Tablet support: pressure can drive radius or speed (`eTabletControl`).

## Texture paint (`ui/texturing_tool.cpp`, `texture_set.hpp`, `ui/TexturePicker.*`, `ui/texture_swapper.*`)

- Brush sliders: Hardness 0-1 (default 0.5), Radius 0-100 (default 15), Pressure 0-1 (default 0.9).
- Extra paint styles: spray brush (own radius, pressure 0-10, optional inner radius), gradient strength (min/max toggle), swap brush (replace texture A by B under brush), animation-flag brush (add/remove texture animation).
- **Hard layer limit of 4 textures per chunk**: `texture_set.hpp` stores 4 layer infos and `std::array<alpha_layer, 4>`; `texture_set.cpp` tries to erase unused layers when the 4th is full. Editor works on float alpha internally ("use 4 alphamaps for easier editing") and packs on save.
- Alpha precision option: "big alpha" (8-bit) versus 4-bit packed maps, with a global convert command (`World::convert_alphamap`).
- Texture UX: picker per chunk (Ctrl+LMB), small docked palette, texture list/browser, fill base layer on an empty chunk, "clear all textures on chunk", remove duplicate textures on tile, "clear" tool with alpha-threshold slider (`ui/clearing_tool.cpp`).

## Liquids (`ui/Water.cpp`, `liquid_*.hpp`, `World.cpp` paintLiquid/setWaterType)

- Paint add (Shift+LMB) / remove (Ctrl+LMB) liquid with a brush; brush size via Alt+drag.
- Liquid type chosen from a dropdown of liquid IDs; opacity mode: Auto, River, Ocean, Custom factor.
- Water height set by: lock position (F), lock mode toggle, Space+MMB to change height; angled (tilted) water plane with angle/orientation.
- Multiple liquid layers per chunk ("Current layer"); middle-click picks the liquid type under the cursor.
- Toggle for cursor/liquid collision so the brush can land on the water surface.

## Object placement (`ui/ObjectEditor.cpp`, `ui/RotationEditor.*`, `gizmo.*`, `ui/asset_browser.*`)

- Objects = models (M2 "doodads") and large structures (WMO). Placed from an asset browser, a text-file list, or imported from an external viewer.
- Paste modes: on terrain, on selection, on camera. Copy/paste/duplicate; "copy rotation, tilt and scale" option.
- Randomisation on paste: random rotation range, random tilt range, random scale range (spin-box ranges).
- Transform: move (MMB), rotate (Shift/Ctrl/Alt + RMB, numpad), uniform scale (+/- and Alt+MMB), reset rotation, numeric Pos/Rotation editor panel, on-screen gizmo with local/world toggle, median pivot for multi-select.
- Snapping: "snap models to ground" on mouse move, set to ground level (PageDown), "rotate along ground" normal alignment. No grid snapping found; snapping is to terrain only.
- Selection: LMB selects chunk or object; multi-selection; hide/show selected, hidden-list management; WMO doodad-set switching (Ctrl+0-9).

## Vertex colours and shading (`ui/shader_tool.cpp`, `ui/shadow_editor.*`)

- "Shader" mode paints per-vertex colour (MCCV) with radius and speed, a colour palette (drag and drop), colour pick from ground (MMB), add to palette (+).
- Shadow mode paints/erases baked shadow, with brush radius; plus a generator with sun pitch and shadow threshold; MMB regenerates shadows for the tile under the cursor.

## Holes, area flags, chunk operations

- Holes: Ctrl+LMB adds a hole (small or big), T removes all holes on tile, Alt+T removes all ground on tile (`ui/Help.cpp`, `World::setHole`).
- Area ID (zone) paint/pick/fill-tile and impassible-flag paint/clear as separate mask-style layers (`ui/ZoneIDBrowser.*`, `World::setAreaID`).
- Chunk mover (`chunk_mover.*`): select chunks (Shift add / Ctrl remove), rotate, mirror horizontally/vertically, preview, paste with height-offset and height blend modes Normal/Min/Max/Add/Subtract; radius 1-500.
- Clearing tool: wipe height, textures or duplicates by chunk or whole tile within a radius.

## Navigation, files, undo

- Free-fly camera: WASD, Q/E vertical, RMB-drag look, arrows rotate, roll keys, four speed presets (Shift+1-4), O/P speed, Home = move to cursor, L = lookat toggle, I = invert mouse (`ui/Help.cpp`, `camera.*`).
- Cursor types: disk, sphere, terrain-projected (`cursor_mode`); toggles for chunk/tile grid lines (F7), contour (F9), wireframe (F10), fog (F12), water/ground/object visibility (F1-F6).
- Minimap dock (M) and a 2D texture/map view (U) (`ui/minimap_widget.*`, `MapView.cpp`); bookmarks menu with F5 to save (`bookmarks.*`).
- Tiles stream in around the camera and are loaded on demand (`World::ensure_tile_is_loaded`, `AsyncLoader.*`); "reload tile under camera" exists as a recovery action.
- Save is explicit and tile-granular: save all changed tiles, save tile at camera, save all loaded (Ctrl+S variants). Unsaved-tile tracking via update queue (`world_tile_update_queue.*`).
- **No undo/redo found**: a search for "undo" in `src/noggit` matches only an icon font header. Recovery is by reload-tile and save discipline.
- Lua scripting API for batch edits (`scripts/docs/README.md`).

## What this implies for a small Godot environment-authoring workflow (observations only)

- A small set of modes, one active at a time, each with a handful of sliders, covers most authoring; modifier-key conventions keep the UI small.
- Brush = radius + inner/hardness + falloff curve + strength; the same four concepts recur for sculpt, paint, colour, water and shadow.
- Texture painting is the most constrained tool in the reference; the 4-layer cap is a storage artefact that drives UX workarounds (auto-erase unused layers, swap brush, cleanup tools).
- Object placement mostly needs: pick asset, drop on terrain, ground-snap, rotate/scale, copy/duplicate, random variation ranges, hide/show.
- Water is edited as an area mask plus a height plane, with type presets and optional tilt.
- Fast camera navigation, visibility toggles, chunk/tile grid overlay and a minimap matter as much as the brushes.
- Explicit saving and the absence of undo are pain points worth noting; they are a gap, not a feature to copy.
