using System.Numerics;
using PaintPP.Core.Geometry;

namespace PaintPP.Core.Tests.Geometry;

public class Geometry2DTests
{
    [Fact]
    public void PointAboveSegment_ReturnsPerpendicularDistance()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(10, 0);
        var p = new Vector2(4, 3);

        float distance = Geometry2D.DistanceToSegment(p, a, b);

        Assert.Equal(3f, distance, precision: 4);
    }

    [Fact]
    public void PointBeyondEnd_ReturnsDistanceToEndpoint()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(10, 0);
        var p = new Vector2(13, 4);

        float distance = Geometry2D.DistanceToSegment(p, a, b);

        Assert.Equal(5f, distance, precision: 4);
    }

    [Fact]
    public void PointBeforeStart_ReturnsDistanceToStartpoint()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(10, 0);
        var p = new Vector2(-3, -4);

        float distance = Geometry2D.DistanceToSegment(p, a, b);

        Assert.Equal(5f, distance, precision: 4);
    }

    [Fact]
    public void DiagonalSegment_ReturnsPerpendicularDistance()
    {
        var a = new Vector2(0, 0);
        var b = new Vector2(6, 8);
        var p = new Vector2(7, 1);

        float distance = Geometry2D.DistanceToSegment(p, a, b);

        Assert.Equal(5f, distance, precision: 4);
    }

    [Fact]
    public void DegenerateSegment_ReturnsDistanceToPoint()
    {
        var a = new Vector2(2, 2);
        var p = new Vector2(5, 6);

        float distance = Geometry2D.DistanceToSegment(p, a, a);

        Assert.Equal(5f, distance, precision: 4);
    }
}
