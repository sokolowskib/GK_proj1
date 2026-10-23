using System.Numerics;
using PaintPP.Core.Rasterization;

namespace PaintPP.Core.Tests.Rasterization;

public class BezierCurveTests
{
    private const float Eps = 0.01f;

    private static void AssertNear(Vector2 expected, Vector2 actual, float eps = Eps) =>
        Assert.True(Vector2.Distance(expected, actual) < eps, $"Oczekiwano {expected}, jest {actual}");

    // Wzór Bernsteina — niezależna od różnic w przód referencja
    private static Vector2 Bernstein(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    public static TheoryData<Vector2, Vector2, Vector2, Vector2, int> Curves => new()
    {
        { new(0, 0), new(0, 100), new(100, 100), new(100, 0), 10 },     // łuk "do góry"
        { new(0, 0), new(100, 100), new(0, 100), new(100, 0), 32 },     // pętla (uchwyty skrzyżowane)
        { new(50, 80), new(-30, 10), new(200, -40), new(120, 300), 7 }, // dowolne punkty
        { new(0, 0), new(300, 0), new(-200, 0), new(100, 0), 50 },      // wszystkie na jednej prostej, zawraca
    };

    // ---------- Liczba punktów i końce ----------

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(32)]
    public void Sample_ReturnsStepsPlusOnePoints(int steps)
    {
        var points = BezierCurve.Sample(new(0, 0), new(10, 20), new(30, 20), new(40, 0), steps).ToList();

        Assert.Equal(steps + 1, points.Count);
    }

    [Theory]
    [MemberData(nameof(Curves))]
    public void Sample_StartsAtP0AndEndsAtP3(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
    {
        var points = BezierCurve.Sample(p0, p1, p2, p3, steps).ToList();

        Assert.Equal(p0, points[0]);
        AssertNear(p3, points[^1]);
    }

    // ---------- Zgodność z Bernsteinem ----------

    [Theory]
    [MemberData(nameof(Curves))]
    public void Sample_MatchesBernsteinAtEveryStep(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps)
    {
        var points = BezierCurve.Sample(p0, p1, p2, p3, steps).ToList();

        for (int i = 0; i <= steps; i++)
            AssertNear(Bernstein(p0, p1, p2, p3, (float)i / steps), points[i]);
    }

    [Fact]
    public void Sample_ManySteps_ErrorDoesNotAccumulate()
    {
        // 1000 kroków dodawania floatów — błąd zaokrągleń ma zostać poniżej piksela
        Vector2 p0 = new(0, 0), p1 = new(400, 900), p2 = new(800, -300), p3 = new(1200, 600);

        var points = BezierCurve.Sample(p0, p1, p2, p3, 1000).ToList();

        for (int i = 0; i <= 1000; i += 50)
            AssertNear(Bernstein(p0, p1, p2, p3, i / 1000f), points[i], 0.5f);
    }

    // ---------- Przypadki szczególne ----------

    [Fact]
    public void Sample_ControlPointsAtThirds_IsUniformSegment()
    {
        // inicjalizacja po przełączeniu na Béziera: krzywa = odcinek, punkty równomiernie
        Vector2 a = new(10, 20), b = new(130, 80);
        var d = b - a;

        var points = BezierCurve.Sample(a, a + d / 3f, a + 2f * d / 3f, b, 6).ToList();

        for (int i = 0; i <= 6; i++)
            AssertNear(a + d * (i / 6f), points[i]);
    }

    [Fact]
    public void Sample_AllPointsEqual_StaysInPlace()
    {
        var p = new Vector2(42, 17);

        var points = BezierCurve.Sample(p, p, p, p, 8).ToList();

        Assert.All(points, q => AssertNear(p, q));
    }

    [Fact]
    public void Sample_StartTangentPointsTowardsP1()
    {
        Vector2 p0 = new(0, 0), p1 = new(0, 50), p2 = new(100, 50), p3 = new(100, 0);

        var points = BezierCurve.Sample(p0, p1, p2, p3, 200).ToList();

        var dir = Vector2.Normalize(points[1] - points[0]);
        AssertNear(Vector2.Normalize(p1 - p0), dir, 0.05f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Sample_NonPositiveSteps_Throws(int steps)
    {
        // ToList — metoda z yield sprawdza argumenty dopiero przy iteracji
        Assert.ThrowsAny<ArgumentException>(() =>
            BezierCurve.Sample(new(0, 0), new(1, 1), new(2, 1), new(3, 0), steps).ToList());
    }
}
