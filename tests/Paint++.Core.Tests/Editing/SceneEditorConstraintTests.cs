using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Editing;

public class SceneEditorConstraintTests
{
    private const float Eps = 0.05f; // luźniej niż epsilon solvera (0.01)

    private static (SceneEditor Editor, Scene Scene, Polygon Square, SceneBuilder Builder) CreateSquare()
    {
        var builder = new SceneBuilder();
        var square = builder.Polygon((0, 0), (100, 0), (100, 100), (0, 100));
        var scene = builder.Build();
        return (new SceneEditor(scene), scene, square, builder);
    }

    private static Dictionary<ScenePoint, Vector2> Positions(Scene scene) =>
        scene.AllPoints.ToDictionary(p => p, p => p.Position);

    private static void AssertNear(Vector2 expected, Vector2 actual) =>
        Assert.True(Vector2.Distance(expected, actual) < Eps, $"Oczekiwano {expected}, jest {actual}");

    // ---------- MovePoint ----------

    [Fact]
    public void MovePoint_NoConstraints_MovesOnlyThatPoint()
    {
        var (editor, scene, square, _) = CreateSquare();
        var moved = square.Vertices[1];
        var before = Positions(scene);

        editor.MovePoint(square, moved, new Vector2(150, 20));

        Assert.Equal(new Vector2(150, 20), moved.Position);
        foreach (var (p, pos) in before)
            if (p != moved)
                Assert.Equal(pos, p.Position);
    }

    [Fact]
    public void MovePoint_WithLength_PointFollowsCursorAndLengthIsKept()
    {
        var (editor, _, square, builder) = CreateSquare();
        var constraint = builder.Length(square.Edges[0], 100);
        var moved = square.Vertices[1];

        editor.MovePoint(square, moved, new Vector2(150, 30));

        Assert.Equal(new Vector2(150, 30), moved.Position); // pinned — dokładnie pod kursorem
        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void MovePoint_WithLengthChain_AllConstraintsSatisfied()
    {
        var (editor, _, square, builder) = CreateSquare();
        var c0 = builder.Length(square.Edges[0], 100);
        var c1 = builder.Length(square.Edges[1], 100);

        editor.MovePoint(square, square.Vertices[0], new Vector2(-40, -40));

        Assert.True(c0.IsSatisfied(Eps));
        Assert.True(c1.IsSatisfied(Eps));
    }

    [Fact]
    public void MovePoint_ContradictoryConstraints_FallsBackToTranslation()
    {
        var (editor, scene, square, builder) = CreateSquare();
        // dwie różne długości na tej samej krawędzi — solver nie może zbiec
        builder.Length(square.Edges[0], 100);
        builder.Length(square.Edges[0], 50);
        var moved = square.Vertices[2];
        var before = Positions(scene);
        var delta = new Vector2(30, -10);

        editor.MovePoint(square, moved, before[moved] + delta);

        foreach (var (p, pos) in before)
            AssertNear(pos + delta, p.Position);
    }

    [Fact]
    public void MovePoint_Fallback_DraggedPointEndsUnderCursor()
    {
        var (editor, _, square, builder) = CreateSquare();
        builder.Length(square.Edges[0], 100);
        builder.Length(square.Edges[0], 50);
        var moved = square.Vertices[0];
        var target = new Vector2(-25, 60);

        editor.MovePoint(square, moved, target);

        AssertNear(target, moved.Position);
    }

    [Fact]
    public void MovePoint_Fallback_DoesNotMoveOtherPolygons()
    {
        var (editor, _, square, builder) = CreateSquare();
        var other = builder.Polygon((300, 300), (400, 300), (350, 380));
        builder.Length(square.Edges[0], 100);
        builder.Length(square.Edges[0], 50);
        var otherBefore = other.AllPoints.Select(p => p.Position).ToList();

        editor.MovePoint(square, square.Vertices[0], new Vector2(-30, -30));

        Assert.Equal(otherBefore, other.AllPoints.Select(p => p.Position).ToList());
    }

    // ---------- TranslatePolygon ----------

    [Fact]
    public void TranslatePolygon_MovesAllPointsByDelta()
    {
        var (editor, scene, square, _) = CreateSquare();
        var before = Positions(scene);
        var delta = new Vector2(12, -7);

        editor.TranslatePolygon(square, delta);

        foreach (var (p, pos) in before)
            Assert.Equal(pos + delta, p.Position);
    }

    [Fact]
    public void TranslatePolygon_KeepsConstraintsSatisfied()
    {
        var (editor, _, square, builder) = CreateSquare();
        var constraint = builder.Length(square.Edges[0], 100);

        editor.TranslatePolygon(square, new Vector2(50, 50));

        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void TranslatePolygon_DoesNotMoveOtherPolygons()
    {
        var (editor, _, square, builder) = CreateSquare();
        var other = builder.Polygon((300, 300), (400, 300), (350, 380));
        var otherBefore = other.AllPoints.Select(p => p.Position).ToList();

        editor.TranslatePolygon(square, new Vector2(50, 50));

        Assert.Equal(otherBefore, other.AllPoints.Select(p => p.Position).ToList());
    }

    // ---------- AddConstraint / RemoveConstraint ----------

    [Fact]
    public void AddConstraint_Satisfiable_ReturnsTrueAddsAndSatisfies()
    {
        var (editor, scene, square, _) = CreateSquare();
        var constraint = new LengthConstraint(square.Edges[0], 150);

        bool added = editor.AddConstraint(constraint);

        Assert.True(added);
        Assert.Contains(constraint, scene.Constraints);
        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void AddConstraint_AlreadySatisfied_MovesNothing()
    {
        var (editor, scene, square, _) = CreateSquare();
        var before = Positions(scene);

        editor.AddConstraint(new LengthConstraint(square.Edges[0], 100));

        foreach (var (p, pos) in before)
            Assert.Equal(pos, p.Position);
    }

    [Fact]
    public void AddConstraint_Contradictory_ReturnsFalseAndRollsBack()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var existing = builder.Length(square.Edges[0], 100);
        var before = Positions(scene);
        var conflicting = new LengthConstraint(square.Edges[0], 50);

        bool added = editor.AddConstraint(conflicting);

        Assert.False(added);
        Assert.DoesNotContain(conflicting, scene.Constraints);
        Assert.Contains(existing, scene.Constraints);
        foreach (var (p, pos) in before)
            Assert.Equal(pos, p.Position);
    }

    [Fact]
    public void RemoveConstraint_RemovesOnlyThatConstraint()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var removed = builder.Length(square.Edges[0], 100);
        var kept = builder.Length(square.Edges[1], 100);

        editor.RemoveConstraint(removed);

        Assert.DoesNotContain(removed, scene.Constraints);
        Assert.Contains(kept, scene.Constraints);
    }

    // ---------- SetLength ----------

    [Fact]
    public void SetLength_NoExisting_AddsSatisfiedConstraint()
    {
        var (editor, scene, square, _) = CreateSquare();
        var edge = square.Edges[0];

        bool ok = editor.SetLength(edge, 140);

        Assert.True(ok);
        var constraint = Assert.Single(scene.Constraints.OfType<LengthConstraint>());
        Assert.Same(edge, constraint.Edges[0]);
        Assert.Equal(140, edge.Length, 1);
    }

    [Fact]
    public void SetLength_Existing_ReplacesInsteadOfAdding()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var edge = square.Edges[0];
        var old = builder.Length(edge, 100);

        bool ok = editor.SetLength(edge, 60);

        Assert.True(ok);
        Assert.DoesNotContain(old, scene.Constraints);
        var constraint = Assert.Single(scene.Constraints.OfType<LengthConstraint>());
        Assert.Equal(60, constraint.Length);
        Assert.Equal(60, edge.Length, 1);
    }

    [Fact]
    public void SetLength_DoesNotTouchConstraintsOfOtherEdges()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var other = builder.Length(square.Edges[2], 100);

        editor.SetLength(square.Edges[0], 80);

        Assert.Contains(other, scene.Constraints);
        Assert.True(other.IsSatisfied(Eps));
    }

    [Fact]
    public void SetLength_Impossible_ReturnsFalseAndRestoresOldConstraint()
    {
        // Trójkąt: dwie krawędzie o stałych długościach 10 → trzecia nie może mieć 100 (nierówność trójkąta)
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (10, 0), (5, 8));
        builder.Length(triangle.Edges[0], triangle.Edges[0].Length);
        builder.Length(triangle.Edges[1], triangle.Edges[1].Length);
        var old = builder.Length(triangle.Edges[2], triangle.Edges[2].Length);
        var scene = builder.Build();
        var editor = new SceneEditor(scene);
        var before = Positions(scene);

        bool ok = editor.SetLength(triangle.Edges[2], 100);

        Assert.False(ok);
        Assert.Contains(old, scene.Constraints);
        Assert.Equal(3, scene.Constraints.Count);
        foreach (var (p, pos) in before)
            Assert.Equal(pos, p.Position);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-10f)]
    public void SetLength_NonPositive_ReturnsFalseAndKeepsOldConstraint(float length)
    {
        var (editor, scene, square, builder) = CreateSquare();
        var old = builder.Length(square.Edges[0], 100);
        var before = Positions(scene);

        bool ok = editor.SetLength(square.Edges[0], length);

        Assert.False(ok);
        Assert.Same(old, Assert.Single(scene.Constraints));
        foreach (var (p, pos) in before)
            Assert.Equal(pos, p.Position);
    }

    // ---------- Insert/Delete usuwają ograniczenia podmienionych krawędzi ----------

    [Fact]
    public void Insert_RemovesConstraintsOfSplitEdgeOnly()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var onSplit = builder.Length(square.Edges[0], 100);
        var onOther = builder.Length(square.Edges[2], 100);

        editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);

        Assert.DoesNotContain(onSplit, scene.Constraints);
        Assert.Contains(onOther, scene.Constraints);
    }

    [Fact]
    public void Insert_RemovesAllConstraintsOfSplitEdge()
    {
        var (editor, scene, square, builder) = CreateSquare();
        builder.Length(square.Edges[0], 100);
        builder.Length(square.Edges[0], 100);

        editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);

        Assert.Empty(scene.Constraints);
    }

    [Fact]
    public void Delete_RemovesConstraintsOfBothAdjacentEdges()
    {
        var (editor, scene, square, builder) = CreateSquare();
        var victim = square.Vertices[1];
        var incoming = builder.Length(square.IncomingEdge(victim), 100);
        var outgoing = builder.Length(square.OutgoingEdge(victim), 100);
        var unrelated = builder.Length(square.Edges[2], 100);

        editor.DeleteVertex(square, victim);

        Assert.DoesNotContain(incoming, scene.Constraints);
        Assert.DoesNotContain(outgoing, scene.Constraints);
        Assert.Contains(unrelated, scene.Constraints);
    }

    [Fact]
    public void Delete_Rejected_KeepsConstraints()
    {
        var builder = new SceneBuilder();
        var triangle = builder.Polygon((0, 0), (100, 0), (50, 80));
        var constraint = builder.Length(triangle.Edges[0], 100);
        var scene = builder.Build();
        var editor = new SceneEditor(scene);

        editor.DeleteVertex(triangle, triangle.Vertices[0]);

        Assert.Contains(constraint, scene.Constraints);
    }

    [Fact]
    public void Insert_ThenMoveNewVertex_OldConstraintDoesNotPullIt()
    {
        // regresja: stare ograniczenie usuniętej krawędzi ciągnęło wierzchołki
        var (editor, _, square, builder) = CreateSquare();
        builder.Length(square.Edges[0], 100);
        var a = square.Vertices[0];
        var b = square.Vertices[1];

        var inserted = editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);
        editor.MovePoint(square, inserted, new Vector2(50, -80));

        Assert.Equal(new Vector2(0, 0), a.Position);
        Assert.Equal(new Vector2(100, 0), b.Position);
        Assert.Equal(new Vector2(50, -80), inserted.Position);
    }
}
