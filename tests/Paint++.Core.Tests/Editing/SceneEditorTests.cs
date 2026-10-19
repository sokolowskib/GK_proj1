using System.Numerics;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Editing;

public class SceneEditorTests
{
    private static (SceneEditor Editor, Polygon Square) CreateSquare()
    {
        var builder = new SceneBuilder();
        var square = builder.Polygon((0, 0), (100, 0), (100, 100), (0, 100));
        return (new SceneEditor(builder.Build()), square);
    }

    /// <summary>
    /// Niezmiennik: Edges[i] łączy Vertices[i] z Vertices[i + 1] (cyklicznie).
    /// </summary>
    private static void AssertEdgesConsistent(Polygon polygon)
    {
        int n = polygon.Vertices.Count;
        Assert.Equal(n, polygon.Edges.Count);
        for (int i = 0; i < n; i++)
        {
            Assert.Same(polygon.Vertices[i], polygon.Edges[i].Start);
            Assert.Same(polygon.Vertices[(i + 1) % n], polygon.Edges[i].End);
        }
    }

    // ---------- InsertVertexAtEdgeMidpoint ----------

    [Fact]
    public void Insert_AddsVertexAtEdgeMidpoint()
    {
        var (editor, square) = CreateSquare();

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);

        Assert.Equal(5, square.Vertices.Count);
        Assert.Equal(new Vector2(50, 0), inserted.Position);
    }

    [Fact]
    public void Insert_PlacesVertexBetweenEdgeEndpoints()
    {
        var (editor, square) = CreateSquare();
        var a = square.Vertices[1];
        var b = square.Vertices[2];

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[1]);

        Assert.Same(a, square.Vertices[1]);
        Assert.Same(inserted, square.Vertices[2]);
        Assert.Same(b, square.Vertices[3]);
    }

    [Fact]
    public void Insert_KeepsEdgesConsistent()
    {
        var (editor, square) = CreateSquare();

        editor.InsertVertexAtEdgeMidpoint(square, square.Edges[1]);

        AssertEdgesConsistent(square);
    }

    [Fact]
    public void Insert_RemovesSplitEdge()
    {
        var (editor, square) = CreateSquare();
        var split = square.Edges[1];

        editor.InsertVertexAtEdgeMidpoint(square, split);

        Assert.DoesNotContain(split, square.Edges);
    }

    [Fact]
    public void Insert_PreservesOtherEdgeObjects()
    {
        var (editor, square) = CreateSquare();
        var untouched = new[] { square.Edges[0], square.Edges[2], square.Edges[3] };

        editor.InsertVertexAtEdgeMidpoint(square, square.Edges[1]);

        foreach (var edge in untouched)
            Assert.Contains(edge, square.Edges);
    }

    [Fact]
    public void Insert_OnClosingEdge_AppendsVertexAtEnd()
    {
        var (editor, square) = CreateSquare();

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[3]);

        Assert.Same(inserted, square.Vertices[4]);
        Assert.Equal(new Vector2(0, 50), inserted.Position);
        AssertEdgesConsistent(square);
    }

    [Fact]
    public void Insert_ThenIncomingAndOutgoingEdgesMeetAtNewVertex()
    {
        var (editor, square) = CreateSquare();

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);

        Assert.Same(inserted, square.IncomingEdge(inserted).End);
        Assert.Same(inserted, square.OutgoingEdge(inserted).Start);
        Assert.Same(square.Vertices[0], square.IncomingEdge(inserted).Start);
    }

    // ---------- DeleteVertex ----------

    [Fact]
    public void Delete_RemovesVertex()
    {
        var (editor, square) = CreateSquare();
        var victim = square.Vertices[1];

        bool deleted = editor.DeleteVertex(square, victim);

        Assert.True(deleted);
        Assert.Equal(3, square.Vertices.Count);
        Assert.DoesNotContain(victim, square.Vertices);
    }

    [Fact]
    public void Delete_ConnectsNeighboursWithNewEdge()
    {
        var (editor, square) = CreateSquare();
        var previous = square.Vertices[0];
        var next = square.Vertices[2];

        editor.DeleteVertex(square, square.Vertices[1]);

        Assert.Contains(square.Edges, e => e.Start == previous && e.End == next);
    }

    [Fact]
    public void Delete_KeepsEdgesConsistent()
    {
        var (editor, square) = CreateSquare();

        editor.DeleteVertex(square, square.Vertices[2]);

        AssertEdgesConsistent(square);
    }

    [Fact]
    public void Delete_RemovesBothAdjacentEdges()
    {
        var (editor, square) = CreateSquare();
        var victim = square.Vertices[1];
        var incoming = square.IncomingEdge(victim);
        var outgoing = square.OutgoingEdge(victim);

        editor.DeleteVertex(square, victim);

        Assert.DoesNotContain(incoming, square.Edges);
        Assert.DoesNotContain(outgoing, square.Edges);
    }

    [Fact]
    public void Delete_PreservesOtherEdgeObjects()
    {
        var (editor, square) = CreateSquare();
        var untouched = new[] { square.Edges[2], square.Edges[3] };

        editor.DeleteVertex(square, square.Vertices[1]);

        foreach (var edge in untouched)
            Assert.Contains(edge, square.Edges);
    }

    [Fact]
    public void Delete_FirstVertex_WrapsAround()
    {
        var (editor, square) = CreateSquare();
        var last = square.Vertices[3];
        var second = square.Vertices[1];

        editor.DeleteVertex(square, square.Vertices[0]);

        AssertEdgesConsistent(square);
        Assert.Contains(square.Edges, e => e.Start == last && e.End == second);
    }

    [Fact]
    public void Delete_LastVertex_WrapsAround()
    {
        var (editor, square) = CreateSquare();
        var first = square.Vertices[0];
        var third = square.Vertices[2];

        editor.DeleteVertex(square, square.Vertices[3]);

        AssertEdgesConsistent(square);
        Assert.Contains(square.Edges, e => e.Start == third && e.End == first);
    }

    [Fact]
    public void Delete_FromTriangle_IsRejectedAndChangesNothing()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var editor = new SceneEditor(builder.Build());
        var edgesBefore = triangle.Edges.ToList();

        bool deleted = editor.DeleteVertex(triangle, triangle.Vertices[0]);

        Assert.False(deleted);
        Assert.Equal(3, triangle.Vertices.Count);
        Assert.Equal(edgesBefore, triangle.Edges);
    }

    [Fact]
    public void InsertThenDelete_RestoresVertexCount()
    {
        var (editor, square) = CreateSquare();

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);
        editor.DeleteVertex(square, inserted);

        Assert.Equal(4, square.Vertices.Count);
        AssertEdgesConsistent(square);
    }
}
