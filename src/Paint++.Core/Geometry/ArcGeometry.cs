namespace PaintPP.Core.Geometry;
using System.Numerics;

public readonly record struct Arc(Vector2 Center, float Radius, float StartAngle, float Sweep);

public class ArcGeometry
{
    public static Arc? FromTangent(Vector2 a, Vector2 b, Vector2 direction)
    {
        var chord = b - a;
        var normal = new Vector2(-direction.Y, direction.X);
        float denom = 2f * Vector2.Dot(normal, chord);
        if (MathF.Abs(denom) < 1e-5)
            return null;
        float offset = chord.LengthSquared() / denom;
        var center = a + offset * normal;
        float radius = Vector2.Distance(center, a);

        var toA = a - center;
        var toB = b - center;
        float startAngle = MathF.Atan2(toA.Y, toA.X);
        float endAngle = MathF.Atan2(toB.Y, toB.X);

        bool increasing = toA.X * direction.Y - toA.Y * direction.X > 0;

        float sweep = endAngle - startAngle;
        if (increasing)
            while (sweep <= 0)
                sweep += 2 * MathF.PI;
        else
            while (sweep >= 0)
                sweep -= 2 * MathF.PI;

        return new Arc(center, radius, startAngle, sweep);
    }

    public static IEnumerable<Vector2> Sample(Arc arc, int steps)
    {
        if (steps <= 0)
            throw new ArgumentException("no. steps must be positive");
        for (int i = 0; i <= steps; i++)
        {
            var angle = arc.StartAngle + arc.Sweep * i / steps;
            yield return arc.Center + arc.Radius * new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
    }
}
