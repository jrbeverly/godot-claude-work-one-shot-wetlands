# Asset references

Selected reference assets are committed under `selected/`; Godot ignores this library via `references/.gdignore`. Copy only chosen files into application `assets/` when building a scene, retaining licence notices and recording imports in `THIRD_PARTY.md`.

| Selection / previews | glTF scenes | Stored size | Classic use | Licence |
|---|---|---|---|---|
| [Quaternius Stylized Nature](selected/quaternius-stylized-nature/README.md) | 37 | 18.64 MiB | Broadleaf/pine/dead trees, bushes, tufts, ferns, flowers, mushrooms and rocks. Red twisted-tree/bush defaults are palette references; recolour for Elwynn. | CC0; Quaternius |
| [Quaternius Medieval Village](selected/quaternius-medieval-village/README.md) | 51 | 15.31 MiB | Timber/plaster cottage kit, roofs, stone walls, stairs, windows, fences, chimneys and wagon. | CC0; Quaternius |

## Notes

- Each selection includes external glTF buffers/images, a licence notice and `selection.json` with source paths and hashes.
- Quaternius PNG textures are capped at 1024 px; full-resolution originals remain in `.git/reference-archives/` locally. Other selected models/textures are unchanged.
- These are silhouette/material references; palettes and scale still need tuning against the [Classic look notes](../visual/wow-classic/CLASSIC_LOOK.md).
- More candidate pages and remaining decisions: [needs-review.md](../../needs-review.md) and [asset-packs.md](../visual/asset-packs.md).
