using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Assembles a cottage from the Quaternius Medieval Village kit. Local space: origin at the centre of the footprint
/// on the ground, +Z is the front (door side). Footprint is [w, d] in 2 m wall modules, so the walls stand at
/// x = ±w, z = ±d metres. Wall modules are 2 m wide, 3.12 m tall and 0.2 m thick with the exterior face at local +Z,
/// which is why side walls are yawed so their +Z points outward. Roofs have their origin at eave/wall-top level
/// with the eaves hanging ~0.5 m below it, so the roof goes at (storeys * 3.12 - 0.12): measured from the kit's AABBs.
/// </summary>
public static class CottageBuilder
{
    public const float WallH = 3.12f;
    private const float RoofDrop = 0.12f;

    /// <summary>A prop the cottage would like next to it; placed by SiteBuilder on its own ground height.</summary>
    public readonly record struct SideProp(string Model, Vector3 LocalOffset, float YawDeg);

    public sealed class Result
    {
        public Node3D Node = null!;
        /// <summary>Footprint half extents in metres (walls only, no eaves).</summary>
        public float HalfW, HalfD;
        public List<SideProp> Props = new();
    }

    private enum Module { Straight, Accent, Window, Door }

    private sealed record Style(string Straight, string Accent, string Window, string WindowInsert, string Door, string DoorInsert, string Corner, float AccentChance)
    {
        /// <summary>Upper storeys drop the stone base course and lean on timber framing.</summary>
        public Style Upper => Straight == "Wall_Plaster_Straight_Base" ? this with { Straight = "Wall_Plaster_Straight", AccentChance = 0.6f } : this;
    }

    private static readonly Style Plaster = new("Wall_Plaster_Straight_Base", "Wall_Plaster_WoodGrid", "Wall_Plaster_Window_Wide_Flat",
        "Window_Wide_Flat1", "Wall_Plaster_Door_Flat", "Door_2_Flat", "Corner_Exterior_Wood", 0.3f);
    private static readonly Style Timber = new("Wall_Plaster_WoodGrid", "Wall_Plaster_Straight", "Wall_Plaster_Window_Thin_Round",
        "Window_Thin_Round1", "Wall_Plaster_Door_Round", "Door_1_Round", "Corner_Exterior_Wood", 0.2f);
    private static readonly Style Stone = new("Wall_UnevenBrick_Straight", "Wall_UnevenBrick_Straight", "Wall_UnevenBrick_Window_Wide_Round",
        "Window_Wide_Flat1", "Wall_UnevenBrick_Door_Round", "Door_1_Round", "Corner_Exterior_Wood", 0f);

    public static Result Build(Kit kit, int w, int d, string? styleName, int storeys, bool chimney, RandomNumberGenerator rng)
    {
        w = Math.Clamp(w, 1, 6);
        d = Math.Clamp(d, 1, 6);
        storeys = Math.Clamp(storeys, 1, 2);
        var style = styleName?.ToLowerInvariant() switch { "timber" => Timber, "stone" => Stone, _ => Plaster };
        var node = new Node3D { Name = $"Cottage_{w}x{d}_{styleName ?? "plaster"}" };
        var res = new Result { Node = node, HalfW = w, HalfD = d };

        for (int storey = 0; storey < storeys; storey++)
        {
            float y = storey * WallH;
            var st = storey == 0 ? style : style.Upper;
            // Front (+Z), back (-Z), east (+X), west (-X): yaw turns the module's exterior (+Z) outward.
            BuildSide(kit, node, st, rng, w, new Vector3(0, y, d), 0f, storey == 0, isFront: true);
            BuildSide(kit, node, st, rng, w, new Vector3(0, y, -d), 180f, false, isFront: false);
            BuildSide(kit, node, st, rng, d, new Vector3(w, y, 0), 90f, false, isFront: false);
            BuildSide(kit, node, st, rng, d, new Vector3(-w, y, 0), -90f, false, isFront: false);
            foreach (var (sx, sz) in new[] { (1, 1), (1, -1), (-1, 1), (-1, -1) })
                Place(node, kit.Instance(style.Corner), new Vector3(sx * w, y, sz * d), 0f);
            // A jetty board between storeys reads as a timber-framed upper floor without needing the kit's odd overhang pieces.
            if (storey == 1) AddBand(node, w, d, y, 0.22f, 0.18f);
        }

        // Dark interior so windows and the door opening never show sky through back-face-culled walls.
        float totalH = storeys * WallH;
        node.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(2 * w - 0.5f, totalH - 0.1f, 2 * d - 0.5f) },
            Position = new Vector3(0, totalH * 0.5f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#17120E"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        AddRoof(kit, node, w, d, totalH - RoofDrop, chimney, rng);

        // 0-2 props beside the house: a crate by the door, sometimes a wagon along a gable.
        int propCount = rng.RandiRange(0, 2);
        if (propCount >= 1) res.Props.Add(new SideProp("Prop_Crate", new Vector3(w * 0.5f + 0.9f, 0, d + 0.9f), rng.RandfRange(0, 90)));
        if (propCount >= 2) res.Props.Add(new SideProp("Prop_Wagon", new Vector3(-(w + 2.2f), 0, d * 0.4f), rng.RandfRange(-15, 15)));
        return res;
    }

    private static void BuildSide(Kit kit, Node3D parent, Style style, RandomNumberGenerator rng, int count, Vector3 centre, float yawDeg, bool groundFront, bool isFront)
    {
        var modules = new Module[count];
        int door = isFront && groundFront ? count / 2 : -1;
        int windows = count <= 2 ? 1 : 2;
        for (int k = 0; k < count; k++) modules[k] = door == k ? Module.Door : Module.Straight;
        for (int tries = 0; windows > 0 && tries < 20; tries++)
        {
            int k = rng.RandiRange(0, count - 1);
            if (modules[k] != Module.Straight) continue;
            modules[k] = Module.Window; windows--;
        }
        for (int k = 0; k < count; k++)
            if (modules[k] == Module.Straight && rng.Randf() < style.AccentChance) modules[k] = Module.Accent;

        var yawBasis = Basis.FromEuler(new Vector3(0, Mathf.DegToRad(yawDeg), 0));
        for (int k = 0; k < count; k++)
        {
            float lx = -count + 1 + 2 * k; // module centres along the side
            var pos = centre + yawBasis * new Vector3(lx, 0, 0);
            string wall = modules[k] switch
            {
                Module.Door => style.Door,
                Module.Window => style.Window,
                Module.Accent => style.Accent,
                _ => style.Straight,
            };
            Place(parent, kit.Instance(wall), pos, yawDeg);
            if (modules[k] == Module.Window)
                Place(parent, kit.Instance(style.WindowInsert), pos, yawDeg);
            if (modules[k] == Module.Door)
                // Door leaves are modelled hinge-at-origin, 1.1 m wide; the opening is 1.3 m wide centred on the module.
                Place(parent, kit.Instance(style.DoorInsert), pos + yawBasis * new Vector3(-0.55f, 0, -0.1f), yawDeg);
        }
    }

    /// <summary>Thin dark wood band around the walls (jetty / top plate line).</summary>
    private static void AddBand(Node3D parent, int w, int d, float y, float height, float thickness)
    {
        var mat = new StandardMaterial3D { AlbedoColor = new Color("#4A3728"), Roughness = 1f, MetallicSpecular = 0f };
        foreach (var (size, pos) in new[]
        {
            (new Vector3(2 * w + 2 * thickness, height, thickness), new Vector3(0, y, d + thickness * 0.5f)),
            (new Vector3(2 * w + 2 * thickness, height, thickness), new Vector3(0, y, -d - thickness * 0.5f)),
            (new Vector3(thickness, height, 2 * d), new Vector3(w + thickness * 0.5f, y, 0)),
            (new Vector3(thickness, height, 2 * d), new Vector3(-w - thickness * 0.5f, y, 0)),
        })
            parent.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat });
    }

    private static void AddRoof(Kit kit, Node3D parent, int w, int d, float y, bool chimney, RandomNumberGenerator rng)
    {
        // Roof meshes: 4x4 covers 4x4 m, 4x8 is 4 m along X and 8 m along Z, 6x8 is 6 x 8. Longer axis of the house gets the long roof axis.
        bool rotate = w > d;                 // long axis along X -> turn the roof 90 degrees
        float across = 2 * Math.Min(w, d);  // metres across the short axis
        float along = 2 * Math.Max(w, d);
        string roof = across <= 4 ? (along <= 4 ? "Roof_RoundTiles_4x4" : "Roof_RoundTiles_4x8") : "Roof_RoundTiles_6x8";
        float roofAcross = across <= 4 ? 4f : 6f, roofAlong = along <= 4 && across <= 4 ? 4f : 8f;
        float ridge = across <= 4 ? 3.71f : 4.89f, halfRoofW = across <= 4 ? 2.76f : 4.12f;
        var inst = kit.Instance(roof);
        // Odd footprints (e.g. 2x3) stretch the nearest roof; exact ones get scale 1.
        inst.Scale = new Vector3(across / roofAcross, 1f, along / roofAlong);
        Place(parent, inst, new Vector3(0, y, 0), rotate ? 90f : 0f);

        if (!chimney) return;
        // Chimney poking through the roof near a gable end, 1.4 m above the tiles. The roof is a hip whose surface height we
        // approximate linearly from the eave (-0.4 at the overhang edge) to the ridge.
        float ax = across * 0.5f * 0.45f, az = -(along * 0.5f - 1.4f);
        float ridgeHalf = Math.Max(along - across, 0f) * 0.5f;
        float dist = Math.Max(Math.Abs(ax), Math.Max(Math.Abs(az) - ridgeHalf, 0f)) * (halfRoofW / (across * 0.5f + 0.76f));
        float roofH = ridge - (ridge + 0.4f) * dist / halfRoofW;
        var local = rotate ? new Vector3(az, 0, ax) : new Vector3(ax, 0, az);
        var chimneyInst = kit.Instance(rng.Randf() < 0.5f ? "Prop_Chimney" : "Prop_Chimney2");
        Place(parent, chimneyInst, new Vector3(local.X, y + roofH - 1.6f, local.Z), 0f);
    }

    private static void Place(Node3D parent, Node3D inst, Vector3 pos, float yawDeg)
    {
        inst.Position = pos;
        inst.RotationDegrees = new Vector3(0, yawDeg, 0);
        parent.AddChild(inst);
    }
}
