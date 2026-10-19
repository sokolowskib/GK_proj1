using System.Numerics;

namespace PaintPP.Core.Model;

public class Polygon
{
    private readonly List<Vertex> _vertices;
    private readonly List<Edge> _edges;

    public Polygon(IEnumerable<Vector2> points)
    {
        _vertices = points.Select(p => new Vertex(p)).ToList();

        if (_vertices.Count < 3)
        {
            throw new ArgumentException("Wielokąty muszą mieć przynajmniej 3 wierzchołki!", nameof(points));
        }
        _edges = [];

        for (int i = 0; i < _vertices.Count; i++)
        {
            Vertex start = _vertices[i];
            Vertex end = _vertices[Wrap(i + 1)];
            _edges.Add(new Edge(start, end));
        }
    }
    public IReadOnlyList<Vertex> Vertices => _vertices;
    public IReadOnlyList<Edge> Edges => _edges;
    public IEnumerable<ScenePoint> AllPoints => _vertices.Concat(_edges.SelectMany(e => e.Shape.ControlPoints));

    public Edge IncomingEdge(Vertex vertex) => _edges[Wrap(IndexOf(vertex) - 1)];
    public Edge OutgoingEdge(Vertex vertex) => _edges[IndexOf(vertex)];

    public void Translate(Vector2 delta)
    {
        foreach (var point in AllPoints)
        {
            point.Position += delta;
        }
    }
    private int IndexOf(Vertex vertex)
    {
        int index = _vertices.IndexOf(vertex);
        if (index < 0)
        {
            throw new ArgumentException("Nie ma takiego wierzchołka w tym wielokącie", nameof(vertex));
        }
        return index;
    }
    private int IndexOf(Edge edge)
    {
        int index = _edges.IndexOf(edge);
        if (index < 0)
            throw new ArgumentException("Nie ma takiej krawedzi w tym wielokacie", nameof(edge));
        return index;
    }

    private int Wrap(int index)
    {
        int count = _vertices.Count;
        if (index >= count)
            return index - count;
        if (index < 0)
            return index + count;
        return index;
    }
    ///< summary>
    /// Metoda przyjmuje krawedz A-B, a nastepnie rodziela ja na dwie Krawedzie A-X-B, usuwajac stara krawedz z
    /// wielokata, wstawiajac nowa w liste
    /// Prawdziwy (i zachowany bedzie) niezmiennik: E[i] = V[i] -> V[i+1]
    ///< summary>
    public Vertex InsertVertex(Edge edge, Vector2 point)
    {
        Vertex a = edge.Start;
        Vertex b = edge.End;
        Vertex x = new Vertex(point);
        int i = IndexOf(edge);
        _vertices.Insert(i + 1, x);

        Edge e1 = new Edge(a, x);
        Edge e2 = new Edge(x, b);

        _edges[i] = e1;
        _edges.Insert(i + 1, e2);
        return x;
    }
    public void RemoveVertex(Vertex vertex)
    {
        var e1 = IncomingEdge(vertex);
        var e2 = OutgoingEdge(vertex);

        int i = IndexOf(e1);

        _vertices.Remove(vertex);

        var v1 = e1.Start;
        var v2 = e2.End;
        var new_edge = new Edge(v1, v2);
        _edges[i] = new_edge;
        _edges.Remove(e2);
    }
}
