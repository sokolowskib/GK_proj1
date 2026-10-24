using System.Numerics;
using PaintPP.Core.Geometry;

namespace PaintPP.Core.Tests.Geometry;

public class ArcGeometryTests
{
    private const float Eps = 0.01f;

    private static void AssertNear(Vector2 expected, Vector2 actual, float eps = Eps) =>
        Assert.True(Vector2.Distance(expected, actual) < eps, $"Oczekiwano {expected}, jest {actual}");

    private static Vector2 Left(Vector2 a, Vector2 b) => new(-(b - a).Y, (b - a).X);

    public static TheoryData<Vector2, Vector2, Vector2> Cases => new()
    {
        { new(0, 0), new(100, 0), new(50, 50) },     // przykład z rozmowy: środek (50, −50)
        { new(0, 0), new(100, 0), new(50, -50) },    // lustro
        { new(0, 0), new(100, 0), new(-30, 40) },    // wychodzi "do tyłu" → łuk dłuższy niż półokrąg
        { new(20, 80), new(-60, 10), new(5, 17) },   // dowolne
        { new(0, 0), new(100, 0), new(0, 100) },     // prostopadle → półokrąg
    };

    [Fact]
    public void FromTangent_ExampleFromConversation()
    {
        var arc = ArcGeometry.FromTangent(new(0, 0), new(100, 0), new(50, 50))!.Value;

        AssertNear(new(50, -50), arc.Center);
        Assert.Equal(MathF.Sqrt(5000), arc.Radius, 0.01f);
        Assert.Equal(-MathF.PI / 2, arc.Sweep, 0.001f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FromTangent_CirclePassesThroughBothEnds(Vector2 a, Vector2 b, Vector2 dir)
    {
        var arc = ArcGeometry.FromTangent(a, b, dir)!.Value;

        Assert.Equal(arc.Radius, Vector2.Distance(arc.Center, a), 0.01f);
        Assert.Equal(arc.Radius, Vector2.Distance(arc.Center, b), 0.01f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Sample_StartsAtAEndsAtB(Vector2 a, Vector2 b, Vector2 dir)
    {
        var pts = ArcGeometry.Sample(ArcGeometry.FromTangent(a, b, dir)!.Value, 32).ToList();

        Assert.Equal(33, pts.Count);
        AssertNear(a, pts[0]);
        AssertNear(b, pts[^1]);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Sample_LeavesAInGivenDirection(Vector2 a, Vector2 b, Vector2 dir)
    {
        // pierwszy krok łamanej ma iść (prawie) w kierunku direction — to sprawdza znak Sweep
        var pts = ArcGeometry.Sample(ArcGeometry.FromTangent(a, b, dir)!.Value, 2000).ToList();

        var step = Vector2.Normalize(pts[1] - pts[0]);
        AssertNear(Vector2.Normalize(dir), step, 0.01f);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FromTangent_RadiusAtAIsPerpendicularToDirection(Vector2 a, Vector2 b, Vector2 dir)
    {
        var arc = ArcGeometry.FromTangent(a, b, dir)!.Value;

        var cos = Vector2.Dot(Vector2.Normalize(a - arc.Center), Vector2.Normalize(dir));
        Assert.True(MathF.Abs(cos) < 1e-3f);
    }

    [Fact]
    public void FromTangent_PerpendicularLeft_IsSemicircleOnTheLeft()
    {
        Vector2 a = new(10, 20), b = new(130, 80);

        var arc = ArcGeometry.FromTangent(a, b, Left(a, b))!.Value;

        AssertNear((a + b) / 2, arc.Center);
        Assert.Equal(Vector2.Distance(a, b) / 2, arc.Radius, 0.01f);
        Assert.Equal(MathF.PI, MathF.Abs(arc.Sweep), 0.001f);
        // środek łuku (próbka w połowie) leży po lewej od A→B
        var mid = ArcGeometry.Sample(arc, 2).ElementAt(1);
        var d = b - a;
        Assert.True(d.X * (mid - a).Y - d.Y * (mid - a).X > 0);
    }

    [Fact]
    public void FromTangent_BackwardsDirection_GivesLongArc()
    {
        // wyjście z A "od B" → łuk musi obejść ponad pół okręgu
        var arc = ArcGeometry.FromTangent(new(0, 0), new(100, 0), new(-30, 40))!.Value;

        Assert.True(MathF.Abs(arc.Sweep) > MathF.PI);
    }

    [Theory]
    [InlineData(100f, 0f)]    // wzdłuż cięciwy
    [InlineData(-100f, 0f)]   // przeciwnie do cięciwy
    [InlineData(0f, 0f)]      // brak kierunku
    public void FromTangent_DirectionParallelToChordOrZero_ReturnsNull(float dx, float dy)
    {
        Assert.Null(ArcGeometry.FromTangent(new(0, 0), new(100, 0), new(dx, dy)));
    }

    [Fact]
    public void FromTangent_SamePoints_ReturnsNull()
    {
        Assert.Null(ArcGeometry.FromTangent(new(5, 5), new(5, 5), new(0, 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Sample_NonPositiveSteps_Throws(int steps)
    {
        var arc = new Arc(Vector2.Zero, 1, 0, 1);

        Assert.ThrowsAny<ArgumentException>(() => ArcGeometry.Sample(arc, steps).ToList());
    }
}
