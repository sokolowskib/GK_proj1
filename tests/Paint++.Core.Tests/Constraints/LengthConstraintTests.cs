using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Constraints;

public class LengthConstraintTests
{
    private const float Eps = 0.01f;

    private static Edge CreateEdge(float ax, float ay, float bx, float by) =>
        new(new Vertex(new Vector2(ax, ay)), new Vertex(new Vector2(bx, by)));

    private static HashSet<ScenePoint> Pinned(params ScenePoint[] points) => [.. points];

    private static void AssertPosition(Vector2 expected, ScenePoint actual)
    {
        Assert.Equal(expected.X, actual.Position.X, precision: 3);
        Assert.Equal(expected.Y, actual.Position.Y, precision: 3);
    }

    // ---------- Dane ograniczenia ----------

    [Fact]
    public void Edges_ContainsOnlyConstrainedEdge()
    {
        var edge = CreateEdge(0, 0, 10, 0);

        var constraint = new LengthConstraint(edge, 10);

        Assert.Same(edge, Assert.Single(constraint.Edges));
        Assert.Equal(10f, constraint.Length);
    }

    // ---------- IsSatisfied ----------

    [Fact]
    public void IsSatisfied_WhenLengthMatches_ReturnsTrue()
    {
        var edge = CreateEdge(0, 0, 6, 8); // |AB| = 10

        Assert.True(new LengthConstraint(edge, 10).IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_WhenLengthWithinEpsilon_ReturnsTrue()
    {
        var edge = CreateEdge(0, 0, 10.005f, 0);

        Assert.True(new LengthConstraint(edge, 10).IsSatisfied(Eps));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void IsSatisfied_WhenLengthDiffers_ReturnsFalse(float length)
    {
        var edge = CreateEdge(0, 0, 10, 0);

        Assert.False(new LengthConstraint(edge, length).IsSatisfied(Eps));
    }

    // ---------- Project: który punkt się rusza ----------

    [Fact]
    public void Project_StartPinned_MovesOnlyEndAlongEdge()
    {
        var edge = CreateEdge(0, 0, 6, 8); // kierunek (0.6, 0.8), długość 10
        var constraint = new LengthConstraint(edge, 5);

        constraint.Project(Pinned(edge.Start));

        AssertPosition(new Vector2(0, 0), edge.Start);
        AssertPosition(new Vector2(3, 4), edge.End);
    }

    [Fact]
    public void Project_EndPinned_MovesOnlyStartAlongEdge()
    {
        var edge = CreateEdge(0, 0, 6, 8);
        var constraint = new LengthConstraint(edge, 5);

        constraint.Project(Pinned(edge.End));

        AssertPosition(new Vector2(3, 4), edge.Start);
        AssertPosition(new Vector2(6, 8), edge.End);
    }

    [Fact]
    public void Project_StartPinned_CanAlsoLengthenEdge()
    {
        var edge = CreateEdge(0, 0, 6, 8);
        var constraint = new LengthConstraint(edge, 20);

        constraint.Project(Pinned(edge.Start));

        AssertPosition(new Vector2(12, 16), edge.End);
    }

    [Fact]
    public void Project_NothingPinned_MovesBothSymmetricallyAroundMidpoint()
    {
        var edge = CreateEdge(0, 0, 10, 0); // środek (5, 0)
        var constraint = new LengthConstraint(edge, 20);

        constraint.Project(Pinned());

        AssertPosition(new Vector2(-5, 0), edge.Start);
        AssertPosition(new Vector2(15, 0), edge.End);
    }

    [Fact]
    public void Project_BothPinned_MovesNothing()
    {
        var edge = CreateEdge(0, 0, 10, 0);
        var constraint = new LengthConstraint(edge, 20);

        constraint.Project(Pinned(edge.Start, edge.End));

        AssertPosition(new Vector2(0, 0), edge.Start);
        AssertPosition(new Vector2(10, 0), edge.End);
    }

    // ---------- Project: wynik ----------

    [Fact]
    public void Project_ThenIsSatisfied_ReturnsTrue()
    {
        var edge = CreateEdge(3, -7, 41, 12);
        var constraint = new LengthConstraint(edge, 25);

        constraint.Project(Pinned());

        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void Project_ZeroLengthEdge_StillReachesRequestedLength()
    {
        // Końce w tym samym miejscu → kierunek nieokreślony, nie wolno dzielić przez zero (NaN).
        var edge = CreateEdge(5, 5, 5, 5);
        var constraint = new LengthConstraint(edge, 10);

        constraint.Project(Pinned(edge.Start));

        AssertPosition(new Vector2(5, 5), edge.Start);
        Assert.False(float.IsNaN(edge.End.Position.X));
        Assert.True(constraint.IsSatisfied(Eps));
    }
}
