using System.Numerics;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Editing;

public class SceneEditorBezierTests
{
    private const float Eps = 0.05f;

    private static (SceneEditor Editor, Scene Scene, Polygon Square, SceneBuilder Builder) CreateSquare()
    {
        var builder = new SceneBuilder();
        var square = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        // jawne G0 — te testy sprawdzają samą mechanikę Béziera, bez domyślnego C1 (to: ContinuityTests)
        foreach (var v in square.Vertices)
            v.ContinuityExplicit = true;
        var scene = builder.Build();
        return (new SceneEditor(scene), scene, square, builder);
    }

    private static void AssertNear(Vector2 expected, Vector2 actual) =>
        Assert.True(Vector2.Distance(expected, actual) < Eps, $"Oczekiwano {expected}, jest {actual}");

    // ---------- ConvertToBezier ----------

    [Fact]
    public void ConvertToBezier_ControlPointsAtOneAndTwoThirds()
    {
        var (editor, _, square, _) = CreateSquare();
        var edge = square.Edges[0];   // (0,0) → (90,0)

        editor.ConvertToBezier(edge);

        var shape = Assert.IsType<BezierShape>(edge.Shape);
        AssertNear(new(30, 0), shape.C1.Position);
        AssertNear(new(60, 0), shape.C2.Position);
    }

    [Fact]
    public void ConvertToBezier_RemovesConstraintsOfThatEdgeOnly()
    {
        var (editor, scene, square, builder) = CreateSquare();
        builder.Length(square.Edges[0], 90);
        var kept = builder.Length(square.Edges[2], 90);
        var perpendicular = builder.Perpendicular(square.Edges[0], square.Edges[1]);

        editor.ConvertToBezier(square.Edges[0]);

        Assert.Equal([kept], scene.Constraints);
        Assert.DoesNotContain(perpendicular, scene.Constraints);
    }

    [Fact]
    public void ConvertToBezier_DoesNotMoveVertices()
    {
        var (editor, _, square, _) = CreateSquare();
        var before = square.Vertices.Select(v => v.Position).ToList();

        editor.ConvertToBezier(square.Edges[1]);

        Assert.Equal(before, square.Vertices.Select(v => v.Position));
    }

    [Fact]
    public void ConvertToBezier_ControlPointsJoinAllPoints()
    {
        var (editor, scene, square, _) = CreateSquare();

        editor.ConvertToBezier(square.Edges[0]);

        var shape = (BezierShape)square.Edges[0].Shape;
        Assert.Contains(shape.C1, scene.AllPoints);
        Assert.Contains(shape.C2, scene.AllPoints);
        Assert.Equal(6, scene.AllPoints.Count());
    }

    // ---------- ConvertToLine ----------

    [Fact]
    public void ConvertToLine_RestoresLineAndDropsControlPoints()
    {
        var (editor, scene, square, _) = CreateSquare();
        var edge = square.Edges[0];
        editor.ConvertToBezier(edge);

        editor.ConvertToLine(edge);

        Assert.IsType<LineShape>(edge.Shape);
        Assert.Equal(4, scene.AllPoints.Count());
    }

    // ---------- Interakcja z resztą edytora ----------

    [Fact]
    public void HitTestPoint_FindsControlPoint()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var shape = (BezierShape)square.Edges[0].Shape;

        var hit = scene.HitTestPoint(new(31, 1), 8f);

        Assert.NotNull(hit);
        Assert.Same(shape.C1, hit.Point);
        Assert.Same(square, hit.Polygon);
    }

    [Fact]
    public void MovePoint_ControlPoint_MovesOnlyIt()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var c1 = ((BezierShape)square.Edges[0].Shape).C1;
        var before = scene.AllPoints.ToDictionary(p => p, p => p.Position);
        var target = new Vector2(20, -50);

        Assert.True(editor.MovePoint(square, c1, target));

        foreach (var (p, pos) in before)
            AssertNear(p == c1 ? target : pos, p.Position);
    }

    [Fact]
    public void TranslatePolygon_MovesControlPointsToo()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var shape = (BezierShape)square.Edges[0].Shape;
        var delta = new Vector2(15, -7);

        editor.TranslatePolygon(square, delta);

        AssertNear(new Vector2(30, 0) + delta, shape.C1.Position);
        AssertNear(new Vector2(60, 0) + delta, shape.C2.Position);
    }

    [Fact]
    public void MovePoint_Fallback_TranslatesControlPointsWithPolygon()
    {
        var (editor, _, square, builder) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var shape = (BezierShape)square.Edges[0].Shape;
        // sprzeczne długości na innej krawędzi → fallback translacji
        builder.Length(square.Edges[2], 90);
        builder.Length(square.Edges[2], 40);
        var delta = new Vector2(10, 25);

        Assert.False(editor.MovePoint(square, square.Vertices[0], square.Vertices[0].Position + delta));

        AssertNear(new Vector2(30, 0) + delta, shape.C1.Position);
        AssertNear(new Vector2(60, 0) + delta, shape.C2.Position);
    }
}
