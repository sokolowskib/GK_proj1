using System.Numerics;
using PaintPP.Core.Model;
using Raylib_cs;

namespace PaintPP.App.Input;

public class IdleState : IInteractionState
{
    private const float HitRadius = 8f;
    public IInteractionState Update(Scene scene, Vector2 mouse)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return this;

        var pointHit = scene.HitTestPoint(mouse, HitRadius);
        if (pointHit != null)
            return new DraggingPointState(pointHit.Point);

        var edgeHit = scene.HitTestEdge(mouse, HitRadius);
        if (edgeHit != null)
            return new DraggingPolygonState(edgeHit.Polygon, mouse);

        return this;
    }
}
