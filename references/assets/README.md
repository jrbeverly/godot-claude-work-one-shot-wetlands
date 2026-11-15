# Asset references

Selected reference assets are committed under `selected/`; Godot ignores this library via `references/.gdignore`. Copy only chosen files into application `assets/` when building a scene, retaining licence notices and recording imports in `THIRD_PARTY.md`.

| Selection / previews | glTF scenes | Stored size | Classic use | Licence |
|---|---|---|---|---|
| [Quaternius Stylized Nature](selected/quaternius-stylized-nature/README.md) | 37 | 18.64 MiB | Broadleaf/pine/dead trees, bushes, tufts, ferns, flowers, mushrooms and rocks. Red twisted-tree/bush defaults are palette references; recolour for Elwynn. | CC0; Quaternius |
| [Quaternius Medieval Village](selected/quaternius-medieval-village/README.md) | 51 | 15.31 MiB | Timber/plaster cottage kit, roofs, stone walls, stairs, windows, fences, chimneys and wagon. | CC0; Quaternius |
| [KayKit village landmarks](selected/kaykit-medieval-landmarks/README.md) | 8 | 0.98 MiB | Church, smithy, tavern, mine, mills, well and tower; exaggerated silhouettes, blue-roof colourway. | CC0; Kay Lousberg |
| [KayKit forest silhouettes](selected/kaykit-forest/README.md) | 16 | 0.32 MiB | Atlas-based broadleaf/pine/bare trees, bushes, grass and rocks; cleaner/toylike alternative to textured foliage. | CC0; Kay Lousberg |
| [Classic pine references](selected/classic-pines/README.md) | 2 | 0.43 MiB | Two low-poly textured pine packs for Dun Morogh; the second scene contains multiple pine variants. | CC-BY-4.0; Wersaus33 and Vova Truestory |

## Notes

- Each selection includes external glTF buffers/images, a licence notice and `selection.json` with source paths and hashes.
- Quaternius PNG textures are capped at 1024 px; full-resolution originals remain in `.git/reference-archives/` locally. Other selected models/textures are unchanged.
- These are silhouette/material references; palettes and scale still need tuning against the [Classic look notes](../visual/wow-classic/CLASSIC_LOOK.md).
- More candidate pages: [asset-packs.md](../visual/asset-packs.md).
