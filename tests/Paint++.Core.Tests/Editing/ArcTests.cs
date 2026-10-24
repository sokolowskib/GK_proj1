using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Editing;

public class ArcTests
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

    private static ArcShape Arc(Edge edge) => Assert.IsType<ArcShape>(edge.Shape);

    private static IReadOnlyList<Vector2> Points(Edge edge, int steps = 64) =>
        Arc(edge).Sample(edge.Start.Position, edge.End.Position, steps);

    private static void AssertNear(Vector2 expected, Vector2 actual, float eps = Eps) =>
        Assert.True(Vector2.Distance(expected, actual) < eps, $"Oczekiwano {expected}, jest {actual}");

    // kierunek wyjścia łuku z jego początku (z gęstych próbek)
    private static Vector2 StartDir(Edge edge)
    {
        var pts = Points(edge, 2000);
        return Vector2.Normalize(pts[1] - pts[0]);
    }

    // ---------- ConvertToArc ----------

    [Fact]
    public void ConvertToArc_G0_IsSemicircleThroughBothEnds()
    {
        var (editor, _, square, _) = CreateSquare();
        var edge = square.Edges[0];

        editor.ConvertToArc(edge);

        var pts = Points(edge);
        AssertNear(edge.Start.Position, pts[0]);
        AssertNear(edge.End.Position, pts[^1]);
        var mid = new Vector2(45, 0);
        Assert.All(pts, p => Assert.Equal(45f, Vector2.Distance(p, mid), 0.05f));   // r = |AB|/2, środek w połowie
    }

    [Fact]
    public void ConvertToArc_RemovesConstraintsOfThatEdge()
    {
        var (editor, scene, square, builder) = CreateSquare();
        builder.Length(square.Edges[0], 90);
        var kept = builder.Length(square.Edges[2], 90);

        editor.ConvertToArc(square.Edges[0]);

        Assert.Equal([kept], scene.Constraints);
    }

    [Fact]
    public void ConvertToArc_C1AtEnds_BecomesG0()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[0]);   // V0, V1 → C1

        editor.ConvertToArc(square.Edges[0]);

        Assert.Equal(Continuity.G0, square.Vertices[0].Continuity);
        Assert.Equal(Continuity.G0, square.Vertices[1].Continuity);
    }

    [Fact]
    public void ConvertToArc_BothEndsG1_KeepsStartOnly()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        editor.SetContinuity(square.Vertices[1], Continuity.G1);

        editor.ConvertToArc(square.Edges[0]);

        Assert.Equal(Continuity.G1, square.Vertices[0].Continuity);
        Assert.Equal(Continuity.G0, square.Vertices[1].Continuity);
    }

    [Fact]
    public void ConvertToArc_DoesNotMoveVertices()
    {
        var (editor, _, square, _) = CreateSquare();
        var before = square.Vertices.Select(v => v.Position).ToList();

        editor.ConvertToArc(square.Edges[0]);

        Assert.Equal(before, square.Vertices.Select(v => v.Position));
    }

    [Fact]
    public void ConvertToBezier_NextToArc_DoesNotSetC1OnSharedVertex()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);

        editor.ConvertToBezier(square.Edges[1]);   // V1 dzielony z łukiem

        Assert.NotEqual(Continuity.C1, square.Vertices[1].Continuity);
        Assert.Equal(Continuity.C1, square.Vertices[2].Continuity);
    }

    // ---------- CanSetContinuity ----------

    [Fact]
    public void CanSetContinuity_C1NextToArc_False()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);

        Assert.False(editor.CanSetContinuity(square.Vertices[0], Continuity.C1));
        Assert.False(editor.CanSetContinuity(square.Vertices[1], Continuity.C1));
        Assert.False(editor.SetContinuity(square.Vertices[0], Continuity.C1));
    }

    [Fact]
    public void CanSetContinuity_G1OnOtherEndOfArc_False()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);

        Assert.False(editor.CanSetContinuity(square.Vertices[1], Continuity.G1));
    }

    [Fact]
    public void CanSetContinuity_G1OnOtherEndOfIncomingArc_False()
    {
        // łuk V3→V0 (przychodzący do V0) ma G1 w V3 → G1 w V0 zabronione
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[3]);
        editor.SetContinuity(square.Vertices[3], Continuity.G1);

        Assert.False(editor.CanSetContinuity(square.Vertices[0], Continuity.G1));
    }

    [Fact]
    public void TwoArcs_G1OnOuterEndsThenMiddle_Rejected()
    {
        // X=V0 → V=V1 → Y=V2, łuki e0 i e1; G1 w X i Y dozwolone, potem G1 w V już nie
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.ConvertToArc(square.Edges[1]);

        Assert.True(editor.SetContinuity(square.Vertices[0], Continuity.G1));
        Assert.True(editor.SetContinuity(square.Vertices[2], Continuity.G1));
        Assert.False(editor.SetContinuity(square.Vertices[1], Continuity.G1));
    }

    [Fact]
    public void TwoArcs_G1OnFirstOuterEndThenMiddle_Rejected()
    {
        // tylko X ma G1 — G1 w V dałoby łukowi X→V G1 na obu końcach
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.ConvertToArc(square.Edges[1]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);

        Assert.False(editor.SetContinuity(square.Vertices[1], Continuity.G1));
    }

    [Fact]
    public void AllArcsG1Everywhere_NoInfiniteRecursion()
    {
        var (editor, _, square, _) = CreateSquare();
        foreach (var e in square.Edges.ToList())
            editor.ConvertToArc(e);
        foreach (var v in square.Vertices)
            editor.SetContinuity(v, Continuity.G1);

        Assert.True(square.Vertices.Count(v => v.Continuity == Continuity.G1) <= 2);
        foreach (var e in square.Edges)
            Assert.True(Points(e).Count > 1);
    }

    // ---------- Styczność ----------

    [Fact]
    public void G1AtStart_ArcLeavesInDirectionOfPreviousSegment()
    {
        // przed V0 odcinek V3→V0 (w górę ekranu: (0,−1)); łuk V0→V1 ma wyjść z V0 w górę
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);

        AssertNear(new(0, -1), StartDir(square.Edges[0]), 0.01f);
    }

    [Fact]
    public void G1AtEnd_ArcArrivesInDirectionOfNextSegment()
    {
        // za V1 odcinek V1→V2 (w dół: (0,1)); łuk V0→V1 ma dojść do V1 jadąc w dół
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[1], Continuity.G1);

        var pts = Points(square.Edges[0], 2000);
        AssertNear(new(0, 1), Vector2.Normalize(pts[^1] - pts[^2]), 0.01f);
        AssertNear(square.Vertices[1].Position, pts[^1]);
    }

    [Fact]
    public void G1NextToBezier_ArcFollowsHandle()
    {
        // Bézier V3→V0, łuk V0→V1, G1 w V0: łuk wychodzi w kierunku V0 − uchwyt 2
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[3]);
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        var handle = ((BezierShape)square.Edges[3].Shape).C2;

        editor.MovePoint(square, handle, new(-30, 20));

        var expected = Vector2.Normalize(square.Vertices[0].Position - handle.Position);
        AssertNear(expected, StartDir(square.Edges[0]), 0.01f);
    }

    [Fact]
    public void ArcNeighbourVertex_GetsNoContinuityConstraint()
    {
        // łuk pilnuje się sam — solver nie dostaje ograniczenia w wierzchołku przy łuku
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToBezier(square.Edges[3]);
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);

        Assert.DoesNotContain(scene.Constraints.OfType<ContinuityConstraint>(),
                              c => c.Edges.Contains(square.Edges[0]));
    }

    [Fact]
    public void TwoArcsG1_SecondArcContinuesFirst()
    {
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.ConvertToArc(square.Edges[1]);

        Assert.True(editor.SetContinuity(square.Vertices[1], Continuity.G1));

        var first = Points(square.Edges[0], 2000);
        var arrive = Vector2.Normalize(first[^1] - first[^2]);
        AssertNear(arrive, StartDir(square.Edges[1]), 0.02f);
    }

    [Fact]
    public void G1_TangentParallelToChord_DrawsStraightSegment()
    {
        // V3 przesunięty tak, że V3→V0 leży na przedłużeniu V0→V1 → łuk zdegenerowany do odcinka
        var (editor, _, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        editor.SetContinuity(square.Vertices[0], Continuity.G1);
        square.Vertices[3].Position = new(-50, 0);

        var pts = Points(square.Edges[0]);
        Assert.Equal(2, pts.Count);
    }

    [Fact]
    public void HitTestEdge_FindsArc()
    {
        var (editor, scene, square, _) = CreateSquare();
        editor.ConvertToArc(square.Edges[0]);
        var onArc = Points(square.Edges[0])[32];   // szczyt półokręgu

        var hit = scene.HitTestEdge(onArc, 3f);

        Assert.NotNull(hit);
        Assert.Same(square.Edges[0], hit.Edge);
    }
}
