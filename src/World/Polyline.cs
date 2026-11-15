using System;
using System.Collections.Generic;
using Godot;

namespace GodotWarcraft.World;

/// <summary>Smoothed 2D (xz) polyline with arc length, used by roads, rivers and ridges.</summary>
public sealed class Polyline
{
    public List<Vector2> Points { get; } = new();
    /// <summary>Cumulative arc length at each point.</summary>
    public List<float> Arc { get; } = new();
    public float Length => Arc.Count == 0 ? 0f : Arc[^1];

    public Vector2 Min { get; private set; }
    public Vector2 Max { get; private set; }

    public static Polyline FromControlPoints(IReadOnlyList<float[]> control, float stepM = 3f, bool smooth = true)
    {
        var pts = new List<Vector2>(control.Count);
        foreach (var c in control) pts.Add(new Vector2(c[0], c[1]));
        return Build(pts, stepM, smooth);
    }

    public static Polyline Build(List<Vector2> control, float stepM = 3f, bool smooth = true)
    {
        var pl = new Polyline();
        if (control.Count == 0) return pl;
        if (control.Count == 1 || !smooth)
        {
            foreach (var p in control) pl.Add(p);
            return pl;
        }
        // Centripetal Catmull-Rom through the control points, resampled every ~stepM.
        for (int i = 0; i < control.Count - 1; i++)
        {
            var p0 = control[Math.Max(i - 1, 0)];
            var p1 = control[i];
            var p2 = control[i + 1];
            var p3 = control[Math.Min(i + 2, control.Count - 1)];
            float segLen = p1.DistanceTo(p2);
            int steps = Math.Max(1, (int)MathF.Ceiling(segLen / stepM));
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                pl.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        pl.Add(control[^1]);
        return pl;
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        // Centripetal parameterisation (alpha = 0.5) avoids loops on uneven spacing.
        float Knot(float ti, Vector2 a, Vector2 b) => ti + MathF.Pow(Math.Max(a.DistanceTo(b), 1e-4f), 0.5f);
        float t0 = 0f, t1 = Knot(t0, p0, p1), t2 = Knot(t1, p1, p2), t3 = Knot(t2, p2, p3);
        float tt = Mathf.Lerp(t1, t2, t);
        Vector2 A1 = (t1 - tt) / (t1 - t0) * p0 + (tt - t0) / (t1 - t0) * p1;
        Vector2 A2 = (t2 - tt) / (t2 - t1) * p1 + (tt - t1) / (t2 - t1) * p2;
        Vector2 A3 = (t3 - tt) / (t3 - t2) * p2 + (tt - t2) / (t3 - t2) * p3;
        Vector2 B1 = (t2 - tt) / (t2 - t0) * A1 + (tt - t0) / (t2 - t0) * A2;
        Vector2 B2 = (t3 - tt) / (t3 - t1) * A2 + (tt - t1) / (t3 - t1) * A3;
        return (t2 - tt) / (t2 - t1) * B1 + (tt - t1) / (t2 - t1) * B2;
    }

    private void Add(Vector2 p)
    {
        if (Points.Count == 0) { Min = Max = p; Arc.Add(0f); }
        else
        {
            Arc.Add(Arc[^1] + Points[^1].DistanceTo(p));
            Min = new Vector2(MathF.Min(Min.X, p.X), MathF.Min(Min.Y, p.Y));
            Max = new Vector2(MathF.Max(Max.X, p.X), MathF.Max(Max.Y, p.Y));
        }
        Points.Add(p);
    }

    /// <summary>Closest point on the polyline: distance and arc-length parameter (0..Length).</summary>
    public (float dist, float arc) Closest(Vector2 p)
    {
        float best = float.MaxValue, bestArc = 0f;
        for (int i = 0; i < Points.Count - 1; i++)
        {
            var a = Points[i]; var b = Points[i + 1];
            var ab = b - a;
            float len2 = ab.LengthSquared();
            float t = len2 < 1e-8f ? 0f : Mathf.Clamp((p - a).Dot(ab) / len2, 0f, 1f);
            var q = a + ab * t;
            float d = p.DistanceSquaredTo(q);
            if (d < best) { best = d; bestArc = Mathf.Lerp(Arc[i], Arc[i + 1], t); }
        }
        return (MathF.Sqrt(best), bestArc);
    }

    public Vector2 PointAtArc(float arc)
    {
        if (Points.Count == 0) return Vector2.Zero;
        if (arc <= 0f) return Points[0];
        if (arc >= Length) return Points[^1];
        int lo = 0, hi = Arc.Count - 1;
        while (hi - lo > 1) { int mid = (lo + hi) / 2; if (Arc[mid] <= arc) lo = mid; else hi = mid; }
        float t = (arc - Arc[lo]) / MathF.Max(Arc[hi] - Arc[lo], 1e-5f);
        return Points[lo].Lerp(Points[hi], t);
    }

    public Vector2 TangentAtArc(float arc)
    {
        var a = PointAtArc(MathF.Max(arc - 2f, 0f));
        var b = PointAtArc(MathF.Min(arc + 2f, Length));
        var d = b - a;
        return d.LengthSquared() < 1e-8f ? Vector2.Right : d.Normalized();
    }
}
