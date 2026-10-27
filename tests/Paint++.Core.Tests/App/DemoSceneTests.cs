using System.Numerics;
using PaintPP.App;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.App;

// Każdy test zaczyna od świeżej sceny demo i sprawdza, że po operacji scena jest nadal poprawna.
public class DemoSceneTests
{
    private const float Epsilon = 0.05f;

    private static (Scene Scene, SceneEditor Editor) Fresh()
    {
        var scene = DemoScene.Create();
        return (scene, new SceneEditor(scene));
    }

    private static Polygon PolygonOf(Scene scene, ScenePoint point) =>
        scene.Polygons.First(p => p.AllPoints.Contains(point));

    private static void AssertValid(Scene scene)
    {
        var edges = scene.Polygons.SelectMany(p => p.Edges).ToHashSet();
        foreach (var point in scene.AllPoints)
        {
            Assert.True(float.IsFinite(point.Position.X) && float.IsFinite(point.Position.Y),
                        $"Punkt poza zakresem: {point.Position}");
        }
        foreach (var constraint in scene.Constraints)
        {
            Assert.All(constraint.Edges, e => Assert.Contains(e, edges));
            Assert.True(constraint.IsSatisfied(Epsilon), $"Niespełnione: {constraint.GetType().Name}");
        }
        foreach (var edge in edges)
        {
            if (edge.Shape is ArcShape)
                Assert.False(edge.Start.Continuity == Continuity.G1 && edge.End.Continuity == Continuity.G1,
                             "Łuk z G1 na obu końcach");
            Assert.True(float.IsFinite(edge.DistanceTo(Vector2.Zero)), "Nie da się spróbkować krawędzi");
        }
    }

    private static void AssertNear(Vector2 expected, Vector2 actual) =>
        Assert.True(Vector2.Distance(expected, actual) < Epsilon, $"Oczekiwano {expected}, jest {actual}");

    public static TheoryData<int, int> AllPoints()
    {
        var data = new TheoryData<int, int>();
        var scene = DemoScene.Create();
        for (int p = 0; p < scene.Polygons.Count; ++p)
            for (int i = 0; i < scene.Polygons[p].AllPoints.Count(); ++i)
                data.Add(p, i);
        return data;
    }

    public static TheoryData<int, int> AllVertices()
    {
        var data = new TheoryData<int, int>();
        var scene = DemoScene.Create();
        for (int p = 0; p < scene.Polygons.Count; ++p)
            for (int i = 0; i < scene.Polygons[p].Vertices.Count; ++i)
                data.Add(p, i);
        return data;
    }

    public static TheoryData<int, int> AllBezierControlPoints()
    {
        var data = new TheoryData<int, int>();
        var scene = DemoScene.Create();
        for (int p = 0; p < scene.Polygons.Count; ++p)
            for (int e = 0; e < scene.Polygons[p].Edges.Count; ++e)
                if (scene.Polygons[p].Edges[e].Shape is BezierShape)
                {
                    data.Add(p, e * 2);
                    data.Add(p, e * 2 + 1);
                }
        return data;
    }

    private static ScenePoint PointAt(Scene scene, int polygon, int index) =>
        scene.Polygons[polygon].AllPoints.ElementAt(index);

    // --- scena początkowa ---

    [Fact]
    public void Create_AllConstraintsSatisfied()
    {
        var (scene, _) = Fresh();

        AssertValid(scene);
    }

    [Fact]
    public void Create_MeetsSpecification()
    {
        var (scene, _) = Fresh();
        var edges = scene.Polygons.SelectMany(p => p.Edges).ToList();

        Assert.True(scene.Polygons.Count >= 2);
        Assert.NotEmpty(scene.Constraints);
        Assert.Contains(edges, e => e.Shape is BezierShape);
        Assert.Contains(edges, e => e.Shape is ArcShape);
        Assert.Contains(scene.Constraints.OfType<PerpendicularConstraint>(),
                        c => PolygonOf(scene, c.Edges[0].Start) != PolygonOf(scene, c.Edges[1].Start));
        Assert.Contains(scene.Constraints.OfType<ContinuityConstraint>(), c => c.Kind == Continuity.C1);
        Assert.Contains(scene.Constraints.OfType<ContinuityConstraint>(), c => c.Kind == Continuity.G1);
    }

    // --- przeciąganie ---

    [Theory]
    [MemberData(nameof(AllPoints))]
    public void MovePoint_Small_FollowsCursorAndKeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var point = PointAt(scene, polygon, index);
        var target = point.Position + new Vector2(15, -10);

        editor.MovePoint(PolygonOf(scene, point), point, target);

        AssertNear(target, point.Position);
        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllPoints))]
    public void MovePoint_Large_FollowsCursorAndKeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var point = PointAt(scene, polygon, index);
        var target = point.Position + new Vector2(300, 200);

        editor.MovePoint(PolygonOf(scene, point), point, target);

        AssertNear(target, point.Position);
        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllPoints))]
    public void MovePoint_ManySmallSteps_FollowsCursorAndKeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var point = PointAt(scene, polygon, index);
        var polygonOfPoint = PolygonOf(scene, point);

        for (int step = 0; step < 30; ++step)
        {
            var target = point.Position + new Vector2(4, 3);
            editor.MovePoint(polygonOfPoint, point, target);
            AssertNear(target, point.Position);
        }
        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void MoveVertex_OntoNextVertex_NoNaN(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];
        var vertex = poly.Vertices[index];
        var target = poly.OutgoingEdge(vertex).End.Position;

        editor.MovePoint(poly, vertex, target);

        AssertNear(target, vertex.Position);
        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllBezierControlPoints))]
    public void MoveControlPoint_DoesNotMoveOwnSegmentVertices(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];
        var edge = poly.Edges[index / 2];
        var bezier = (BezierShape)edge.Shape;
        var handle = index % 2 == 0 ? bezier.C1 : bezier.C2;
        var start = edge.Start.Position;
        var end = edge.End.Position;

        bool solved = editor.MovePoint(poly, handle, handle.Position + new Vector2(20, 15));

        Assert.True(solved);
        AssertNear(start, edge.Start.Position);
        AssertNear(end, edge.End.Position);
    }

    [Fact]
    public void TranslatePolygon_KeepsSceneValid()
    {
        var (scene, editor) = Fresh();

        foreach (var polygon in scene.Polygons)
            editor.TranslatePolygon(polygon, new Vector2(-40, 25));

        AssertValid(scene);
    }

    // --- edycja wierzchołków ---

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void DeleteVertex_KeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];

        editor.DeleteVertex(poly, poly.Vertices[index]);

        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void InsertVertex_KeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];

        editor.InsertVertexAtEdgeMidpoint(poly, poly.Edges[index]);

        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void DeleteThenDrag_FollowsCursor(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];
        editor.DeleteVertex(poly, poly.Vertices[index]);
        var vertex = poly.Vertices[0];
        var target = vertex.Position + new Vector2(25, 25);

        editor.MovePoint(poly, vertex, target);

        AssertNear(target, vertex.Position);
        AssertValid(scene);
    }

    // --- zmiana kształtu krawędzi ---

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void ConvertToLine_KeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();

        editor.ConvertToLine(scene.Polygons[polygon].Edges[index]);

        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void ConvertToBezier_KeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();

        editor.ConvertToBezier(scene.Polygons[polygon].Edges[index]);

        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void ConvertToArc_KeepsSceneValid(int polygon, int index)
    {
        var (scene, editor) = Fresh();

        editor.ConvertToArc(scene.Polygons[polygon].Edges[index]);

        AssertValid(scene);
    }

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void ConvertToArcThenDrag_FollowsCursor(int polygon, int index)
    {
        var (scene, editor) = Fresh();
        var poly = scene.Polygons[polygon];
        var edge = poly.Edges[index];
        editor.ConvertToArc(edge);
        var target = edge.End.Position + new Vector2(30, -20);

        editor.MovePoint(poly, edge.End, target);

        AssertNear(target, edge.End.Position);
        AssertValid(scene);
    }

    // --- ciągłość ---

    [Theory]
    [MemberData(nameof(AllVertices))]
    public void SetContinuity_EveryAllowedKind_KeepsSceneValid(int polygon, int index)
    {
        foreach (var kind in Enum.GetValues<Continuity>())
        {
            var (scene, editor) = Fresh();
            var vertex = scene.Polygons[polygon].Vertices[index];

            if (editor.CanSetContinuity(vertex, kind))
                Assert.True(editor.SetContinuity(vertex, kind));
            else
                Assert.False(editor.SetContinuity(vertex, kind));

            AssertValid(scene);
        }
    }

    // --- ograniczenia ---

    [Fact]
    public void RemoveAllUserConstraints_ThenDrag_SolverSucceeds()
    {
        var (scene, editor) = Fresh();
        foreach (var c in scene.Constraints.Where(c => c is not ContinuityConstraint).ToList())
            editor.RemoveConstraint(c);
        var vertex = scene.Polygons[0].Vertices[0];

        bool solved = editor.MovePoint(scene.Polygons[0], vertex, vertex.Position + new Vector2(30, 30));

        Assert.True(solved);
        AssertValid(scene);
    }

    [Fact]
    public void AddPerpendicularToEdgeWithTwoPerpendiculars_RejectedOrSatisfied()
    {
        var (scene, editor) = Fresh();
        var shape = scene.Polygons[0];
        var quad = scene.Polygons[1];

        editor.AddPerpendicular(quad.Edges[2], shape.Edges[0]);

        AssertValid(scene);
    }
}
