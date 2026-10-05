using System;
using Godot;

namespace GodotWarcraft.World;

/// <summary>
/// Classic-style atmosphere: a painted gradient sky with soft cloud banks (<c>shaders/sky.gdshader</c>),
/// one gentle sun with tinted shadows, strong sky ambient, and depth fog whose colour matches the
/// horizon so distant ridges flatten into painted planes and the ground meets the sky without a seam.
/// </summary>
public static class Atmosphere
{
    public static void Apply(Node3D parent, WorldLayout L)
    {
        const float tonemapWhite = 6f;
        // The palette holds the colours we want ON SCREEN. Sky and fog go through the filmic tonemapper
        // like everything else, which would turn the pale horizon grey, so pre-invert the curve for them.
        // Paint() returns LINEAR HDR values: the sky shader takes them through plain (non source_color)
        // uniforms, and Environment colours get re-encoded because Godot linearises those itself.
        var fogCol = Paint(L.Col("fog", "#A9BFD3"), tonemapWhite);
        var horizon = Paint(L.Col("skyHorizon", "#C9D7E2"), tonemapWhite);
        var top = Paint(L.Col("skyTop", "#6F95C2"), tonemapWhite);
        var ambient = L.Col("ambient", "#8FA2C4");
        var sunCol = new Color(L.Sun.Color);

        var skyMat = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/sky.gdshader") };
        skyMat.SetShaderParameter("sky_top", top);
        skyMat.SetShaderParameter("sky_horizon", horizon);
        skyMat.SetShaderParameter("fog_color", fogCol);
        skyMat.SetShaderParameter("cloud_light", Paint(new Color("#F2EBDC"), tonemapWhite));
        skyMat.SetShaderParameter("cloud_shadow", Paint(new Color("#7F85A6"), tonemapWhite));
        // Horizon warmth and disc derive from the sun colour so a palette change moves everything together.
        skyMat.SetShaderParameter("sun_warm", Paint(sunCol.Lerp(new Color("#F6E8C4"), 0.7f), tonemapWhite));
        skyMat.SetShaderParameter("sun_color", sunCol.Lerp(Colors.White, 0.5f));
        var sky = new Sky
        {
            SkyMaterial = skyMat,
            // Reflections are disabled below, so the radiance cubemap only feeds nothing we use; keep it tiny
            // and let it update lazily (the visible sky itself is still drawn every frame).
            RadianceSize = Sky.RadianceSizeEnum.Size64,
            ProcessMode = Sky.ProcessModeEnum.Incremental,
        };

        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = sky,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = ambient,
            AmbientLightEnergy = 1.2f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
            TonemapWhite = tonemapWhite,
            TonemapExposure = 1.0f,
            FogEnabled = true,
            FogMode = Godot.Environment.FogModeEnum.Depth,
            FogLightColor = fogCol.LinearToSrgb(),
            FogLightEnergy = 1.0f,
            // Low: the sky shader paints its own warm horizon; scatter would double it.
            FogSunScatter = 0.04f,
            FogDepthBegin = L.Fog.StartM,
            FogDepthEnd = L.Fog.EndM,
            FogDepthCurve = 1.0f,
            // Off: aerial perspective samples the radiance cubemap, which averages the sun into a warm cast and
            // shows a half-screen LOD seam on the software rasteriser. The sky shader blends to the fog colour
            // at the horizon instead, so fogged ridges still meet the sky without a seam.
            FogAerialPerspective = 0.0f,
            // The sky shader fades into the fog colour at the horizon itself; a high value here would wash out the clouds.
            FogSkyAffect = 0.25f,
            // Low mist band (lake surface, the Throat floor) when the layout asks for it; the village flats sit above it.
            // Thin: long grazing views across the Mere pass through hundreds of metres of this band, so keep it a haze.
            FogHeight = L.Fog.HeightFogEnabled ? L.Fog.HeightFogLevel - 2.0f : 0f,
            FogHeightDensity = L.Fog.HeightFogEnabled ? 0.0045f : 0.0f,
            AdjustmentEnabled = true,
            AdjustmentSaturation = 1.0f,
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
            LightColor = sunCol,
            LightEnergy = L.Sun.Energy,
            ShadowEnabled = true,
            ShadowOpacity = 0.6f,
            ShadowBlur = 0.8f,
            DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits,
            DirectionalShadowMaxDistance = 180f,
            DirectionalShadowSplit1 = 0.08f,
            DirectionalShadowSplit2 = 0.22f,
            DirectionalShadowSplit3 = 0.5f,
            DirectionalShadowFadeStart = 0.8f,
            LightAngularDistance = 1.5f,
            // Feeds LIGHT0_DIRECTION/COLOR to the sky shader for the disc and halo.
            SkyMode = DirectionalLight3D.SkyModeEnum.LightAndSky,
        };
        // Azimuth: 0 = light travelling from the north (towards +Z), 90 = from the east (towards -X), clockwise.
        float az = Mathf.DegToRad(L.Sun.AzimuthDeg), el = Mathf.DegToRad(L.Sun.ElevationDeg);
        var dir = new Vector3(-Mathf.Sin(az) * Mathf.Cos(el), -Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)).Normalized();
        sun.LookAtFromPosition(Vector3.Zero - dir * 10f, Vector3.Zero, Vector3.Up);
        parent.AddChild(sun);
    }

    /// <summary>
    /// Linear HDR colour that Godot's filmic tonemapper (with the given white) displays as <paramref name="screen"/>,
    /// an ordinary sRGB-encoded colour such as a palette hex. Exact inverse of tonemap_filmic in Godot's
    /// tonemap.glsl (Hable curve with exposure bias 2), applied after decoding sRGB to linear.
    /// </summary>
    public static Color Paint(Color screen, float white)
    {
        var lin = screen.SrgbToLinear();
        return new Color(InverseFilmic(lin.R, white), InverseFilmic(lin.G, white), InverseFilmic(lin.B, white), screen.A);
    }

    private static float InverseFilmic(float y, float white)
    {
        const float A = 0.22f * 4f, B = 0.30f * 2f, C = 0.10f, D = 0.20f, E = 0.01f, F = 0.30f;
        static float Partial(float x) => (x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F) - E / F;
        float g = Mathf.Clamp(y, 0f, 0.995f) * Partial(white) + E / F;
        // Solve g = (x(Ax+CB)+DE)/(x(Ax+B)+DF) for x: a quadratic with one positive root.
        float qa = A * (g - 1f), qb = g * B - C * B, qc = D * (g * F - E);
        float disc = MathF.Max(qb * qb - 4f * qa * qc, 0f);
        float x = (-qb - MathF.Sqrt(disc)) / (2f * qa);
        return MathF.Max(x, 0f);
    }
}
