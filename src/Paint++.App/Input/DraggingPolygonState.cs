using System.Numerics;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using Raylib_cs;

namespace PaintPP.App.Input;

public class DraggingPolygonState(Polygon polygon, Vector2 startMouse) : IInteractionState
{
    private Vector2 _lastMouse = startMouse;
    public IInteractionState Update(EditorContext ctx, Vector2 mouse)
    {
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            return new IdleState();

        var delta = mouse - _lastMouse;

        ctx.Editor.TranslatePolygon(polygon, delta);
        _lastMouse = mouse;
        return this;
    }
}
