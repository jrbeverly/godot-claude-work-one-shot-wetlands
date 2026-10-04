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

## Materials
| File | Use | Palette idea | Suggested world tile |
|---|---|---|---|
| `grass.png` | default ground | mid green turf blobs, lighter tufts, deep pockets, warm patches | 6-7 m |
| `meadow.png` | dry/golden biome ground | yellower, wind-leaned clumps, sparse tiny flower specks | 6-7 m |
| `forest_floor.png` | under tree canopy | dark green-brown, moss cushions, ochre leaf drifts, root shadows | 8 m |
| `dirt_road.png` | road ribbon | tan, lighter worn pools, soft pebbles, one faint rut | 5 m |
| `rock.png` | steep slopes / cliffs (triplanar) | banded strata: lit top edges, dark undersides, vertical fissures | 11 m |
| `snow.png` | above the snow line | near-white drifts with soft blue hollows | 9 m |
| `mud.png` | lake / river bed | dark olive-brown, silt streaks along the current | 6 m |

`meadow.png` and `mud.png` are not yet wired into the terrain material; they are ready for a
biome layer and the water subsystem respectively.

## Import settings
The `.import` files enable `mipmaps/generate=true` (the shader samples with
`filter_linear_mipmap_anisotropic`; without mipmaps distant ground sparkles). Keep that when
re-importing. Compression stays lossless (mode 0) so the soft gradients do not band.
