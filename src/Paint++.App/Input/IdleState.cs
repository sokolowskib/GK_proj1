using System.Numerics;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using Raylib_cs;

namespace PaintPP.App.Input;

public class IdleState : IInteractionState
{
    private const float HitRadius = 8f;
    public IInteractionState Update(EditorContext ctx, Vector2 mouse)
    {
        if (!Raylib.IsMouseButtonPressed(MouseButton.Left))
            return this;

        var pointHit = ctx.Scene.HitTestPoint(mouse, HitRadius);
        if (pointHit != null)
            return new DraggingPointState(pointHit.Point, pointHit.Polygon);

        var edgeHit = ctx.Scene.HitTestEdge(mouse, HitRadius);
        if (edgeHit != null)
        {
            if (Raylib.IsKeyDown(KeyboardKey.LeftSuper) || Raylib.IsKeyDown(KeyboardKey.RightSuper))
            {
                ctx.Selection.Toggle(edgeHit.Edge);
            }
            else
            {
                ctx.Selection.Select(edgeHit.Edge);
            }
            return new DraggingPolygonState(edgeHit.Polygon, mouse);
        }

        ctx.Selection.Clear();
        return this;
    }
}
