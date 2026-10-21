using System.Numerics;
using PaintPP.Core.Constraints;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.Core.Tests.Constraints;

public class PerpendicularConstraintTests
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

    private static Vector2 Direction(Edge e) => Vector2.Normalize(e.End.Position - e.Start.Position);

    // ---------- Dane ograniczenia ----------

    [Fact]
    public void Edges_ContainsBothEdges()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);

        var constraint = new PerpendicularConstraint(a, b);

        Assert.Equal([a, b], constraint.Edges);
    }

    // ---------- IsSatisfied ----------

    [Fact]
    public void IsSatisfied_Perpendicular_True()
    {
        var constraint = new PerpendicularConstraint(CreateEdge(0, 0, 10, 0), CreateEdge(20, 0, 20, 7));

        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_PerpendicularButOppositeDirection_True()
    {
        var constraint = new PerpendicularConstraint(CreateEdge(0, 0, 10, 0), CreateEdge(20, 7, 20, 0));

        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_Diagonal_False()
    {
        var constraint = new PerpendicularConstraint(CreateEdge(0, 0, 10, 0), CreateEdge(20, 0, 30, 10));

        Assert.False(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_Parallel_False()
    {
        var constraint = new PerpendicularConstraint(CreateEdge(0, 0, 10, 0), CreateEdge(0, 5, 10, 5));

        Assert.False(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_DoesNotDependOnEdgeLength()
    {
        // prawie prostopadłe: dla długich krawędzi nieznormalizowany Dot byłby ogromny
        var constraint = new PerpendicularConstraint(CreateEdge(0, 0, 1000, 0), CreateEdge(0, 0, 1, 1000));

        Assert.True(constraint.IsSatisfied(Eps));
    }

    [Fact]
    public void IsSatisfied_ZeroLengthEdge_TrueInsteadOfNaN()
    {
        var constraint = new PerpendicularConstraint(CreateEdge(5, 5, 5, 5), CreateEdge(0, 0, 10, 10));

        Assert.True(constraint.IsSatisfied(Eps));
    }

    // ---------- Project: wybór krawędzi i oś obrotu ----------

    [Fact]
    public void Project_NothingPinned_BecomesPerpendicularAndKeepsLengths()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        float lengthA = a.Length, lengthB = b.Length;
        var constraint = new PerpendicularConstraint(a, b);

        constraint.Project(Pinned());

        Assert.True(constraint.IsSatisfied(Eps));
        Assert.Equal(lengthA, a.Length, precision: 3);
        Assert.Equal(lengthB, b.Length, precision: 3);
    }

    [Fact]
    public void Project_NothingPinned_RotatesOnlyOneEdgeAroundItsMidpoint()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        var midA = a.Midpoint;
        var midB = b.Midpoint;
        var aStart = a.Start.Position;
        var bStart = b.Start.Position;

        new PerpendicularConstraint(a, b).Project(Pinned());

        bool aMoved = a.Start.Position != aStart;
        bool bMoved = b.Start.Position != bStart;
        Assert.True(aMoved ^ bMoved, "Dokładnie jedna krawędź powinna się obrócić");
        Assert.True(Vector2.Distance(midA, a.Midpoint) < 1e-3f);
        Assert.True(Vector2.Distance(midB, b.Midpoint) < 1e-3f);
    }

    [Fact]
    public void Project_ExampleFromExplanation()
    {
        // A pozioma z 1 przypiętym końcem → B obraca się wokół środka (22.5, 5) do pionu
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        float half = b.Length / 2f;

        new PerpendicularConstraint(a, b).Project(Pinned(a.Start));

        AssertPosition(new Vector2(22.5f, 5 - half), b.Start);
        AssertPosition(new Vector2(22.5f, 5 + half), b.End);
    }

    [Fact]
    public void Project_FirstHasPinnedEnd_RotatesSecondAndLeavesFirst()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);

        new PerpendicularConstraint(a, b).Project(Pinned(a.End));

        AssertPosition(new Vector2(0, 0), a.Start);
        AssertPosition(new Vector2(10, 0), a.End);
        Assert.True(new PerpendicularConstraint(a, b).IsSatisfied(Eps));
    }

    [Fact]
    public void Project_SecondHasPinnedEnd_RotatesFirstAndLeavesSecond()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);

        new PerpendicularConstraint(a, b).Project(Pinned(b.Start));

        AssertPosition(new Vector2(20, 0), b.Start);
        AssertPosition(new Vector2(25, 10), b.End);
        Assert.True(new PerpendicularConstraint(a, b).IsSatisfied(Eps));
    }

    [Fact]
    public void Project_RotatedEdgeStartPinned_RotatesAroundStart()
    {
        // A całkiem przypięta (2), B ma przypięty Start (1) → B obraca się wokół Start
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        float length = b.Length;

        new PerpendicularConstraint(a, b).Project(Pinned(a.Start, a.End, b.Start));

        AssertPosition(new Vector2(20, 0), b.Start);
        AssertPosition(new Vector2(20, length), b.End);
    }

    [Fact]
    public void Project_RotatedEdgeEndPinned_RotatesAroundEnd()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        float length = b.Length;

        new PerpendicularConstraint(a, b).Project(Pinned(a.Start, a.End, b.End));

        AssertPosition(new Vector2(25, 10), b.End);
        AssertPosition(new Vector2(25, 10 - length), b.Start);
    }

    [Fact]
    public void Project_BothEdgesFullyPinned_MovesNothing()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);

        new PerpendicularConstraint(a, b).Project(Pinned(a.Start, a.End, b.Start, b.End));

        AssertPosition(new Vector2(20, 0), b.Start);
        AssertPosition(new Vector2(25, 10), b.End);
        AssertPosition(new Vector2(0, 0), a.Start);
        AssertPosition(new Vector2(10, 0), a.End);
    }

    [Fact]
    public void Project_ZeroLengthEdge_MovesNothing()
    {
        var a = CreateEdge(5, 5, 5, 5);
        var b = CreateEdge(20, 0, 25, 10);

        new PerpendicularConstraint(a, b).Project(Pinned());

        AssertPosition(new Vector2(5, 5), a.Start);
        AssertPosition(new Vector2(20, 0), b.Start);
        AssertPosition(new Vector2(25, 10), b.End);
    }

    // ---------- Project: wybór ±n (bez przeskoku) ----------

    [Theory]
    [InlineData(20, 0, 25, 10)]   // B w dół-prawo
    [InlineData(25, 10, 20, 0)]   // B odwrócona: w górę-lewo
    [InlineData(20, 0, 15, 10)]   // B w dół-lewo
    [InlineData(20, 10, 25, 0)]   // B w górę-prawo
    public void Project_KeepsOrientation_EndpointsDoNotSwap(float bx1, float by1, float bx2, float by2)
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(bx1, by1, bx2, by2);
        var before = Direction(b);

        new PerpendicularConstraint(a, b).Project(Pinned(a.Start));

        Assert.True(Vector2.Dot(before, Direction(b)) > 0, "Krawędź obróciła się o więcej niż 90°");
    }

    [Fact]
    public void Project_AlreadyPerpendicular_MovesNothing()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 20, 7);

        new PerpendicularConstraint(a, b).Project(Pinned());

        AssertPosition(new Vector2(20, 0), b.Start);
        AssertPosition(new Vector2(20, 7), b.End);
        AssertPosition(new Vector2(0, 0), a.Start);
        AssertPosition(new Vector2(10, 0), a.End);
    }

    // ---------- Z solverem ----------

    [Fact]
    public void Solver_PerpendicularAndLengthOnSameEdge_BothSatisfied()
    {
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        var perpendicular = new PerpendicularConstraint(a, b);
        var length = new LengthConstraint(b, 30);

        bool solved = new ConstraintSolver().Solve([perpendicular, length], Pinned());

        Assert.True(solved);
        Assert.True(perpendicular.IsSatisfied(Eps));
        Assert.True(length.IsSatisfied(Eps));
    }

    [Fact]
    public void Solver_ChainOfPerpendiculars_Satisfied()
    {
        // A ⊥ B, B ⊥ C → A ∥ C; rozłączne krawędzie, więc da się
        var a = CreateEdge(0, 0, 10, 0);
        var b = CreateEdge(20, 0, 25, 10);
        var c = CreateEdge(40, 0, 45, 3);
        var ab = new PerpendicularConstraint(a, b);
        var bc = new PerpendicularConstraint(b, c);

        bool solved = new ConstraintSolver().Solve([ab, bc], Pinned());

        Assert.True(solved);
        Assert.True(ab.IsSatisfied(Eps));
        Assert.True(bc.IsSatisfied(Eps));
    }

    // ---------- Krawędzie sąsiednie (wspólny wierzchołek) ----------

    [Theory]
    [InlineData(-1)]  // nic nie przypięte (dodawanie z menu)
    [InlineData(0)]   // dalszy koniec pierwszej krawędzi
    [InlineData(1)]   // wspólny wierzchołek
    [InlineData(2)]   // dalszy koniec drugiej krawędzi
    public void Solver_AdjacentEdges_Converges(int pinnedVertex)
    {
        var builder = new SceneBuilder();
        var quad = builder.Polygon((0, 0), (100, 10), (130, 90), (20, 110));
        var perpendicular = new PerpendicularConstraint(quad.Edges[0], quad.Edges[1]);
        var length = new LengthConstraint(quad.Edges[0], quad.Edges[0].Length);
        var pinned = Pinned();
        if (pinnedVertex >= 0)
        {
            quad.Vertices[pinnedVertex].Position += new Vector2(15, -20);
            pinned.Add(quad.Vertices[pinnedVertex]);
        }

        bool solved = new ConstraintSolver().Solve([perpendicular, length], pinned);

        Assert.True(solved);
        Assert.True(perpendicular.IsSatisfied(Eps));
        Assert.True(length.IsSatisfied(Eps));
    }

    // ---------- Przeciąganie między wielokątami (SceneEditor.MovePoint) ----------

    [Fact]
    public void MovePoint_CrossPolygon_DraggedEdgeFollowsMouseOtherEdgeRotates()
    {
        // konsultacje 21.10: przy przeciąganiu krawędzi jednego wielokąta druga ma się dopasować
        var builder = new SceneBuilder();
        var p1 = builder.Polygon((0, 0), (100, 0), (100, 100), (0, 100));
        var p2 = builder.Polygon((300, 0), (400, 0), (400, 100), (300, 100));
        var scene = builder.Build();
        var a = p1.Edges[0];  // (0,0)→(100,0)
        var b = p2.Edges[1];  // (400,0)→(400,100) — już prostopadła
        var constraint = new PerpendicularConstraint(a, b);
        scene.Constraints.Add(constraint);
        var editor = new SceneEditor(scene);
        var p2Others = new[] { p2.Vertices[0], p2.Vertices[3] }.Select(v => v.Position).ToList();

        editor.MovePoint(p1, a.End, new Vector2(100, 40));  // A się przechyla

        AssertPosition(new Vector2(100, 40), a.End);   // pod kursorem
        AssertPosition(new Vector2(0, 0), a.Start);    // A nie jest obracana przez solver
        Assert.True(constraint.IsSatisfied(Eps));
        Assert.Equal(p2Others, new[] { p2.Vertices[0], p2.Vertices[3] }.Select(v => v.Position).ToList());
    }
}
