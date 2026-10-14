using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Model;

public class PolygonTests
{
    private static Polygon CreateTriangle() => new([new(0, 0), new(10, 0), new(5, 8)]);

    [Fact]
    public void Constructor_CreatesOneEdgePerVertex()
    {
        var triangle = CreateTriangle();

        Assert.Equal(3, triangle.Vertices.Count);
        Assert.Equal(3, triangle.Edges.Count);
    }

    [Fact]
    public void Edges_ConnectConsecutiveVertices()
    {
        var triangle = CreateTriangle();

        Assert.Same(triangle.Vertices[0], triangle.Edges[0].Start);
        Assert.Same(triangle.Vertices[1], triangle.Edges[0].End);
        Assert.Same(triangle.Vertices[1], triangle.Edges[1].Start);
    }

    [Fact]
    public void LastEdge_WrapsAroundToFirstVertex()
    {
        var triangle = CreateTriangle();

        Assert.Same(triangle.Vertices[0], triangle.Edges[2].End);
    }

    [Fact]
    public void IncomingEdge_OfFirstVertex_IsLastEdge()
    {
        var triangle = CreateTriangle();

        Assert.Same(triangle.Edges[2], triangle.IncomingEdge(triangle.Vertices[0]));
    }

    [Fact]
    public void OutgoingEdge_OfVertex_StartsAtThatVertex()
    {
        var triangle = CreateTriangle();
        var vertex = triangle.Vertices[1];

        Assert.Same(vertex, triangle.OutgoingEdge(vertex).Start);
    }

    [Fact]
    public void IncomingEdge_OfForeignVertex_Throws()
    {
        var triangle = CreateTriangle();
        var foreign = new Vertex(new Vector2(0, 0));

        Assert.Throws<ArgumentException>(() => triangle.IncomingEdge(foreign));
    }

    [Fact]
    public void Constructor_WithTwoPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Polygon([new(0, 0), new(1, 1)]));
    }

    [Fact]
    public void Translate_MovesAllVertices()
    {
        var triangle = CreateTriangle();

        triangle.Translate(new Vector2(5, -2));

        Assert.Equal(new Vector2(5, -2), triangle.Vertices[0].Position);
        Assert.Equal(new Vector2(15, -2), triangle.Vertices[1].Position);
        Assert.Equal(new Vector2(10, 6), triangle.Vertices[2].Position);
    }

    [Fact]
    public void Edge_DistanceTo_UsesEdgeEndpoints()
    {
        var triangle = CreateTriangle();

        float distance = triangle.Edges[0].DistanceTo(new Vector2(4, 3));

        Assert.Equal(3f, distance, precision: 4);
    }

    [Fact]
    public void MovingSharedVertex_IsVisibleInBothEdges()
    {
        var triangle = CreateTriangle();
        var shared = triangle.Vertices[1];

        shared.Position = new Vector2(100, 100);

        Assert.Equal(new Vector2(100, 100), triangle.Edges[0].End.Position);
        Assert.Equal(new Vector2(100, 100), triangle.Edges[1].Start.Position);
    }
}
