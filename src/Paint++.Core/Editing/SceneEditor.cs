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

        return polygon.InsertVertex(edge, mid);
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

    private Dictionary<ScenePoint, Vector2> TakeSnapshot() => scene.AllPoints.ToDictionary(p => p, p => p.Position);
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
    }
    public void ConvertToLine(Edge edge)
    {
        edge.Shape = new LineShape();
    }
}
