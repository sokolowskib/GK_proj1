using PaintPP.Core.Model;
namespace PaintPP.Core.Constraints;

public interface IConstraint
{
    IReadOnlyList<Edge> Edges { get; }
    bool IsSatisfied(float epsilon);
    void Project(IReadOnlySet<ScenePoint> pinned);
}
