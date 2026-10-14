using System.Numerics;

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
    public Scene Build() => _scene;
}
