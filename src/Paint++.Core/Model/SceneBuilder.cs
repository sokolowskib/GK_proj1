using System.Numerics;
using PaintPP.Core.Constraints;

namespace PaintPP.Core.Model;

public class SceneBuilder
{
    private readonly Scene _scene = new();

    ///< summary>
    /// dodaje i zwraca polygon do sceny
    ///< summary>
    public Polygon Polygon(params(float X, float Y)[] points)
    {
        var polygon = new Polygon(points.Select(p => new Vector2(p.X, p.Y)));
        _scene.Polygons.Add(polygon);
        return polygon;
    }
    public LengthConstraint Length(Edge edge, float length)
    {
        var constraint = new LengthConstraint(edge, length);
        _scene.Constraints.Add(constraint);
        return constraint;
    }
    public Scene Build() => _scene;
}
