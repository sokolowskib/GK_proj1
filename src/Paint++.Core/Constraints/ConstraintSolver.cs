using PaintPP.Core.Model;
namespace PaintPP.Core.Constraints;

public class ConstraintSolver
{
    public int MaxIterations { get; set; } = 100;
    public float Epsilon { get; set; } = 0.01f;

    public bool Solve(IReadOnlyList<IConstraint> constraints, IReadOnlySet<ScenePoint> pinned)
    {
        for (int i = 0; i < MaxIterations; ++i)
        {
            bool status = constraints.All(c => c.IsSatisfied(Epsilon));
            if (status)
            {
                return true;
            }
            else
            {
                foreach (var constraint in constraints)
                {
                    constraint.Project(pinned);
                }
            }
        }
        return constraints.All(c => c.IsSatisfied(Epsilon));
    }
}
