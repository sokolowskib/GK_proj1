using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Editing;

public class ContinuityTests
{
    private const float Eps = 0.05f;

    // kwadrat V0(0,0) → V1(90,0) → V2(90,90) → V3(0,90); krawędź i wychodzi z V[i]
    private static (SceneEditor Editor, Scene Scene, Polygon Square, SceneBuilder Builder) CreateSquare()
    {
        var builder = new SceneBuilder();
        var square = builder.Polygon((0, 0), (90, 0), (90, 90), (0, 90));
        var scene = builder.Build();
        return (new SceneEditor(scene), scene, square, builder);
    }

    private static BezierShape Bezier(Edge edge) => Assert.IsType<BezierShape>(edge.Shape);

    private static void AssertNear(Vector2 expected, Vector2 actual) =>
        Assert.True(Vector2.Distance(expected, actual) < Eps, $"Oczekiwano {expected}, jest {actual}");

    private static void AssertAllSatisfied(Scene scene) =>
        Assert.All(scene.Constraints, c => Assert.True(c.IsSatisfied(0.01f), $"Niespełnione: {c.GetType().Name}"));

    // ---------- Domyślne C1 ----------

    [Fact]
    public void ConvertToBezier_SetsC1OnBothEndsImplicitly()
    {
        var (editor, _, square, _) = CreateSquare();

        editor.ConvertToBezier(square.Edges[0]);

        Assert.Equal(Continuity.C1, square.Vertices[0].Continuity);
        Assert.Equal(Continuity.C1, square.Vertices[1].Continuity);
        Assert.False(square.Vertices[0].ContinuityExplicit);
        Assert.False(square.Vertices[1].ContinuityExplicit);
    }

    [Fact]
    public void ConvertToBezier_C1WithSegments_HandlesOnExtensionsAtOneThird()
    {
        // V0: przed nim odcinek V3→V0 (w górę), więc uchwyt 1 = V0 + (V0 − V3)/3 = (0, −30)
        // V1: za nim odcinek V1→V2 (w dół), więc uchwyt 2 = V1 − (V2 − V1)/3 = (90, −30)
        var (editor, scene, square, _) = CreateSquare();

        editor.ConvertToBezier(square.Edges[0]);

        var bezier = Bezier(square.Edges[0]);
        AssertNear(new(0, -30), bezier.C1.Position);
        AssertNear(new(90, -30), bezier.C2.Position);
        AssertAllSatisfied(scene);
    }

    [Fact]
    public void ConvertToBezier_DoesNotMoveVertices()
    {
        var (editor, _, square, _) = CreateSquare();
        var before = square.Vertices.Select(v => v.Position).ToList();

        editor.ConvertToBezier(square.Edges[0]);

        Assert.Equal(before, square.Vertices.Select(v => v.Position));
    }

    [Fact]
    public void ConvertToBezier_ExplicitContinuity_IsNotOverridden()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.SetContinuity(square.Vertices[0], Continuity.G0);

        editor.ConvertToBezier(square.Edges[0]);

        Assert.Equal(Continuity.G0, square.Vertices[0].Continuity);
        Assert.Equal(Continuity.C1, square.Vertices[1].Continuity);
        AssertNear(new(30, 0), Bezier(square.Edges[0]).C1.Position);   // G0 → uchwyt tam, gdzie go zainicjowano
    }

    [Fact]
    public void SetContinuity_MarksExplicit()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);

        editor.SetContinuity(square.Vertices[0], Continuity.G1);

        Assert.Equal(Continuity.G1, square.Vertices[0].Continuity);
        Assert.True(square.Vertices[0].ContinuityExplicit);
    }

    // ---------- Które ograniczenia istnieją (RebuildContinuityConstraints) ----------

    [Fact]
    public void Rebuild_OneConstraintPerNonG0VertexNextToBezier()
    {
        var (editor, scene, square, _) = CreateSquare();

        editor.ConvertToBezier(square.Edges[0]);

        var continuity = scene.Constraints.OfType<ContinuityConstraint>().ToList();
        Assert.Equal(2, continuity.Count);
        Assert.All(continuity, c => Assert.Equal(Continuity.C1, c.Kind));
    }

    [Fact]
    public void SetContinuity_BetweenTwoSegments_CreatesNoConstraint()
    {
        var (editor, scene, square, _) = CreateSquare();

        editor.SetContinuity(square.Vertices[2], Continuity.C1);

        Assert.Empty(scene.Constraints);
    }

    [Fact]
    public void SetContinuity_G0_RemovesConstraintOfThatVertex()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);

        editor.SetContinuity(square.Vertices[0], Continuity.G0);

        Assert.Single(scene.Constraints.OfType<ContinuityConstraint>());
    }

    [Fact]
    public void ConvertToLine_RemovesContinuityConstraints()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);

        editor.ConvertToLine(square.Edges[0]);

        Assert.Empty(scene.Constraints.OfType<ContinuityConstraint>());
    }

    [Fact]
    public void InsertVertexOnBezier_NoBezierLeft_NoContinuityConstraints()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);

        editor.InsertVertexAtEdgeMidpoint(square, square.Edges[0]);

        Assert.Empty(scene.Constraints.OfType<ContinuityConstraint>());
    }

    [Fact]
    public void DeleteVertexNextToBezier_RebuildsForNewEdges()
    {
        // pięciokąt; Bézier na V0→V1, usuwamy V4 (sąsiad V0) → V0 nadal ma C1 z nową krawędzią V3→V0
        var builder = new SceneBuilder();
        var pentagon = builder.Polygon((0, 0), (90, 0), (90, 90), (45, 120), (0, 90));
        var scene = builder.Build();
        var editor = new SceneEditor(scene);
        editor.ConvertToBezier(pentagon.Edges[0]);

        editor.DeleteVertex(pentagon, pentagon.Vertices[4]);

        var atV0 = scene.Constraints.OfType<ContinuityConstraint>()
                        .Single(c => c.Edges.Contains(pentagon.Edges[0]) && c.Edges.Contains(pentagon.IncomingEdge(pentagon.Vertices[0])));
        Assert.Contains(pentagon.Edges[^1], atV0.Edges);
    }

    // ---------- Przeciąganie ----------

    [Fact]
    public void MovePoint_VertexWithC1_HandleFollowsToOneThird()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var target = new Vector2(20, -10);

        Assert.True(editor.MovePoint(square, square.Vertices[0], target));

        AssertNear(target, square.Vertices[0].Position);
        var v3 = square.Vertices[3].Position;
        AssertNear(target + (target - v3) / 3f, Bezier(square.Edges[0]).C1.Position);
        AssertAllSatisfied(scene);
    }

    [Fact]
    public void MovePoint_HandleWithC1NextToSegment_MovesFarEndOfSegment()
    {
        // ciągniemy uchwyt 1 do (0, −60): C1 wymaga V0 − V3 = 3·(uchwyt − V0) = (0, −180) → V3 = (0, 180)
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var handle = Bezier(square.Edges[0]).C1;

        Assert.True(editor.MovePoint(square, handle, new(0, -60)));

        AssertNear(new(0, -60), handle.Position);
        AssertNear(new(0, 0), square.Vertices[0].Position);    // wierzchołek segmentu stoi
        AssertNear(new(0, 180), square.Vertices[3].Position);  // koniec sąsiedniego odcinka się przesunął
        AssertAllSatisfied(scene);
    }

    [Fact]
    public void MovePoint_HandleWithG1_KeepsSegmentEndDistance()
    {
        // G1: ciągnięcie uchwytu w dół osi y (ten sam kierunek) nic nie wymusza
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        var handle = Bezier(square.Edges[0]).C1;

        Assert.True(editor.MovePoint(square, handle, new(0, -60)));

        AssertNear(new(0, 90), square.Vertices[3].Position);
    }

    [Fact]
    public void MovePoint_G1_HandleKeepsItsDistanceAndTurns()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        var handle = Bezier(square.Edges[0]).C1;
        float distBefore = Vector2.Distance(handle.Position, square.Vertices[0].Position);

        Assert.True(editor.MovePoint(square, square.Vertices[3], new(30, 90)));

        var v0 = square.Vertices[0].Position;
        var expectedDir = Vector2.Normalize(v0 - new Vector2(30, 90));
        AssertNear(v0 + expectedDir * distBefore, handle.Position);
        AssertAllSatisfied(scene);
    }

    [Fact]
    public void TwoBeziersWithC1_DraggingOneHandle_MirrorsTheOther()
    {
        // V0 między Bézierem V3→V0 (uchwyt 2) i Bézierem V0→V1 (uchwyt 1): C1 ⇒ uchwyty symetryczne względem V0
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        editor.ConvertToBezier(square.Edges[3]);
        var outHandle = Bezier(square.Edges[0]).C1;
        var inHandle = Bezier(square.Edges[3]).C2;

        Assert.True(editor.MovePoint(square, outHandle, new(-20, -40)));

        AssertNear(new(20, 40), inHandle.Position);
        AssertAllSatisfied(scene);
    }

    [Fact]
    public void C1WithLengthOnNeighbourSegment_BothSatisfiedAfterDrag()
    {
        var (editor, scene, square, builder) = CreateSquare();
        builder.Length(square.Edges[3], 90);
        editor.ConvertToBezier(square.Edges[0]);

        Assert.True(editor.MovePoint(square, square.Vertices[0], new(15, -15)));

        Assert.Equal(90, square.Edges[3].Length, 0.05f);
        AssertAllSatisfied(scene);
    }

    // ---------- Sam warunek ----------

    [Fact]
    public void G1_HandlePointingBack_IsNotSatisfied()
    {
        // uchwyt po złej stronie V0 (w kierunku V3) — ta sama prosta, przeciwny zwrot
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        var atV0 = scene.Constraints.OfType<ContinuityConstraint>().Single(c => c.Edges.Contains(square.Edges[3]));

        Bezier(square.Edges[0]).C1.Position = new(0, 30);

        Assert.False(atV0.IsSatisfied(0.01f));
    }

    [Fact]
    public void C1_SameDirectionWrongLength_IsNotSatisfiedButG1Is()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);
        var handle = Bezier(square.Edges[0]).C1;
        handle.Position = new(0, -50);   // dobry kierunek, zła odległość (powinno być 30)

        var c1 = scene.Constraints.OfType<ContinuityConstraint>().Single(c => c.Edges.Contains(square.Edges[3]));
        Assert.False(c1.IsSatisfied(0.01f));

        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        handle.Position = new(0, -50);   // SetContinuity mógł poprawić — ustawiamy z powrotem
        var g1 = scene.Constraints.OfType<ContinuityConstraint>().Single(c => c.Edges.Contains(square.Edges[3]));
        Assert.True(g1.IsSatisfied(0.01f));
    }
}
