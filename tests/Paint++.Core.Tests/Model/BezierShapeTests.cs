using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Model;

public class BezierShapeTests
{
    // łuk A(0,0) → B(600,0), uchwyty w górę; szczyt krzywej w t = 0.5 to (300, 225)
    private static readonly Vector2 Start = new(0, 0);
    private static readonly Vector2 End = new(600, 0);

    private static BezierShape CreateArch() =>
        new(new ScenePoint(new(0, 300)), new ScenePoint(new(600, 300)));

    [Fact]
    public void ControlPoints_AreC1ThenC2()
    {
        var shape = CreateArch();

        Assert.Equal([shape.C1, shape.C2], shape.ControlPoints);
    }

    [Theory]
    [InlineData(0f, 0f)]       // początek
    [InlineData(600f, 0f)]     // koniec
    [InlineData(300f, 225f)]   // szczyt (t = 0.5)
    public void DistanceTo_PointOnCurve_IsNearZero(float x, float y)
    {
        var shape = CreateArch();

        Assert.True(shape.DistanceTo(Start, End, new(x, y)) < 1f);
    }

    [Fact]
    public void DistanceTo_BetweenSamples_StillNearZero()
    {
        // długa krzywa, punkt w t = 1/64 — dokładnie między próbkami (krok 1/32)
        var shape = CreateArch();
        float t = 1f / 64, u = 1 - t;
        var onCurve = 3 * u * u * t * shape.C1.Position + 3 * u * t * t * shape.C2.Position + t * t * t * End;

        Assert.True(shape.DistanceTo(Start, End, onCurve) < 1f);
    }

    [Fact]
    public void DistanceTo_FarFromCurve_IsLarge()
    {
        var shape = CreateArch();

        // środek cięciwy AB — krzywa jest 225 px wyżej
        Assert.True(shape.DistanceTo(Start, End, new(300, 0)) > 100f);
    }

    [Fact]
    public void DistanceTo_ControlPoint_IsNotOnCurve()
    {
        // krzywa nie przechodzi przez punkty kontrolne
        var shape = CreateArch();

        Assert.True(shape.DistanceTo(Start, End, shape.C1.Position) > 50f);
    }
}
