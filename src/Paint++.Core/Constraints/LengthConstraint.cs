using System.Numerics;
namespace PaintPP.Core.Constraints;
using PaintPP.Core.Model;

public class LengthConstraint(Edge edge, float length) : IConstraint
{
    public float Length { get; set; } = length;
    public IReadOnlyList<Edge> Edges { get; } = [edge];
    public bool IsSatisfied(float epsilon) => MathF.Abs(edge.Length - Length) < epsilon;

    public void Project(IReadOnlySet<ScenePoint> pinned)
    {
        bool startPinned = pinned.Contains(edge.Start);
        bool endPinned = pinned.Contains(edge.End);
        if (startPinned && endPinned)  // oba konce przypiete, nie mozna nic zrobic
            return;

        var delta = edge.End.Position - edge.Start.Position;
        var direction = delta.LengthSquared() > 1e-12f ? Vector2.Normalize(delta) : Vector2.UnitX;

        if (startPinned)
        {
            edge.End.Position = edge.Start.Position + direction * Length;
        }
        else if (endPinned)
        {
            edge.Start.Position = edge.End.Position - direction * Length;
        }
        else
        {
            Vector2 mid = edge.Midpoint;
            edge.Start.Position = mid - (direction * Length / 2f);
            edge.End.Position = mid + (direction * Length / 2f);
        }
    }
}
