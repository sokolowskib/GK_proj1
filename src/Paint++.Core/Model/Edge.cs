using System.Numerics;

namespace PaintPP.Core.Model;

public class Edge(Vertex start, Vertex end)
{
    public Vertex Start { get; } = start;
    public Vertex End { get; } = end;
    public EdgeShape Shape { get; set; } = new LineShape();

    public Vector2 Midpoint => (Start.Position + End.Position) / 2f;
    public float Length => Vector2.Distance(Start.Position, End.Position);
    public float DistanceTo(Vector2 point) => Shape.DistanceTo(Start.Position, End.Position, point);
}
