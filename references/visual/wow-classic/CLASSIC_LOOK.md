# The Classic look: cross-zone synthesis

Basis: the four zone files, search results (polycount WoW texturing breakdown, MassivelyOP "how WoW made bad graphics look good", ArtStation hand-painted breakdowns) and `docs/CLASSIC_TERRAIN_NOTES.md` (technique analysis). Palette and "feel" claims are general knowledge, not measured.

## Defining traits
- **Hand-painted, low-res textures.** Soft colour blobs with painted-in highlight/shadow/dirt, little high-frequency noise. Texture suggests depth the geometry lacks (search-backed: MassivelyOP, polycount). Ground tiles are repeated, then broken up by blending 3-4 layers (see terrain notes).
- **Vertex-colour / baked lighting.** Terrain and buildings carry painted tints (warmer sunlit patches, cooler hollows) multiplied into albedo; runtime lighting is simple. Result: gentle, shadow-light, "storybook" lighting.
- **Sparse, flat, chunky shapes.** Low poly counts, exaggerated proportions (fat roofs, oversized beams, door frames larger than needed). Silhouette beats detail.
- **Restrained props.** A few repeated prop types per area, placed along fences, roads and farms; plenty of empty ground.
- **Simple skyboxes.** Soft gradient sky, a few painted cloud banks, large pale sun disc; skies are time-of-day keyed colour sets rather than physical sims.
- **Fog as depth device.** Short-ish fog distances tinted per zone and per time of day; hills fade to flat colour planes. Fog hides the draw distance and creates the layered-ridge look.
- **Chunky terrain.** Broad rolling hills and valleys with smoothed slopes, cliffs as simple banded rock; roads are lighter ribbons painted into the ground texture.
- **Zone-coded colour.** Each zone commits to a narrow palette (green / white-blue / ochre / grey-green). Contrast comes from zone transitions.
- **Scale.** Wide, walkable valleys, big trees, and small clusters of buildings far apart; destinations visible from afar (towers, abbey, bridge).

## Palette overview (approximate, from memory)
| Zone | Dominant | Accent | Sky/fog |
|---|---|---|---|
| Elwynn | `#5E8C3A` greens | `#B08A55` road/wood, `#C8B070` fields | `#9DB8D4` blue, light haze |
| Dun Morogh | `#E8EEF4` snow, `#2F4A3A` pine | `#8A7A6A` rock, warm `#E0A050` window light | `#C0CCDA` pale blue-grey |
| Westfall | `#B8A050` dry grass, `#8E7A44` | `#A5603A` barn red, `#6B5B3A` dirt | `#D8C890` warm haze |
| Wetlands | `#556650` murky green, `#6E7468` grey | `#4A5B66` water | `#8C9690` flat grey, heavy mist |

## Approximating in modern Godot (principles only)
- Use **unshaded-ish or low-roughness albedo-dominant materials**; no normal/roughness detail maps on terrain. Paint or author textures at 256-512 px with soft shading; keep texel density modest.
- Drive **ambient light and fog per zone**: strong ambient/sky contribution, soft sun, low shadow strength or short shadow range; tint shadows. Avoid SSAO/SSR/bloom/volumetrics unless they serve the fog look.
- **Fog colour = horizon colour**; tune depth-fog start/end so 3 ridge layers show. Height fog for Wetlands.
- **Vertex colours** (or a large low-res tint map) to vary hue across hills and break tiling; fake baked AO into trunks and eaves.
- Keep **silhouettes bold**: low-poly trees with big canopy masses, simple roofs; use alpha-cut cards for leaves with painted, hue-varied clusters.
- **Place props sparsely and in lines**; leave open ground; use roads/fences to organise space.
- Sky: gradient plus a few painted cloud layers, not physically based atmosphere; keep sun disc readable.
- Compare against greyscale thumbnails for value structure before polishing details.

## Sources
- MassivelyOP, "Casually Classic: How WoW made bad graphics look good" https://massivelyop.com/2021/03/23/casually-classic-how-world-of-warcraft-made-bad-graphics-look-good/ (verified-by-search)
- Polycount, "World of Warcraft Style Texturing breakdown" https://polycount.com/discussion/215804/world-of-warcraft-style-texturing-breakdown (verified-by-search)
- WoW Forever graphics note (modern fog/shadow retrofit for original Azeroth) https://wowforeverbuilds.com/news/wow-forever-graphics-volumetric-fog-real-sun-shadows-and-flowing-water-for-origi (verified-by-search; shows what modern upgrades look like, not the target)
