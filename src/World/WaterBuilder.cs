using Godot;

namespace GodotWarcraft.World;

/// <summary>Lake and river surfaces. Reads <see cref="WorldRoot.Layout"/>, <see cref="WorldRoot.Terrain"/> (river polylines) and the heightfield.</summary>
public static class WaterBuilder
{
    public static void Build(WorldRoot world)
    {
        // Implemented by the water subsystem: lake plane at Layout.Lake.WaterLevel, river ribbons along Terrain.Rivers.
    }
}
