using System.Numerics;
using PaintPP.Core.Geometry;

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
