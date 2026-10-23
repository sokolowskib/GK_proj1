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
