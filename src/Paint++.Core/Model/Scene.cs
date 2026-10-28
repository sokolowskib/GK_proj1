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
        RebuildBezierContinuity();
        RebuildArcTangents();
    }

    private void RebuildBezierContinuity()
    {
        Constraints.RemoveAll(c => c is ContinuityConstraint);
        foreach (var p in Polygons)
        {
            foreach (var v in p.Vertices)
            {
                var incoming = p.IncomingEdge(v);
                var outgoing = p.OutgoingEdge(v);

                if (v.Continuity == Continuity.G0)  // nie ma co pilnowac
                    continue;
                if (incoming == p.RemovedEdge || outgoing == p.RemovedEdge)  // z niewidoczna krawedzia nie ma ciaglosci
                    continue;
                if (incoming.Shape is ArcShape ||
                    outgoing.Shape is
                        ArcShape)  // jesli ktores z krawedzi to luk to ten luk ma pilnowac ciaglosci w punkcie
                    continue;
                if (incoming.Shape is BezierShape || outgoing.Shape is BezierShape)
                    Constraints.Add(new ContinuityConstraint(v, incoming, outgoing, v.Continuity));
            }
        }
    }
    private void RebuildArcTangents()
    {
        foreach (var p in Polygons)
        {
            foreach (var e in p.Edges)
            {
                if (e.Shape is not ArcShape arc)
                    continue;
                var previous = p.IncomingEdge(e.Start);
                var next = p.OutgoingEdge(e.End);
                arc.StartTangentSource = null;
                arc.StartTangentArc = null;

                if (e.Start.Continuity == Continuity.G1 && previous != p.RemovedEdge)
                {
                    if (previous.Shape is ArcShape)
                    {
                        arc.StartTangentArc = previous;
                    }
                    else if (previous.Shape is BezierShape bs)
                    {
                        arc.StartTangentSource = bs.C2;
                    }
                    else
                    {
                        arc.StartTangentSource = previous.Start;
                    }
                }
                arc.EndTangentSource = null;
                if (e.End.Continuity == Continuity.G1 && next.Shape is not ArcShape && next != p.RemovedEdge)
                {
                    if (next.Shape is BezierShape bs)
                    {
                        arc.EndTangentSource = bs.C1;
                    }
                    else
                    {
                        arc.EndTangentSource = next.End;
                    }
                }
            }
        }
    }

    public IEnumerable<ScenePoint> AllPoints => Polygons.SelectMany(p => p.AllPoints);
}
