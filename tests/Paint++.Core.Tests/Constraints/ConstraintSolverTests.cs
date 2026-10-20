using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Constraints;

public class ConstraintSolverTests
{
    private const float Eps = 0.01f;

    private static Vertex V(float x, float y) => new(new Vector2(x, y));

    private static HashSet<ScenePoint> Pinned(params ScenePoint[] points) => [.. points];

    private static ConstraintSolver CreateSolver() => new() { Epsilon = Eps, MaxIterations = 100 };

    // ---------- Przypadki trywialne ----------

    [Fact]
    public void Solve_NoConstraints_ReturnsTrue()
    {
        Assert.True(CreateSolver().Solve([], Pinned()));
    }

    [Fact]
    public void Solve_AlreadySatisfied_ReturnsTrueAndMovesNothing()
    {
        var a = V(0, 0);
        var b = V(10, 0);
        var constraint = new LengthConstraint(new Edge(a, b), 10);

        bool solved = CreateSolver().Solve([constraint], Pinned());

        Assert.True(solved);
        Assert.Equal(new Vector2(0, 0), a.Position);
        Assert.Equal(new Vector2(10, 0), b.Position);
    }

    // ---------- Pojedyncze ograniczenie ----------

    [Fact]
    public void Solve_SingleLength_ReturnsTrueAndSatisfiesConstraint()
    {
        var a = V(0, 0);
        var b = V(10, 0);
        var constraint = new LengthConstraint(new Edge(a, b), 30);

        bool solved = CreateSolver().Solve([constraint], Pinned(a));

        Assert.True(solved);
        Assert.True(constraint.IsSatisfied(Eps));
        Assert.Equal(new Vector2(0, 0), a.Position); // przypięty się nie ruszył
    }

    [Fact]
    public void Solve_SatisfiedByLastAllowedIteration_ReturnsTrue()
    {
        // Jedna iteracja = jedno Project, które od razu spełnia ograniczenie.
        // Solver musi to sprawdzić po pętli, a nie zakładać porażki.
        var a = V(0, 0);
        var b = V(10, 0);
        var constraint = new LengthConstraint(new Edge(a, b), 30);
        var solver = new ConstraintSolver { Epsilon = Eps, MaxIterations = 1 };

        Assert.True(solver.Solve([constraint], Pinned(a)));
    }

    // ---------- Łańcuch: ograniczenia dzielą wierzchołek ----------

    [Fact]
    public void Solve_ChainOfLengths_SatisfiesAllConstraints()
    {
        // A — B — C, wspólny wierzchołek B: poprawienie AB psuje BC i odwrotnie,
        // więc jedno przejście nie wystarczy — solver musi iterować.
        var a = V(0, 0);
        var b = V(10, 0);
        var c = V(20, 5);
        var ab = new LengthConstraint(new Edge(a, b), 5);
        var bc = new LengthConstraint(new Edge(b, c), 5);

        bool solved = CreateSolver().Solve([ab, bc], Pinned(a));

        Assert.True(solved);
        Assert.True(ab.IsSatisfied(Eps));
        Assert.True(bc.IsSatisfied(Eps));
        Assert.Equal(new Vector2(0, 0), a.Position);
    }

    [Fact]
    public void Solve_TriangleWithLengths_SatisfiesAllConstraints()
    {
        var a = V(0, 0);
        var b = V(10, 0);
        var c = V(5, 10);
        IConstraint[] constraints =
        [
            new LengthConstraint(new Edge(a, b), 30),
            new LengthConstraint(new Edge(b, c), 40),
            new LengthConstraint(new Edge(c, a), 50),
        ];

        bool solved = CreateSolver().Solve(constraints, Pinned(a));

        Assert.True(solved);
        Assert.All(constraints, c => Assert.True(c.IsSatisfied(Eps)));
    }

    // ---------- Ograniczenia sprzeczne ----------

    [Fact]
    public void Solve_ContradictoryLengthsOnSameEdge_ReturnsFalse()
    {
        var edge = new Edge(V(0, 0), V(10, 0));

        bool solved = CreateSolver().Solve(
            [new LengthConstraint(edge, 10), new LengthConstraint(edge, 20)],
            Pinned());

        Assert.False(solved);
    }

    [Fact]
    public void Solve_TriangleInequalityViolated_ReturnsFalse()
    {
        // 10 + 10 < 100 → takiego trójkąta nie ma.
        var a = V(0, 0);
        var b = V(10, 0);
        var c = V(5, 10);

        bool solved = CreateSolver().Solve(
            [
                new LengthConstraint(new Edge(a, b), 10),
                new LengthConstraint(new Edge(b, c), 10),
                new LengthConstraint(new Edge(c, a), 100),
            ],
            Pinned());

        Assert.False(solved);
    }

    [Fact]
    public void Solve_BothEndsPinnedWithWrongLength_ReturnsFalse()
    {
        var a = V(0, 0);
        var b = V(10, 0);

        bool solved = CreateSolver().Solve([new LengthConstraint(new Edge(a, b), 20)], Pinned(a, b));

        Assert.False(solved);
    }
}
