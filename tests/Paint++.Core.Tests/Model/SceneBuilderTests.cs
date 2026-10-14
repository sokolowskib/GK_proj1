using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Model;

public class SceneBuilderTests
{
    [Fact]
    public void Build_WithoutPolygons_ReturnsEmptyScene()
    {
        var scene = new SceneBuilder().Build();

        Assert.Empty(scene.Polygons);
    }

    [Fact]
    public void Polygon_AddsPolygonsToSceneInOrder()
    {
        var builder = new SceneBuilder();

        var first = builder.Polygon((0, 0), (10, 0), (5, 8));
        var second = builder.Polygon((20, 0), (30, 0), (30, 10), (20, 10));
        var scene = builder.Build();

        Assert.Equal(2, scene.Polygons.Count);
        Assert.Same(first, scene.Polygons[0]);
        Assert.Same(second, scene.Polygons[1]);
    }

    [Fact]
    public void Polygon_ConvertsTuplesToVertexPositions()
    {
        var builder = new SceneBuilder();

        var polygon = builder.Polygon((1, 2), (3.5f, 4), (5, 6));

        Assert.Equal(new Vector2(1, 2), polygon.Vertices[0].Position);
        Assert.Equal(new Vector2(3.5f, 4), polygon.Vertices[1].Position);
        Assert.Equal(new Vector2(5, 6), polygon.Vertices[2].Position);
    }

    [Fact]
    public void Polygon_WithTooFewPoints_Throws()
    {
        var builder = new SceneBuilder();

        Assert.Throws<ArgumentException>(() => builder.Polygon((0, 0), (1, 1)));
    }
}
