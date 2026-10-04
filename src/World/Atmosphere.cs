using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Classic-style atmosphere: soft gradient sky, one gentle sun with tinted shadows, strong sky ambient,
/// and depth fog whose colour matches the horizon so distant ridges flatten into painted planes.
/// </summary>
public static class Atmosphere
{
    public static void Apply(Node3D parent, WorldLayout L)
    {
        var fogCol = L.Col("fog", "#A9BFD3");
        var horizon = L.Col("skyHorizon", "#C9D7E2");
        var top = L.Col("skyTop", "#6F95C2");
        var ambient = L.Col("ambient", "#B9C8D6");

        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = top,
            SkyHorizonColor = horizon,
            SkyCurve = 0.12f,
            GroundBottomColor = horizon.Darkened(0.25f),
            GroundHorizonColor = horizon,
            SunAngleMax = 18f,
            SunCurve = 0.08f,
            UseDebanding = true,
        };
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = sky },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = ambient,
            AmbientLightEnergy = 1.15f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            TonemapWhite = 6f,
            TonemapExposure = 1.0f,
            FogEnabled = true,
            FogMode = Godot.Environment.FogModeEnum.Depth,
            FogLightColor = fogCol,
            FogLightEnergy = 1.0f,
            FogSunScatter = 0.08f,
            FogDepthBegin = L.Fog.StartM,
            FogDepthEnd = L.Fog.EndM,
            FogDepthCurve = 1.0f,
            FogAerialPerspective = 0.6f,
            FogSkyAffect = 0.55f,
            FogHeightDensity = L.Fog.HeightFogEnabled ? 0.0f : 0.0f,
            AdjustmentEnabled = true,
            AdjustmentSaturation = 1.05f,
            AdjustmentContrast = 0.98f,
            SsaoEnabled = false,
            SsilEnabled = false,
            SsrEnabled = false,
            GlowEnabled = false,
        };
        parent.AddChild(new WorldEnvironment { Name = "WorldEnvironment", Environment = env });

        var sun = new DirectionalLight3D
        {
            Name = "Sun",
            LightColor = new Color(L.Sun.Color),
            LightEnergy = L.Sun.Energy,
            ShadowEnabled = true,
            ShadowOpacity = 0.55f,
            ShadowBlur = 1.6f,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
            DirectionalShadowMaxDistance = 260f,
            DirectionalShadowSplit1 = 0.08f,
            DirectionalShadowSplit2 = 0.22f,
            DirectionalShadowSplit3 = 0.5f,
            DirectionalShadowFadeStart = 0.8f,
            LightAngularDistance = 1.5f,
        };
        // Azimuth: 0 = light travelling from the north (towards +Z), 90 = from the east (towards -X), clockwise.
        float az = Mathf.DegToRad(L.Sun.AzimuthDeg), el = Mathf.DegToRad(L.Sun.ElevationDeg);
        var dir = new Vector3(-Mathf.Sin(az) * Mathf.Cos(el), -Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)).Normalized();
        sun.LookAtFromPosition(Vector3.Zero - dir * 10f, Vector3.Zero, Vector3.Up);
        parent.AddChild(sun);
    }
}
