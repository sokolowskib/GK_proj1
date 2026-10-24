using System.Numerics;
using PaintPP.Core.Constraints;
namespace PaintPP.Core.Model;

public class Scene
{
    public List<Polygon> Polygons { get; } = [];
    public List<IConstraint> Constraints { get; } = [];

    public PointHit? HitTestPoint(Vector2 pos, float radius)
    {
        PointHit? best = null;
        float bestDistance = radius;
        foreach (var polygon in Polygons)
        {
            foreach (var point in polygon.AllPoints)
            {
                float distance = Vector2.Distance(point.Position, pos);
                if (distance <= bestDistance)
                {
                    best = new PointHit(polygon, point);
                    bestDistance = distance;
                }
            }
        }
        return best;
    }
    public EdgeHit? HitTestEdge(Vector2 pos, float tolerance)
    {
        EdgeHit? best = null;
        float bestDistance = tolerance;
        foreach (var polygon in Polygons)
        {
            foreach (var edge in polygon.Edges)
            {
                float distance = edge.DistanceTo(pos);
                if (distance <= bestDistance)
                {
                    best = new EdgeHit(polygon, edge);
                    bestDistance = distance;
                }
            }
        }
        return best;
    }

    public void RebuildContinuityConstraints()
    {
        Constraints.RemoveAll(c => c is ContinuityConstraint);
        foreach (var p in Polygons)
        {
            foreach (var v in p.Vertices)
            {
                var incoming = p.IncomingEdge(v);
                var outgoing = p.OutgoingEdge(v);

                if (v.Continuity != Continuity.G0 && (incoming.Shape is BezierShape || outgoing.Shape is BezierShape))
                {
                    Constraints.Add(new ContinuityConstraint(v, incoming, outgoing, v.Continuity));
                }
            }
        }
    }
    public IEnumerable<ScenePoint> AllPoints => Polygons.SelectMany(p => p.AllPoints);
}
