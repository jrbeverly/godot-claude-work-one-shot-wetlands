# Ground textures

Hand-painted-style, seamlessly tileable 512x512 albedo tiles for `shaders/terrain.gdshader`.
All are generated, deterministically, by `tools/paint_textures.py` (python3 + numpy + Pillow):

```sh
python3 tools/paint_textures.py --previews     # rewrites every tile + 2x2 previews in screenshots/
python3 tools/paint_textures.py --only grass   # one material
```

Do not hand-edit the PNGs; change the painter and regenerate. Previews land in
`screenshots/texture_preview_<name>.png` (2x2 tiled, to check seams) and `texture_preview_sheet.png`.

## Style rules baked into the painter
- Soft overlapping brush dabs from a 3-5 tone palette per material; every dab is a small dome lit
  from the top-left with a soft shadow pocket to its lower-right ("baked light").
- A low-frequency colour drift across the tile hides repetition; no high-frequency grain.
- Painted on a torus (coordinates wrap), so tiles are seamless by construction.
- Albedo only. The shader sets ROUGHNESS 1 / SPECULAR 0; there are no normal or roughness maps.
- Tiles are kept a notch duller than the colour we want on screen, because sun + sky light sit on top.
  Grass in particular: sun, sky and the vertex tint all push saturation up, so the grass tile is painted
  olive (mean ~ (93,115,69), B/G ~0.6). A greener tile rendered neon lime in-engine.
- Rock strata are flat-sided slabs (superellipse brush, `power=4`) with a hard crack-tone shadow under each
  lip, so cliffs read as angular bedding rather than cobbles.

## Materials
| File | Use | Palette idea | Suggested world tile |
|---|---|---|---|
| `grass.png` | turf on hummock crowns and higher ground | dull olive marsh turf: wide soft tufts, deep pockets, yellow-leaning patches | 6-7 m |
| `meadow.png` | sedge band around the water (`groundTexture: "meadow"` and the marsh splat rule) | straw-olive wind-combed blades, cream/bog-blue flower specks | 6.5 m |
| `forest_floor.png` | peat: the bog floor and hummock flanks, and under dark woods | dark red-brown earth, olive moss cushions, straw litter, root shadows | 8 m |
| `dirt_road.png` | road ribbon | grey-green trodden mud, paler worn pools, grey pebbles, one faint rut | 5 m |
| `rock.png` | steep slopes / cliffs (triplanar) | pale violet-grey strata: lit top edges, dark undersides, vertical joints; sits near the fog colour so walls loom | 11 m |
| `snow.png` | above `terrain.snowLine` (tinted by palette `tintSnow`) | near-white drifts; in the Wetlands zone this layer is the pale upper cliff wall (pink-grey tint, 120 m line, 50 m blend) | 9 m |
| `mud.png` | lake / river bed | dark grey-olive, silt streaks along the current | 6 m |

Foliage cards (moss curtains, leaf clumps, reeds, pale bark) are painted by `tools/paint_foliage.py` into `assets/textures/foliage/`.

`meadow.png` is the fifth terrain layer (`tex_meadow`, weight in `CUSTOM0.a`, see `shaders/terrain.gdshader`).
`mud.png` is not yet wired; it is ready for the water subsystem (lake / river beds).

## Import settings
The `.import` files enable `mipmaps/generate=true` (the shader samples with
`filter_linear_mipmap_anisotropic`; without mipmaps distant ground sparkles). Keep that when
re-importing. Compression stays lossless (`compress/mode=0`, and `detect_3d/compress_to=0` so the editor
does not silently switch the tiles to VRAM compression on first 3D use) so the soft gradients do not band.
