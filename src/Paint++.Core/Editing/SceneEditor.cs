using PaintPP.Core.Constraints;
using PaintPP.Core.Model;
using System.Numerics;
namespace PaintPP.Core.Editing;

public class SceneEditor(Scene scene)
{
    private readonly ConstraintSolver _solver = new();
    public Vertex InsertVertexAtEdgeMidpoint(Polygon polygon, Edge edge)
    {
        RemoveConstraintsOf(edge);
        var mid = edge.Midpoint;

        var result = polygon.InsertVertex(edge, mid);
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
        return result;
    }
    public bool DeleteVertex(Polygon polygon, Vertex vertex)
    {
        if (polygon.Vertices.Count <= 3)
            return false;
        var e1 = polygon.IncomingEdge(vertex);
        var e2 = polygon.OutgoingEdge(vertex);
        RemoveConstraintsOf(e1);
        RemoveConstraintsOf(e2);
        polygon.RemoveVertex(vertex);
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
        return true;
    }

    public bool MovePoint(Polygon polygon, ScenePoint point, Vector2 target)
    {
        var snapshot = TakeSnapshot();

        point.Position = target;

        HashSet<ScenePoint> pinned = [point];

        if (_solver.Solve(scene.Constraints, pinned))  // solver sobie poradzil
            return true;

        RestoreSnapshot(snapshot);
        Vector2 delta = target - snapshot[point];
        polygon.Translate(delta);
        return false;
    }
    private void RemoveConstraintsOf(Edge edge)
    {
        scene.Constraints.RemoveAll(c => c.Edges.Contains(edge));
    }

    public bool AddConstraint(IConstraint constraint)
    {
        var snapshot = TakeSnapshot();

        scene.Constraints.Add(constraint);

        if (_solver.Solve(scene.Constraints, new HashSet<ScenePoint>()))
            return true;

        RestoreSnapshot(snapshot);
        scene.Constraints.Remove(constraint);
        return false;
    }
    public void RemoveConstraint(IConstraint constraint)
    {
        scene.Constraints.Remove(constraint);
    }
    public bool SetLength(Edge edge, float length)
    {
        if (length <= 0)
            return false;

        var length_constraints = scene.Constraints.OfType<LengthConstraint>();
        LengthConstraint? old = length_constraints.FirstOrDefault(p => p.Edges.Contains(edge));

        if (old != null)
            RemoveConstraint(old);

        bool result = AddConstraint(new LengthConstraint(edge, length));

        if (!result && old != null)
        {
            scene.Constraints.Add(old);
        }
        return result;
    }

    public bool AddPerpendicular(Edge a, Edge b)
    {
        if (a == b)
            return false;

        return AddConstraint(new PerpendicularConstraint(a, b));
    }

    public bool SolveAll() => _solver.Solve(scene.Constraints, new HashSet<ScenePoint>());

    private Dictionary<ScenePoint, Vector2> TakeSnapshot() => scene.AllPoints.ToDictionary(p => p, p => p.Position);
    private void SolveWithVerticesPinned()
    {
        _solver.Solve(scene.Constraints, scene.AllPoints.OfType<Vertex>().ToHashSet<ScenePoint>());
    }

    private static void RestoreSnapshot(Dictionary<ScenePoint, Vector2> dict)
    {
        foreach (var (p, pos) in dict)
        {
            p.Position = pos;
        }
    }
    public void TranslatePolygon(Polygon polygon, Vector2 delta)
    {
        polygon.Translate(delta);
    }

    public void ConvertToBezier(Edge edge)
    {
        RemoveConstraintsOf(edge);
        var a = edge.Start.Position;
        var b = edge.End.Position;
        var d = b - a;
        var t1 = a + d / 3f;
        var t2 = a + 2 * d / 3f;
        var shape = new BezierShape(new ScenePoint(t1), new ScenePoint(t2));
        edge.Shape = shape;

        if (!edge.Start.ContinuityExplicit &&
            PolygonOf(edge.Start)
                .IncomingEdge(edge.Start)
                .Shape is not ArcShape)  // nie istnieje jesli po drugiej stronie wierzcholka jest luk
            edge.Start.Continuity = Continuity.C1;
        if (!edge.End.ContinuityExplicit && PolygonOf(edge.End).OutgoingEdge(edge.End).Shape is not ArcShape)
            edge.End.Continuity = Continuity.C1;

        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
    }

    public bool SetContinuity(Vertex vertex, Continuity kind)
    {
        if (!CanSetContinuity(vertex, kind))
            return false;

        vertex.Continuity = kind;
        vertex.ContinuityExplicit = true;
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
        return true;
    }
    public bool CanSetContinuity(Vertex vertex, Continuity kind)
    {
        var polygon = PolygonOf(vertex);
        var incoming = polygon.IncomingEdge(vertex);
        var outgoing = polygon.OutgoingEdge(vertex);

        if (kind == Continuity.G0)
            return true;

        if (kind == Continuity.C1 && (incoming.Shape is ArcShape || outgoing.Shape is ArcShape))
            return false;

        if (kind == Continuity.G1)
        {
            if (outgoing.Shape is ArcShape && outgoing.End.Continuity == Continuity.G1)
                return false;
            if (incoming.Shape is ArcShape && incoming.Start.Continuity == Continuity.G1)
                return false;
        }
        return true;
    }
    private Polygon PolygonOf(Vertex vertex) => scene.Polygons.First(p => p.Vertices.Contains(vertex));
    public void ConvertToLine(Edge edge)
    {
        edge.Shape = new LineShape();
        scene.RebuildContinuityConstraints();
    }

    // Usunięta krawędź przestaje być rysowana i staje się zwykłym odcinkiem - po przywróceniu wraca jako odcinek,
    // niezależnie czy wcześniej była Bézierem czy łukiem.
    public bool RemoveEdge(Polygon polygon, Edge edge)
    {
        if (polygon.RemovedEdge != null)
            return false;
        RemoveConstraintsOf(edge);
        edge.Shape = new LineShape();
        polygon.RemoveEdge(edge);
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
        return true;
    }

    public void RestoreEdge(Polygon polygon)
    {
        polygon.RestoreEdge();
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
    }

    public void ConvertToArc(Edge edge)
    {
        RemoveConstraintsOf(edge);
        edge.Shape = new ArcShape();

        if (edge.Start.Continuity == Continuity.C1)
        {
            edge.Start.Continuity = Continuity.G0;
        }
        if (edge.End.Continuity == Continuity.C1)
        {
            edge.End.Continuity = Continuity.G0;
        }

        if (edge.End.Continuity == Continuity.G1 && !CanSetContinuity(edge.End, Continuity.G1))
            edge.End.Continuity = Continuity.G0;
        if (edge.Start.Continuity == Continuity.G1 && !CanSetContinuity(edge.Start, Continuity.G1))
            edge.Start.Continuity = Continuity.G0;
        scene.RebuildContinuityConstraints();
        SolveWithVerticesPinned();
    }
}
