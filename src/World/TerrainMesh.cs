using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Builds the visible terrain as a grid of chunk meshes (for frustum culling) plus one heightmap collider.
/// Vertex COLOR carries four ground-layer weights (grass, forest floor, road, rock); CUSTOM0 carries the
/// baked hue tint (rgb) and the meadow weight (a); CUSTOM1.r carries water depth. The shader blends painterly.
/// </summary>
public static class TerrainMesh
{
    public const int ChunkQuads = 32;

    public static Node3D Build(HeightField f, ShaderMaterial material)
    {
        var root = new Node3D { Name = "Terrain" };
        int chunks = (f.Size - 1) / ChunkQuads;
        for (int cj = 0; cj < chunks; cj++)
        for (int ci = 0; ci < chunks; ci++)
        {
            var mi = new MeshInstance3D
            {
                Name = $"Chunk_{ci}_{cj}",
                Mesh = BuildChunk(f, ci * ChunkQuads, cj * ChunkQuads),
                MaterialOverride = material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
            };
            root.AddChild(mi);
        }

        // One heightmap collider for the whole field (cells are CellSize metres; the shape assumes 1 m, so scale).
        var body = new StaticBody3D { Name = "TerrainBody" };
        var shape = new HeightMapShape3D { MapWidth = f.Size, MapDepth = f.Size, MapData = f.Heights };
        var col = new CollisionShape3D { Shape = shape, Scale = new Vector3(f.CellSize, 1f, f.CellSize) };
        body.AddChild(col);
        root.AddChild(body);
        return root;
    }

    private static ArrayMesh BuildChunk(HeightField f, int i0, int j0)
    {
        int n = ChunkQuads + 1;
        var verts = new Vector3[n * n];
        var normals = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        var colors = new Color[n * n];
        var custom = new float[n * n * 4];
        var custom1 = new float[n * n];
        var indices = new int[ChunkQuads * ChunkQuads * 6];

        for (int j = 0; j < n; j++)
        for (int i = 0; i < n; i++)
        {
            int gi = Math.Min(i0 + i, f.Size - 1), gj = Math.Min(j0 + j, f.Size - 1);
            int k = f.Index(gi, gj);
            int v = j * n + i;
            verts[v] = new Vector3(f.WorldX(gi), f.Heights[k], f.WorldZ(gj));
            normals[v] = f.NormalAt(gi, gj);
            uvs[v] = new Vector2(f.WorldX(gi), f.WorldZ(gj));
            colors[v] = new Color(f.SplatGrass[k], f.SplatForest[k], f.SplatRoad[k], f.SplatRock[k]);
            var t = f.Tint[k];
            custom[v * 4 + 0] = t.R; custom[v * 4 + 1] = t.G; custom[v * 4 + 2] = t.B; custom[v * 4 + 3] = f.SplatMeadow[k];
            custom1[v] = f.WaterDepth[k];
        }

        int idx = 0;
        for (int j = 0; j < ChunkQuads; j++)
        for (int i = 0; i < ChunkQuads; i++)
        {
            int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
            // Alternate the diagonal so slopes read evenly (classic chunky look, no directional ridging).
            // Godot front faces are clockwise when seen from the front (here: from above, +Y).
            if (((i + j) & 1) == 0)
            {
                indices[idx++] = a; indices[idx++] = b; indices[idx++] = c;
                indices[idx++] = b; indices[idx++] = d; indices[idx++] = c;
            }
            else
            {
                indices[idx++] = a; indices[idx++] = d; indices[idx++] = c;
                indices[idx++] = a; indices[idx++] = b; indices[idx++] = d;
            }
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = verts;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Custom0] = custom;
        arrays[(int)Mesh.ArrayType.Custom1] = custom1;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var flags = (Mesh.ArrayFormat)(((long)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift)
                                     | ((long)Mesh.ArrayCustomFormat.RFloat << (int)Mesh.ArrayFormat.FormatCustom1Shift));
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, flags);
        return mesh;
    }
}
