using System.Numerics;
using PaintPP.Core.Geometry;
using PaintPP.Core.Rasterization;

namespace PaintPP.Core.Model;

///< summary>
/// abstrakcyjna klasa przedstawiajaca kształt lini.
///< summary>
public abstract class EdgeShape
{
    public virtual IEnumerable<ScenePoint> ControlPoints => [];
    public abstract float DistanceTo(Vector2 start, Vector2 end, Vector2 point);
}

public sealed class LineShape : EdgeShape
{
    public override float DistanceTo(Vector2 start, Vector2 end,
                                     Vector2 point) => Geometry2D.DistanceToSegment(point, start, end);
}

public sealed class BezierShape(ScenePoint c1, ScenePoint c2) : EdgeShape
{
    public ScenePoint C1 { get; } = c1;
    public ScenePoint C2 { get; } = c2;
    public override IEnumerable<ScenePoint> ControlPoints => [C1, C2];
    private const int _steps = 32;
    public override float DistanceTo(Vector2 start, Vector2 end, Vector2 point)
    {
        float dist = float.MaxValue;

        var sample = BezierCurve.Sample(start, C1.Position, C2.Position, end, _steps).ToList();

        for (int i = 0; i < sample.Count - 1; ++i)
        {
            var temp_dist = Geometry2D.DistanceToSegment(point, sample[i], sample[i + 1]);
            if (temp_dist < dist)
                dist = temp_dist;
        }

        return dist;
    }
}

public sealed class ArcShape : EdgeShape
{
    public ScenePoint? StartTangentSource { get; set; }
    public ScenePoint? EndTangentSource { get; set; }
    public Edge? StartTangentArc { get; set; }

    public Vector2 DirectionAtEnd(Vector2 start, Vector2 end)
    {
        var points = Sample(start, end, 256);
        return points[^1] - points[^2];
    }

    public IReadOnlyList<Vector2> Sample(Vector2 start, Vector2 end, int steps)
    {
        Arc? arc;
        bool reversed = false;
        if (StartTangentArc?.Shape is ArcShape previous)
        {
            var dir = previous.DirectionAtEnd(StartTangentArc.Start.Position, StartTangentArc.End.Position);
            arc = ArcGeometry.FromTangent(start, end, dir);
        }
        else if (StartTangentSource != null)
        {
            arc = ArcGeometry.FromTangent(start, end, start - StartTangentSource.Position);
        }
        else if (EndTangentSource != null)
        {
            arc = ArcGeometry.FromTangent(end, start, end - EndTangentSource.Position);
            reversed = true;
        }
        else
        {
            var d = end - start;
            arc = ArcGeometry.FromTangent(start, end, new Vector2(d.Y, -d.X));
        }
        if (arc == null)
            return [start, end];
        var points = ArcGeometry.Sample(arc.Value, steps);
        return reversed ? points.Reverse().ToList() : [..points];
    }
    public override float DistanceTo(Vector2 start, Vector2 end, Vector2 point)
    {
        var points = Sample(start, end, 64);
        float dist = float.MaxValue;

        for (int i = 0; i < points.Count - 1; ++i)
        {
            var temp = Geometry2D.DistanceToSegment(point, points[i], points[i + 1]);
            if (temp < dist)
            {
                dist = temp;
            }
        }
        return dist;
    }
}
