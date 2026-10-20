using PaintPP.Core.Constraints;
using PaintPP.Core.Model;
using System.Numerics;
namespace PaintPP.Core.Editing;

public class SceneEditor(Scene scene)
{
    public Vertex InsertVertexAtEdgeMidpoint(Polygon polygon, Edge edge)
    {
        var mid = edge.Midpoint;

        return polygon.InsertVertex(edge, mid);
    }
    public bool DeleteVertex(Polygon polygon, Vertex vertex)
    {
        if (polygon.Vertices.Count <= 3)
            return false;
        polygon.RemoveVertex(vertex);
        return true;
    }

    public void MovePoint(Polygon polygon, ScenePoint point, Vector2 target)
    {
        Dictionary<ScenePoint, Vector2> snapshot = new();
        foreach (var p in polygon.AllPoints)
        {
            snapshot.Add(p, p.Position);
        }

        point.Position = target;

        var solver = new ConstraintSolver();
        HashSet<ScenePoint> pinned = [point];

        if (solver.Solve(scene.Constraints, pinned))  // solver sobie poradzil
            return;
        foreach (var (p, pos) in snapshot)
        {
            p.Position = pos;
        }
        Vector2 delta = target - snapshot[point];
        polygon.Translate(delta);
    }
}
