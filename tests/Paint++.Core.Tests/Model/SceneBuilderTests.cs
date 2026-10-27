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

    [Fact]
    public void Bezier_SetsGivenControlPointsAndDefaultC1()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));

        var shape = builder.Bezier(polygon.Edges[0], (30, -20), (60, -20));

        Assert.Same(shape, polygon.Edges[0].Shape);
        Assert.Equal(new Vector2(30, -20), shape.C1.Position);
        Assert.Equal(new Vector2(60, -20), shape.C2.Position);
        Assert.Equal(Continuity.C1, polygon.Vertices[0].Continuity);
        Assert.Equal(Continuity.C1, polygon.Vertices[1].Continuity);
    }

    [Fact]
    public void Arc_ConvertsEdgeToArc()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));

        var shape = builder.Arc(polygon.Edges[1]);

        Assert.Same(shape, polygon.Edges[1].Shape);
    }

    [Fact]
    public void SetContinuity_MarksVertexExplicit()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        builder.SetContinuity(polygon.Vertices[0], Continuity.G1);

        builder.Bezier(polygon.Edges[0], (30, -20), (60, -20));

        Assert.Equal(Continuity.G1, polygon.Vertices[0].Continuity);
        Assert.True(polygon.Vertices[0].ContinuityExplicit);
    }

    [Fact]
    public void SetContinuity_G1OnBothEndsOfArc_Throws()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        builder.Arc(polygon.Edges[0]);
        builder.SetContinuity(polygon.Vertices[0], Continuity.G1);

        Assert.Throws<InvalidOperationException>(() => builder.SetContinuity(polygon.Vertices[1], Continuity.G1));
    }

    [Fact]
    public void Build_SolvesConstraints()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        builder.Length(polygon.Edges[0], 50);

        var scene = builder.Build();

        Assert.All(scene.Constraints, c => Assert.True(c.IsSatisfied(0.01f)));
        Assert.Equal(50, polygon.Edges[0].Length, 0.01f);
    }

    [Fact]
    public void Build_ContradictoryConstraints_Throws()
    {
        var builder = new SceneBuilder();
        var polygon = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        builder.Length(polygon.Edges[0], 50);
        builder.Length(polygon.Edges[0], 80);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}
