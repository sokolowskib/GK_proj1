namespace PaintPP.Core.Rasterization;
using System.Numerics;

public class BezierCurve
{
    public static IEnumerable<Vector2> Sample(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
    {
        if (steps <= 0)
            throw new ArgumentException("no. steps cant be negative", nameof(steps));

        Vector2 a = -p0 + 3 * p1 - 3 * p2 + p3;
        Vector2 b = 3 * p0 - 6 * p1 + 3 * p2;
        Vector2 c = 3 * p1 - 3 * p0;
        Vector2 d = p0;
        //  P(t) = a·t³ + b·t² + c·t + d

        var h = 1f / steps;
        var h2 = h * h;
        var h3 = h2 * h;

        var P = d;
        var d1 = a * h3 + b * h2 + c * h;
        var d2 = 6 * a * h3 + 2 * b * h2;
        var d3 = 6 * a * h3;
        yield return P;

        for (int i = 0; i < steps; ++i)
        {
            P += d1;
            d1 += d2;
            d2 += d3;
            yield return P;
        }
    }
}
