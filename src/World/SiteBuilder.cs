using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Settlements, farms, ruins and landmarks from <see cref="SiteSpec"/>s. Must mark the ground it claims in
/// <see cref="HeightField.SiteClear"/> so foliage keeps away. Runs after terrain/water, before foliage.
/// </summary>
public static class SiteBuilder
{
    public static void Build(WorldRoot world)
    {
        // Implemented by the settlements subsystem.
    }
}
