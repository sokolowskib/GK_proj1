using System.Numerics;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using Raylib_cs;

namespace PaintPP.App.Input;

public class IdleState : IInteractionState
{
    private const float HitRadius = 8f;
    public IInteractionState Update(Scene scene, Vector2 mouse, SceneEditor editor)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return this;

        var pointHit = scene.HitTestPoint(mouse, HitRadius);
        if (pointHit != null)
            return new DraggingPointState(pointHit.Point, pointHit.Polygon);

        var edgeHit = scene.HitTestEdge(mouse, HitRadius);
        if (edgeHit != null)
            return new DraggingPolygonState(edgeHit.Polygon, mouse);

        return this;
    }
}
