using System.Numerics;
namespace PaintPP.Core.Constraints;
using PaintPP.Core.Model;

public class PerpendicularConstraint(Edge first, Edge second) : IConstraint
{
    public IReadOnlyList<Edge> Edges { get; } = [first, second];
    public bool IsSatisfied(float epsilon)
    {
        var dStart = first.End.Position - first.Start.Position;
        var dEnd = second.End.Position - second.Start.Position;

        if (dStart.Length() < 1e-5 || dEnd.Length() < 1e-5)
            return true;

        // jesli iloczyn skalarny pomijalnie maly mozna wnioskowac ze krawedzie sa prostopadle
        return MathF.Abs(Vector2.Dot(Vector2.Normalize(dStart), Vector2.Normalize(dEnd))) < epsilon;
    }

    public void Project(IReadOnlySet<ScenePoint> pinned)
    {
        int pinned_first = PinnedCount(pinned, first);
        int pinned_second = PinnedCount(pinned, second);

        Edge example = pinned_first > pinned_second ? first : second;
        Edge rotated = example == first ? second : first;

        if (PinnedCount(pinned, rotated) == 2 || first.Length < 1e-5 || second.Length < 1e-5)
            return;

        // wektory kierunku krawedzi
        var dA = example.End.Position - example.Start.Position;
        var dB = rotated.End.Position - rotated.Start.Position;

        var n = Vector2.Normalize(
            new Vector2(-dA.Y, dA.X));  // znormalizowana wartosc reprezentujaca kierunek prostopadly do wektora A
        if (Vector2.Dot(n, dB) < 0)     // lepiej zeby byla prostopadla w druga strone
            n = -n;

        float L = rotated.Length;

        var M = rotated.Midpoint;
        if (pinned.Contains(rotated.Start))
        {
            rotated.End.Position = rotated.Start.Position + n * L;
        }
        else if (pinned.Contains(rotated.End))
        {
            rotated.Start.Position = rotated.End.Position - n * L;
        }
        else
        {
            rotated.Start.Position = M - n * L / 2f;
            rotated.End.Position = M + n * L / 2f;
        }
    }
    private static int PinnedCount(IReadOnlySet<ScenePoint> pinned, Edge e)
    {
        var u = e.Start;
        var v = e.End;
        int count = 0;
        if (pinned.Contains(u))
            count++;
        if (pinned.Contains(v))
            count++;
        return count;
    }
}
