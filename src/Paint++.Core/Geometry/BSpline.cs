using System.Numerics;

namespace PaintPP.Core.Geometry;

public readonly record struct BezierSegment(Vector2 P0, Vector2 P1, Vector2 P2, Vector2 P3);

///< summary>
/// Geometryczna konstrukcja kubicznej krzywej B-spline jako ciągu segmentów Béziera (wykład 01_krzywe, slajd 41).
/// Boki wieloboku B-spline dzielimy punktami podziału, punkty podziału sąsiednich boków łączymy odcinkami
/// pomocniczymi, a środki tych odcinków są punktami sklejenia segmentów Béziera.
///< summary>
public static class BSpline
{
    public static IReadOnlyList<BezierSegment> ToBezier(IReadOnlyList<Vector2> points, bool closed)
    {
        if (points.Count < 3)
            throw new ArgumentException("Wielobok B-spline musi mieć przynajmniej 3 punkty", nameof(points));
        return closed ? Closed(points) : Open(points);
    }

    // Zamknięty wielobok: każdy bok dzielony na 3 równe części.
    private static List<BezierSegment> Closed(IReadOnlyList<Vector2> p)
    {
        int n = p.Count;
        var near = new Vector2[n];  // punkt podziału boku i bliżej p[i]
        var far = new Vector2[n];   // punkt podziału boku i bliżej p[i+1]
        for (int i = 0; i < n; i++)
        {
            var next = p[(i + 1) % n];
            near[i] = Vector2.Lerp(p[i], next, 1f / 3f);
            far[i] = Vector2.Lerp(p[i], next, 2f / 3f);
        }

        var segments = new List<BezierSegment>(n);
        for (int i = 0; i < n; i++)
        {
            var start = (far[(i - 1 + n) % n] + near[i]) / 2f;  // środek odcinka pomocniczego przy p[i]
            var end = (far[i] + near[(i + 1) % n]) / 2f;        // środek odcinka pomocniczego przy p[i+1]
            segments.Add(new BezierSegment(start, near[i], far[i], end));
        }
        return segments;
    }

    // Otwarty wielobok p[0..n]: pierwszy i ostatni bok bez podziału, drugi i przedostatni na pół, reszta na 3.
    // Niepodzielony pierwszy bok oznacza, że p[1] od razu jest uchwytem pierwszego segmentu (analogicznie p[n-1]).
    private static List<BezierSegment> Open(IReadOnlyList<Vector2> p)
    {
        int n = p.Count - 1;  // liczba boków

        if (n == 2)  // za mało boków na kubiczny B-spline: zostaje parabola p0-p1-p2 zapisana jako Bézier 3 stopnia
            return [new BezierSegment(p[0], Vector2.Lerp(p[0], p[1], 2f / 3f), Vector2.Lerp(p[2], p[1], 2f / 3f),
                                      p[2])];

        var near = new Vector2[n];
        var far = new Vector2[n];
        for (int i = 1; i <= n - 2; i++)
        {
            var mid = (p[i] + p[i + 1]) / 2f;
            bool second = i == 1;
            bool penultimate = i == n - 2;
            near[i] = second ? p[i] : penultimate ? mid : Vector2.Lerp(p[i], p[i + 1], 1f / 3f);
            far[i] = penultimate ? p[i + 1] : second ? mid : Vector2.Lerp(p[i], p[i + 1], 2f / 3f);
        }

        var segments = new List<BezierSegment>(n - 2);
        for (int i = 1; i <= n - 2; i++)
        {
            var start = i == 1 ? p[0] : (far[i - 1] + near[i]) / 2f;
            var end = i == n - 2 ? p[n] : (far[i] + near[i + 1]) / 2f;
            segments.Add(new BezierSegment(start, near[i], far[i], end));
        }
        return segments;
    }
}
