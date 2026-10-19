using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Model;

public class SceneHitTestTests
{
    private const float Radius = 8f;

    [Fact]
    public void HitTestPoint_FarFromEverything_ReturnsNull()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        Assert.Null(scene.HitTestPoint(new Vector2(500, 500), Radius));
    }

    [Fact]
    public void HitTestPoint_NearVertex_ReturnsVertexAndItsPolygon()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        var hit = scene.HitTestPoint(new Vector2(103, 2), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle, hit.Polygon);
        Assert.Same(triangle.Vertices[1], hit.Point);
    }

    [Fact]
    public void HitTestPoint_TwoVerticesInRange_ReturnsCloserOne()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (10, 0), (5, 80));
        var scene = builder.Build();

        var hit = scene.HitTestPoint(new Vector2(6, 0), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle.Vertices[1], hit.Point);
    }

    [Fact]
    public void HitTestPoint_ExactlyAtRadius_Hits()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        var hit = scene.HitTestPoint(new Vector2(-Radius, 0), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle.Vertices[0], hit.Point);
    }

    [Fact]
    public void HitTestPoint_JustOutsideRadius_ReturnsNull()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        Assert.Null(scene.HitTestPoint(new Vector2(-Radius - 0.1f, 0), Radius));
    }

    [Fact]
    public void HitTestPoint_OverlappingPolygons_ReturnsPolygonOfCloserVertex()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var second = builder.Polygon((104, 0), (200, 0), (150, 80));
        var scene = builder.Build();

        var hit = scene.HitTestPoint(new Vector2(103, 0), Radius);

        Assert.NotNull(hit);
        Assert.Same(second, hit.Polygon);
        Assert.Same(second.Vertices[0], hit.Point);
    }

    [Fact]
    public void HitTestPoint_OnEdgeMiddle_ReturnsNull()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        Assert.Null(scene.HitTestPoint(new Vector2(50, 0), Radius));
    }

    [Fact]
    public void HitTestPoint_EmptyScene_ReturnsNull()
    {
        var scene = new SceneBuilder().Build();

        Assert.Null(scene.HitTestPoint(Vector2.Zero, Radius));
    }

    [Fact]
    public void HitTestEdge_FarFromEverything_ReturnsNull()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        Assert.Null(scene.HitTestEdge(new Vector2(500, 500), Radius));
    }

    [Fact]
    public void HitTestEdge_NearEdgeMiddle_ReturnsEdgeAndItsPolygon()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        var hit = scene.HitTestEdge(new Vector2(50, 3), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle, hit.Polygon);
        Assert.Same(triangle.Edges[0], hit.Edge);
    }

    [Fact]
    public void HitTestEdge_InsidePolygonFarFromEdges_ReturnsNull()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (100, 100), (0, 100));
        var scene = builder.Build();

        Assert.Null(scene.HitTestEdge(new Vector2(50, 50), Radius));
    }

    [Fact]
    public void HitTestEdge_TwoEdgesInRange_ReturnsCloserOne()
    {
        var builder = new SceneBuilder();
        var square = builder.Polygon((0, 0), (100, 0), (100, 100), (0, 100));
        var scene = builder.Build();

        // 2 px od krawędzi dolnej (y = 0), 5 px od lewej (x = 0)
        var hit = scene.HitTestEdge(new Vector2(5, 2), Radius);

        Assert.NotNull(hit);
        Assert.Same(square.Edges[0], hit.Edge);
    }

    [Fact]
    public void HitTestEdge_ClosingEdge_IsHit()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        // krawędź V2 -> V0, środek w (25, 40)
        var hit = scene.HitTestEdge(new Vector2(25, 40), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle.Edges[2], hit.Edge);
    }

    [Fact]
    public void HitTestEdge_ExactlyAtTolerance_Hits()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var scene = builder.Build();

        var hit = scene.HitTestEdge(new Vector2(50, -Radius), Radius);

        Assert.NotNull(hit);
        Assert.Same(triangle.Edges[0], hit.Edge);
    }

    [Fact]
    public void HitTestEdge_SecondPolygon_ReturnsThatPolygon()
    {
        var builder = new SceneBuilder();
        builder.Polygon((0, 0), (100, 0), (50, 80));
        var second = builder.Polygon((200, 0), (300, 0), (250, 80));
        var scene = builder.Build();

        var hit = scene.HitTestEdge(new Vector2(250, 1), Radius);

        Assert.NotNull(hit);
        Assert.Same(second, hit.Polygon);
        Assert.Same(second.Edges[0], hit.Edge);
    }
}
