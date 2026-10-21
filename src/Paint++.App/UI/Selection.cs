using PaintPP.Core.Model;
namespace PaintPP.App.UI;

public class Selection
{
    public IReadOnlyList<Edge> Edges => _edges;

    private readonly List<Edge> _edges = [];
    public void Select(Edge edge)
    {
        _edges.Clear();
        _edges.Add(edge);
    }
    public void Toggle(Edge edge)
    {
        if (!_edges.Remove(edge))
        {
            _edges.Add(edge);
        }
    }
    public void Clear()
    {
        _edges.Clear();
    }
    public void RemoveMissing(Scene scene)
    {
        var list = scene.Polygons.SelectMany(p => p.Edges);  // lista wszystkich edge
        foreach (var e in _edges.ToList())
        {
            if (!list.Contains(e))
            {
                _edges.Remove(e);
            }
        }
    }
}
