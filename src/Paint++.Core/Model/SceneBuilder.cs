using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;

namespace PaintPP.Core.Model;

public class SceneBuilder
{
    private readonly Scene _scene;
    private readonly SceneEditor _editor;

    public SceneBuilder()
    {
        _scene = new Scene();
        _editor = new SceneEditor(_scene);
    }

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
    public PerpendicularConstraint Perpendicular(Edge a, Edge b)
    {
        var constraint = new PerpendicularConstraint(a, b);
        _scene.Constraints.Add(constraint);
        return constraint;
    }
    ///< summary>
    /// zamienia krawedz na krzywa Beziera z podanymi punktami kontrolnymi (domyslna ciaglosc jak w edytorze)
    ///< summary>
    public BezierShape Bezier(Edge edge, (float X, float Y) c1, (float X, float Y) c2)
    {
        _editor.ConvertToBezier(edge);
        var shape = (BezierShape)edge.Shape;
        shape.C1.Position = new Vector2(c1.X, c1.Y);
        shape.C2.Position = new Vector2(c2.X, c2.Y);
        return shape;
    }
    public ArcShape Arc(Edge edge)
    {
        _editor.ConvertToArc(edge);
        return (ArcShape)edge.Shape;
    }
    public void SetContinuity(Vertex vertex, Continuity kind)
    {
        if (!_editor.SetContinuity(vertex, kind))
            throw new InvalidOperationException($"Nie można ustawić ciągłości {kind} w tym wierzchołku");
    }

    ///< summary>
    /// zwraca scene po jednorazowym uruchomieniu solvera, zeby ograniczenia byly spelnione od startu;
    /// sprzeczne ograniczenia to blad w kodzie sceny, wiec wyjatek
    ///< summary>
    public Scene Build()
    {
        if (!_editor.SolveAll())
            throw new InvalidOperationException("Ograniczenia sceny są sprzeczne");
        return _scene;
    }
}
