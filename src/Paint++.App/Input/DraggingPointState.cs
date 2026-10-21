using System.Numerics;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using Raylib_cs;

namespace PaintPP.App.Input;

public class DraggingPointState(ScenePoint point, Polygon polygon) : IInteractionState
{
    public IInteractionState Update(EditorContext ctx, Vector2 mouse)
    {
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            return new IdleState();
        if (!ctx.Editor.MovePoint(polygon, point, mouse))
        {
            ctx.Notification.Show("Ograniczenia sprzeczne - przesuwam wielokąt");
        }

        return this;
    }
}
