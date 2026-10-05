using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Settlements, farms, ruins and landmarks from <see cref="SiteSpec"/>s. Marks the ground it claims in
/// <see cref="HeightField.SiteClear"/> so foliage keeps away. Runs after terrain/water, before foliage.
/// <para>
/// Rotation convention: <c>rotationDeg</c> is clockwise seen from above with north (-Z) up. 0 = a piece's local +Z (its
/// front/door) faces south, 90 = faces west. Local offsets are rotated with
/// <c>Vector3.Rotated(Vector3.Up, DegToRad(-deg))</c> and nodes get <c>RotationDegrees.Y = -deg</c>, which is the same thing.
/// Everything is placed upright on sampled ground height; buildings never tilt with the slope.
/// </para>
/// </summary>
public static class SiteBuilder
{
    private const float BuildingSink = 0.15f;
    private const float FenceSpan = 2.05f;
    private const float FieldRowSpacing = 1.2f;

    public static void Build(WorldRoot world)
    {
        var kit = new Kit();
        var root = new Node3D { Name = "Sites" };
        world.AddChild(root);
        var rng = new RandomNumberGenerator { Seed = (ulong)world.Seed * 31 + 7 };
        int pieces = 0;
        foreach (var site in world.Layout.Sites)
        {
            var node = new Node3D { Name = string.IsNullOrEmpty(site.Name) ? site.Type : site.Name };
            root.AddChild(node);
            MarkClearDisc(world.Field, site.X, site.Z, site.ClearRadius);
            foreach (var piece in site.Pieces)
            {
                BuildPiece(world, kit, node, site, piece, rng);
                pieces++;
            }
        }
        int cleared = 0;
        foreach (var v in world.Field.SiteClear) if (v > 0.5f) cleared++;
        GD.Print($"Sites: {world.Layout.Sites.Count} sites, {pieces} pieces, {cleared} heightfield vertices cleared of foliage");
    }

    private static void BuildPiece(WorldRoot world, Kit kit, Node3D parent, SiteSpec site, PieceSpec p, RandomNumberGenerator rng)
    {
        var pos = ToWorld(site, p.X, p.Z);
        float yaw = site.RotationDeg + p.RotationDeg;
        switch (p.Kind.ToLowerInvariant())
        {
            case "cottage": BuildCottage(world, kit, parent, site, p, pos, yaw, rng); break;
            case "landmark": BuildLandmark(world, kit, parent, p, pos, yaw, p.Tint != null ? new Color(p.Tint) : site.Type == "ruin" ? new Color("#8F8C86") : null); break;
            case "fence": BuildFence(world, kit, parent, site, p); break;
            case "prop": BuildProp(world, kit, parent, p, pos, yaw); break;
            case "ruinwall": BuildRuinWall(world, kit, parent, p, pos, yaw, rng); break;
            case "standingstone": BuildStandingStone(world, kit, parent, p, pos, yaw, rng); break;
            case "pier": BuildPier(world, kit, parent, p, pos, yaw); break;
            case "brazier": BuildBrazier(parent, p, pos, world.GroundHeight(pos.X, pos.Y)); break;
            case "windowrow": BuildWindowRow(world, parent, p, pos, yaw); break;
            case "boardwalk": BuildBoardwalk(world, kit, parent, p, pos, yaw); break;
            case "tree": BuildTree(world, kit, parent, p, pos, yaw); break;
            case "field": BuildField(world, kit, parent, site, p, pos, yaw, rng); break;
            default: GD.PushWarning($"Sites: unknown piece kind '{p.Kind}' in site '{site.Name}'"); break;
        }
    }

    // ---- pieces -------------------------------------------------------------------------------------

    private static void BuildCottage(WorldRoot world, Kit kit, Node3D parent, SiteSpec site, PieceSpec p, Vector2 pos, float yaw, RandomNumberGenerator rng)
    {
        int w = p.Footprint is { Length: >= 2 } ? p.Footprint[0] : 2;
        int d = p.Footprint is { Length: >= 2 } ? p.Footprint[1] : 2;
        var cottage = CottageBuilder.Build(kit, w, d, p.Style, p.Storeys, p.Chimney, rng);
        // Highest footprint corner keeps the floor above ground on every side; the sink hides the plinth edge.
        float y = MaxCornerHeight(world, pos, yaw, cottage.HalfW, cottage.HalfD) - BuildingSink;
        Place(parent, cottage.Node, pos, y, yaw);
        MarkClearRect(world.Field, pos, yaw, cottage.HalfW, cottage.HalfD, 3f);
        foreach (var prop in cottage.Props)
        {
            var off = Rotate(prop.LocalOffset, yaw);
            var pp = new Vector2(pos.X + off.X, pos.Y + off.Z);
            Place(parent, kit.Instance(prop.Model), pp, world.GroundHeight(pp.X, pp.Y) - 0.05f, yaw + prop.YawDeg);
        }
    }

    private static void BuildLandmark(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw, Color? tint = null)
    {
        string model = p.Model ?? "building_well_blue";
        // KayKit landmarks are modelled ~1-2 m tall; the spec's default scale of 1 means "use the kit default" (8x).
        float scale = Mathf.IsEqualApprox(p.Scale, 1f) ? 8f : p.Scale;
        var inst = kit.Instance(model, tint);
        inst.Scale = Vector3.One * scale;
        var aabb = kit.Get(model)?.Aabb ?? new Aabb(Vector3.Zero, Vector3.One);
        float hw = aabb.Size.X * 0.5f * scale, hd = aabb.Size.Z * 0.5f * scale;
        float y = MaxCornerHeight(world, pos, yaw, hw, hd) - BuildingSink;
        Place(parent, inst, pos, y, yaw);
        MarkClearRect(world.Field, pos, yaw, hw, hd, 3f);
    }

    private static void BuildFence(WorldRoot world, Kit kit, Node3D parent, SiteSpec site, PieceSpec p)
    {
        if (p.Points == null || p.Points.Count < 2) return;
        var pts = new List<Vector2>(p.Points.Count);
        foreach (var c in p.Points) pts.Add(ToWorld(site, c[0], c[1]));
        var fence = new Node3D { Name = "Fence" };
        parent.AddChild(fence);
        for (int i = 0; i < pts.Count - 1; i++)
        {
            var a = pts[i]; var b = pts[i + 1];
            float len = a.DistanceTo(b);
            if (len < 0.3f) continue;
            var dir = (b - a) / len;
            int spans = Math.Max(1, (int)MathF.Round(len / FenceSpan));
            float spanLen = len / spans; // stretch/squash slightly so the line closes exactly on the corner
            for (int s = 0; s < spans; s++)
            {
                var p0 = a + dir * (spanLen * s);
                var p1 = a + dir * (spanLen * (s + 1));
                float y0 = world.GroundHeight(p0.X, p0.Y), y1 = world.GroundHeight(p1.X, p1.Y);
                var mid = (p0 + p1) * 0.5f;
                var inst = kit.Instance("Prop_WoodenFence_Single");
                // Model spans local X: yaw X onto the segment, then pitch about local Z to follow the ground.
                float yawRad = MathF.Atan2(-dir.Y, dir.X);
                float pitch = MathF.Atan2(y1 - y0, spanLen);
                inst.Transform = new Transform3D(
                    new Basis(Vector3.Up, yawRad) * new Basis(Vector3.Back, pitch) * Basis.FromScale(new Vector3(spanLen / FenceSpan, 1, 1)),
                    new Vector3(mid.X, (y0 + y1) * 0.5f - 0.04f, mid.Y));
                fence.AddChild(inst);
            }
        }
    }

    private static void BuildProp(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw)
    {
        var inst = kit.Instance(p.Model ?? "Prop_Crate");
        inst.Scale = Vector3.One * p.Scale;
        Place(parent, inst, pos, world.GroundHeight(pos.X, pos.Y) - 0.05f, yaw);
    }

    /// <summary>A crumbling line of brick wall modules (two back to back so both faces are brick), length along local X.</summary>
    private static void BuildRuinWall(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw, RandomNumberGenerator rng)
    {
        if (p.Intact && p.HeightM > 6f)
        {
            // Keep wall: one massive smooth slab with a crenellated top, like the sandstone keep in the dawn reference.
            var stone = new StandardMaterial3D { AlbedoColor = new Color(p.Tint ?? "#8E7E72"), Roughness = 1f, MetallicSpecular = 0f };
            float gy = world.GroundHeight(pos.X, pos.Y);
            var wall = new Node3D { Name = "KeepWall", Position = new Vector3(pos.X, gy - 0.3f, pos.Y), RotationDegrees = new Vector3(0, -yaw, 0) };
            parent.AddChild(wall);
            wall.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(p.LengthM, p.HeightM + 0.3f, 1.6f) }, Position = new Vector3(0, (p.HeightM + 0.3f) * 0.5f, 0), MaterialOverride = stone });
            for (float lx = -p.LengthM * 0.5f + 0.8f; lx < p.LengthM * 0.5f; lx += 3.2f)
                wall.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.6f, 1.4f, 1.6f) }, Position = new Vector3(lx, p.HeightM + 0.3f + 0.7f, 0), MaterialOverride = stone });
            return;
        }
        int modules = Math.Max(1, (int)MathF.Ceiling(p.LengthM / 2f));
        var line = new Node3D { Name = "RuinWall" };
        parent.AddChild(line);
        // Weathered grey with a hint of moss; the kit's cream plaster/orange plate would read freshly built.
        // Recoloured (greyscale texture x tint) so the kit's orange timber plate and cream plaster end faces
        // become weathered stone too.
        // Mid-grey weathered stone: the recolour path brightens the kit texture 1.45x, so the tint sits well below the palette stone.
        var stoneTint = new Color("#55534E");
        var rubbleTint = new Color("#4C5244");
        for (int k = 0; k < modules; k++)
        {
            float roll = p.Intact ? 1f : rng.Randf();
            if (roll < 0.15f) continue;                                   // missing
            float hScale = roll < 0.55f ? rng.RandfRange(0.4f, 0.7f) : 1f; // stump or full
            // Intact walls stack to the asked height (module 3.12 m); ruins top out around one module.
            hScale *= p.Intact ? p.HeightM / 3.12f : Mathf.Clamp(p.HeightM / 3f, 0.3f, 1.2f);
            float lx = -modules + 1 + 2 * k;
            var off = Rotate(new Vector3(lx, 0, 0), yaw);
            var wp = new Vector2(pos.X + off.X, pos.Y + off.Z);
            float gy = world.GroundHeight(wp.X, wp.Y);
            var seg = new Node3D { Position = new Vector3(wp.X, gy - 0.12f, wp.Y) };
            seg.RotationDegrees = p.Intact ? new Vector3(0, -yaw, 0) : new Vector3(rng.RandfRange(-3f, 3f), -yaw, rng.RandfRange(-3f, 3f));
            seg.Scale = new Vector3(1, hScale, 1);
            var front = kit.Instance("Wall_UnevenBrick_Straight", stoneTint, recolour: true);
            front.Position = new Vector3(0, 0, 0.2f);
            var back = kit.Instance("Wall_UnevenBrick_Straight", stoneTint, recolour: true);
            back.Position = new Vector3(0, 0, -0.2f);
            back.RotationDegrees = new Vector3(0, 180, 0);
            seg.AddChild(front);
            seg.AddChild(back);
            // Brick end caps hide the open module ends; rubble at the foot sells the collapse.
            foreach (float ex in new[] { -1f, 1f })
            {
                var cap = kit.Instance("Corner_Exterior_Brick", stoneTint, recolour: true);
                cap.Position = new Vector3(ex * 1.0f, 0, 0);
                seg.AddChild(cap);
            }
            line.AddChild(seg);
            if (!p.Intact && rng.Randf() < 0.6f)
            {
                var rubble = kit.Instance(rng.Randf() < 0.5f ? "Rock_Medium_2" : "Rock_Medium_3", rubbleTint, recolour: true);
                float rs = rng.RandfRange(0.18f, 0.32f);
                rubble.Scale = new Vector3(rs, rs * 0.7f, rs);
                var side = Rotate(new Vector3(rng.RandfRange(-0.8f, 0.8f), 0, rng.RandfRange(0.7f, 1.3f) * (rng.Randf() < 0.5f ? 1f : -1f)), yaw);
                Place(line, rubble, new Vector2(wp.X + side.X, wp.Y + side.Z), world.GroundHeight(wp.X + side.X, wp.Y + side.Z) - 0.1f, rng.RandfRange(0, 360));
            }
        }
    }

    /// <summary>
    /// Stone jetty: a deck of brick floor tiles on brick side walls, running along local X from the shore end
    /// out over the water. Deck top sits HeightM above the water (default 1.4 m) or on the ground, whichever is higher.
    /// </summary>
    private static void BuildPier(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw)
    {
        float length = MathF.Max(p.LengthM, 4f), width = MathF.Max(p.WidthM > 10f ? 2.4f : p.WidthM, 2f);
        float water = world.Layout.Lake?.WaterLevel ?? 0f;
        float deckTop = MathF.Max(water + (p.HeightM > 0.1f ? p.HeightM : 1.4f), world.GroundHeight(pos.X, pos.Y) + 0.2f);
        var stone = new Color("#8A8780");
        var pier = new Node3D { Name = "Pier", Position = new Vector3(pos.X, deckTop, pos.Y), RotationDegrees = new Vector3(0, -yaw, 0) };
        parent.AddChild(pier);
        int tiles = (int)MathF.Ceiling(length / 2f);
        float tileScaleZ = width / 2f;
        for (int k = 0; k < tiles; k++)
        {
            float lx = -length * 0.5f + 1f + k * 2f;
            var tile = kit.Instance("Floor_UnevenBrick", stone);
            tile.Position = new Vector3(lx, 0, 0);
            tile.Scale = new Vector3(1, 1, tileScaleZ);
            pier.AddChild(tile);
            // Side walls hang from the deck edge down into the water (module is 3.12 m tall; scale to the depth).
            foreach (float sz in new[] { -1f, 1f })
            {
                var wx = Rotate(new Vector3(lx, 0, sz * width * 0.5f), yaw);
                float bed = world.GroundHeight(pos.X + wx.X, pos.Y + wx.Z);
                float h = MathF.Max(deckTop - bed + 0.3f, 0.6f);
                var wall = kit.Instance("Wall_UnevenBrick_Straight", stone);
                wall.Position = new Vector3(lx, -h, sz * width * 0.5f);
                wall.RotationDegrees = new Vector3(0, sz > 0 ? 0 : 180, 0);
                wall.Scale = new Vector3(1, h / 3.12f, 1);
                pier.AddChild(wall);
            }
        }
        // Far end wall.
        {
            var ex = Rotate(new Vector3(length * 0.5f, 0, 0), yaw);
            float bed = world.GroundHeight(pos.X + ex.X, pos.Y + ex.Z);
            float h = MathF.Max(deckTop - bed + 0.3f, 0.6f);
            var end = kit.Instance("Wall_UnevenBrick_Straight", stone);
            end.Position = new Vector3(length * 0.5f, -h, 0);
            end.RotationDegrees = new Vector3(0, -90, 0);
            end.Scale = new Vector3(width / 2f, h / 3.12f, 1);
            pier.AddChild(end);
        }
        MarkClearRect(world.Field, pos, yaw, length * 0.5f, width * 0.5f, 2f);
    }

    /// <summary>A lit brazier: small emissive box plus a warm point light, HeightM above the ground (e.g. on a tower top).</summary>
    private static void BuildBrazier(Node3D parent, PieceSpec p, Vector2 pos, float groundY)
    {
        var glow = new Color("#F0A050");
        var node = new Node3D { Name = "Brazier", Position = new Vector3(pos.X, groundY + p.HeightM, pos.Y) };
        var bowl = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.1f, 0.5f, 1.1f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#3A3330"), Roughness = 1f, MetallicSpecular = 0f },
        };
        var fire = new MeshInstance3D
        {
            Position = new Vector3(0, 0.55f, 0),
            Mesh = new BoxMesh { Size = new Vector3(0.7f, 0.6f, 0.7f) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color("#FFB060"), EmissionEnabled = true, Emission = new Color("#FFB060"), EmissionEnergyMultiplier = 4f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        // Soft additive glow card so the beacon reads from across the vale without bloom.
        var halo = new MeshInstance3D
        {
            Position = new Vector3(0, 1.6f, 0),
            Mesh = new QuadMesh { Size = new Vector2(4.0f, 4.0f) },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(glow.R, glow.G, glow.B, 0.25f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlbedoTexture = GlowTexture(),
                NoDepthTest = false,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        var light = new OmniLight3D { Position = new Vector3(0, 1.4f, 0), LightColor = glow, LightEnergy = 2.0f, OmniRange = 16f, ShadowEnabled = false };
        node.AddChild(bowl); node.AddChild(fire); node.AddChild(halo); node.AddChild(light);
        parent.AddChild(node);
    }

    /// <summary>
    /// A row of lit windows: Count emissive cards along local X at HeightM above the ground, facing local +Z, plus a warm
    /// light every third window. The keep's glowing window strip in the dawn reference, in one piece.
    /// </summary>
    private static void BuildWindowRow(WorldRoot world, Node3D parent, PieceSpec p, Vector2 pos, float yaw)
    {
        var glow = new Color("#F2C465");
        var row = new Node3D { Name = "WindowRow", Position = new Vector3(pos.X, world.GroundHeight(pos.X, pos.Y) + p.HeightM, pos.Y), RotationDegrees = new Vector3(0, -yaw, 0) };
        parent.AddChild(row);
        var mat = new StandardMaterial3D
        {
            AlbedoColor = glow, EmissionEnabled = true, Emission = glow, EmissionEnergyMultiplier = 6f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        int count = Math.Max(1, p.Count);
        for (int k = 0; k < count; k++)
        {
            float lx = (k - (count - 1) * 0.5f) * p.SpacingM;
            row.AddChild(new MeshInstance3D
            {
                Mesh = new QuadMesh { Size = new Vector2(0.55f, 1.3f) },
                Position = new Vector3(lx, 0, 0.3f),
                MaterialOverride = mat,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
            if (k % 3 == 1)
                row.AddChild(new OmniLight3D { Position = new Vector3(lx, 0, 1.5f), LightColor = glow, LightEnergy = 2f, OmniRange = 14f, ShadowEnabled = false });
        }
    }

    /// <summary>
    /// Timber boardwalk over the bog: dark plank deck tiles along local X on posts down to the bed. Deck top sits HeightM
    /// above the marsh (or lake) water, or just above the ground where the walk crosses land.
    /// </summary>
    private static void BuildBoardwalk(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw)
    {
        float length = MathF.Max(p.LengthM, 4f), width = MathF.Max(p.WidthM > 10f ? 2.4f : p.WidthM, 1.6f);
        float water = world.Layout.Marsh?.WaterLevel ?? world.Layout.Lake?.WaterLevel ?? 0f;
        float deckTop = water + (p.HeightM > 0.05f ? p.HeightM : 0.8f);
        // The kit plank texture is already darkened by Kit.Muted, so the tint stays light (lands around #4E4034 on screen).
        var plank = new Color("#C8B49C");
        var postMat = new StandardMaterial3D { AlbedoColor = new Color("#4A3C30"), Roughness = 1f, MetallicSpecular = 0f };
        var walk = new Node3D { Name = "Boardwalk", Position = new Vector3(pos.X, deckTop, pos.Y), RotationDegrees = new Vector3(0, -yaw, 0) };
        parent.AddChild(walk);
        int tiles = (int)MathF.Ceiling(length / 2f);
        for (int k = 0; k < tiles; k++)
        {
            float lx = -length * 0.5f + 1f + k * 2f;
            var wp = Rotate(new Vector3(lx, 0, 0), yaw);
            float groundHere = world.GroundHeight(pos.X + wp.X, pos.Y + wp.Z);
            float top = MathF.Max(deckTop, groundHere + 0.25f) - deckTop;      // lift the deck where the walk crosses land
            var tile = kit.Instance("Floor_WoodDark", plank);
            tile.Position = new Vector3(lx, top, 0);
            tile.Scale = new Vector3(1, 1, width / 2f);
            walk.AddChild(tile);
            foreach (float sz in new[] { -1f, 1f })
            foreach (float sx in new[] { -0.75f, 0.75f })
            {
                var pp = Rotate(new Vector3(lx + sx, 0, sz * (width * 0.5f - 0.15f)), yaw);
                float bed = world.GroundHeight(pos.X + pp.X, pos.Y + pp.Z) - 0.3f;
                float h = deckTop + top - bed;
                if (h < 0.3f) continue;
                walk.AddChild(new MeshInstance3D
                {
                    Mesh = new BoxMesh { Size = new Vector3(0.4f, h, 0.4f) },
                    Position = new Vector3(lx + sx, top - h * 0.5f, sz * (width * 0.5f - 0.15f)),
                    MaterialOverride = postMat,
                });
            }
        }
        MarkClearRect(world.Field, pos, yaw, length * 0.5f, width * 0.5f, 1.5f);
    }

    private static void BuildStandingStone(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw, RandomNumberGenerator rng)
    {
        // HeightM is the stone's height above ground. Tall stones (>= 2.5 m) are slender menhirs, short ones squat
        // boulders. Greyscale the mossy rock texture so the tint sets a weathered warm grey.
        string[] rocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3" };
        float[] rockH = { 2.26f, 1.90f, 2.32f }, rockW = { 3.23f, 3.05f, 3.42f };
        int pick = p.Model != null ? Array.IndexOf(rocks, p.Model) : rng.RandiRange(0, 2);
        if (pick < 0) pick = 0;
        float h = (p.HeightM > 0.1f ? p.HeightM : 3f) * p.Scale;
        float w = h >= 2.5f ? h * rng.RandfRange(0.24f, 0.32f) : h * rng.RandfRange(0.9f, 1.3f);
        var inst = kit.Instance(rocks[pick], new Color("#6A6458"), recolour: true);
        inst.Scale = new Vector3(w / rockW[pick], (h + 0.4f) / rockH[pick], w * 0.8f / rockW[pick]);
        if (p.Fallen)
        {
            // Toppled: lies along local Z, half sunk into the turf.
            var holder = new Node3D();
            inst.Position = new Vector3(0, -0.2f, h * 0.5f);
            inst.RotationDegrees = new Vector3(85f, 0, 0);
            holder.AddChild(inst);
            Place(parent, holder, pos, world.GroundHeight(pos.X, pos.Y) - 0.3f + w * 0.35f, yaw);
            return;
        }
        Place(parent, inst, pos, world.GroundHeight(pos.X, pos.Y) - 0.6f, yaw + rng.RandfRange(-8f, 8f));
    }

    private static void BuildTree(WorldRoot world, Kit kit, Node3D parent, PieceSpec p, Vector2 pos, float yaw)
    {
        Color? tint = string.IsNullOrEmpty(p.LeafTint) ? null : new Color(p.LeafTint);
        // Procedural hero trees: the same styled mesh the biomes use (foliage shader, tint, sway), one MeshInstance3D.
        if (ProceduralFoliage.IsProcedural(p.Model ?? ""))
        {
            var model = ModelLibrary.Get(p.Model!);
            if (model == null) return;
            var mesh = ModelLibrary.GetStyledMesh(model, tint ?? Colors.White, 0.11f, MathF.Max(model.Bounds.End.Y * 0.8f, 1f));
            var mi = new MeshInstance3D { Name = p.Model, Mesh = mesh, Transform = model.Parts[0].Local };
            var holder = new Node3D { Name = p.Model + "_tree" };
            holder.AddChild(mi);
            holder.Scale = Vector3.One * p.Scale;
            Place(parent, holder, pos, world.GroundHeight(pos.X, pos.Y) - 0.3f * p.Scale, yaw);
            return;
        }
        var inst = kit.Instance(p.Model ?? "CommonTree_1", tint, name => name.Contains("Leaves", StringComparison.OrdinalIgnoreCase), recolour: true);
        inst.Scale = Vector3.One * p.Scale;
        // Bright hero tints carry a little self-light so the accent survives fog and shade; dark tints (pines) do not glow.
        if (tint is { } accent && accent.Luminance > 0.45f)
            foreach (var mi in inst.FindChildren("*", "MeshInstance3D", true, false))
                if (mi is MeshInstance3D m && m.Mesh != null)
                    for (int si = 0; si < m.Mesh.GetSurfaceCount(); si++)
                        if ((m.GetActiveMaterial(si) ?? m.Mesh.SurfaceGetMaterial(si)) is StandardMaterial3D std
                            && (std.ResourceName ?? "").Contains("Leaves", StringComparison.OrdinalIgnoreCase)
                            && !std.EmissionEnabled)
                        {
                            std.EmissionEnabled = true;
                            std.Emission = accent * 0.9f;
                            std.EmissionEnergyMultiplier = 0.35f;
                        }
        Place(parent, inst, pos, world.GroundHeight(pos.X, pos.Y) - 0.1f, yaw);
    }

    /// <summary>Crop rows inside a rotated WidthM x DepthM rectangle, 1.2 m apart, as one MultiMesh.</summary>
    private static void BuildField(WorldRoot world, Kit kit, Node3D parent, SiteSpec site, PieceSpec p, Vector2 pos, float yaw, RandomNumberGenerator rng)
    {
        float hw = p.WidthM * 0.5f, hd = p.DepthM * 0.5f;
        MarkClearRect(world.Field, pos, yaw, hw, hd, 2f);
        string crop = (p.Crop ?? "wheat").ToLowerInvariant();
        if (crop == "fallow") return;

        string model = crop == "pumpkin" ? "Bush_Common" : "Grass_Wispy_Tall";
        float step = crop == "pumpkin" ? 1.6f : 0.75f;
        Color tint = crop == "pumpkin" ? new Color("#9AB04A") : new Color("#D9B35A");
        float baseScale = crop == "pumpkin" ? 0.55f : 1.0f;
        var kitModel = kit.Get(model);
        if (kitModel == null || kitModel.Parts.Count == 0) return;
        var part = kitModel.Parts[0];

        var xforms = new List<Transform3D>();
        var yawBasis = new Basis(Vector3.Up, Mathf.DegToRad(-yaw));
        for (float rz = -hd + FieldRowSpacing * 0.5f; rz < hd; rz += FieldRowSpacing)
        {
            for (float rx = -hw + step * 0.5f; rx < hw; rx += step)
            {
                var local = new Vector3(rx + rng.RandfRange(-0.12f, 0.12f), 0, rz + rng.RandfRange(-0.08f, 0.08f));
                var off = yawBasis * local;
                float wx = pos.X + off.X, wz = pos.Y + off.Z;
                float s = baseScale * rng.RandfRange(0.85f, 1.15f);
                var basis = new Basis(Vector3.Up, rng.RandfRange(0, Mathf.Tau)).Scaled(new Vector3(s, s, s));
                xforms.Add(new Transform3D(basis, new Vector3(wx, world.GroundHeight(wx, wz) - 0.03f, wz)));
            }
        }
        if (xforms.Count == 0) return;
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = part.Mesh, InstanceCount = xforms.Count };
        for (int i = 0; i < xforms.Count; i++) mm.SetInstanceTransform(i, xforms[i]);
        parent.AddChild(new MultiMeshInstance3D
        {
            Name = $"Field_{crop}",
            Multimesh = mm,
            MaterialOverride = kit.Tinted(part.Materials[0], tint, recolour: crop == "pumpkin"),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private static Texture2D? _glowTex;
    /// <summary>Radial falloff sprite for additive glow cards.</summary>
    private static Texture2D GlowTexture()
    {
        if (_glowTex != null) return _glowTex;
        const int n = 64;
        var img = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n - 0.5f, dz = (y + 0.5f) / n - 0.5f;
            float r = MathF.Sqrt(dx * dx + dz * dz) * 2f;
            float a = MathF.Pow(Mathf.Clamp(1f - r, 0f, 1f), 2.2f);
            img.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        _glowTex = ImageTexture.CreateFromImage(img);
        return _glowTex;
    }

    // ---- helpers ------------------------------------------------------------------------------------

    /// <summary>Site-local offset to world xz (see the rotation convention in the class summary).</summary>
    private static Vector2 ToWorld(SiteSpec site, float lx, float lz)
    {
        var o = Rotate(new Vector3(lx, 0, lz), site.RotationDeg);
        return new Vector2(site.X + o.X, site.Z + o.Z);
    }

    private static Vector3 Rotate(Vector3 v, float clockwiseDeg) => v.Rotated(Vector3.Up, Mathf.DegToRad(-clockwiseDeg));

    private static void Place(Node3D parent, Node3D inst, Vector2 xz, float y, float yawDeg)
    {
        inst.Position = new Vector3(xz.X, y, xz.Y);
        inst.RotationDegrees = new Vector3(0, -yawDeg, 0);
        parent.AddChild(inst);
    }

    private static float MaxCornerHeight(WorldRoot world, Vector2 centre, float yaw, float hw, float hd)
    {
        float best = float.MinValue;
        foreach (var (sx, sz) in new[] { (1f, 1f), (1f, -1f), (-1f, 1f), (-1f, -1f), (0f, 0f) })
        {
            var off = Rotate(new Vector3(sx * hw, 0, sz * hd), yaw);
            best = MathF.Max(best, world.GroundHeight(centre.X + off.X, centre.Y + off.Z));
        }
        return best;
    }

    /// <summary>1 inside 0.7 R, falling smoothly to 0 at R.</summary>
    private static void MarkClearDisc(HeightField f, float cx, float cz, float radius)
    {
        if (radius <= 0f) return;
        var (gi0, gj0) = f.ToGrid(cx - radius, cz - radius);
        var (gi1, gj1) = f.ToGrid(cx + radius, cz + radius);
        for (int j = Math.Max(0, (int)gj0); j <= Math.Min(f.Size - 1, (int)gj1 + 1); j++)
            for (int i = Math.Max(0, (int)gi0); i <= Math.Min(f.Size - 1, (int)gi1 + 1); i++)
            {
                float dx = f.WorldX(i) - cx, dz = f.WorldZ(j) - cz;
                float t = MathF.Sqrt(dx * dx + dz * dz) / radius;
                float v = 1f - Mathf.SmoothStep(0.7f, 1f, t);
                int k = f.Index(i, j);
                if (v > f.SiteClear[k]) f.SiteClear[k] = v;
            }
    }

    /// <summary>Full clear within <paramref name="margin"/> metres of a rotated rectangle (building footprints, fields).</summary>
    private static void MarkClearRect(HeightField f, Vector2 centre, float yaw, float hw, float hd, float margin)
    {
        float reach = MathF.Sqrt(hw * hw + hd * hd) + margin;
        var (gi0, gj0) = f.ToGrid(centre.X - reach, centre.Y - reach);
        var (gi1, gj1) = f.ToGrid(centre.X + reach, centre.Y + reach);
        // Inverse rotation takes world offsets back into the rectangle's local frame.
        var inv = new Basis(Vector3.Up, Mathf.DegToRad(yaw));
        for (int j = Math.Max(0, (int)gj0); j <= Math.Min(f.Size - 1, (int)gj1 + 1); j++)
            for (int i = Math.Max(0, (int)gi0); i <= Math.Min(f.Size - 1, (int)gi1 + 1); i++)
            {
                var local = inv * new Vector3(f.WorldX(i) - centre.X, 0, f.WorldZ(j) - centre.Y);
                float ox = MathF.Max(MathF.Abs(local.X) - hw, 0f), oz = MathF.Max(MathF.Abs(local.Z) - hd, 0f);
                if (ox * ox + oz * oz <= margin * margin) f.SiteClear[f.Index(i, j)] = 1f;
            }
    }
}
