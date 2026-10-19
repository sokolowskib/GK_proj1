using System.Numerics;
using PaintPP.Core.Model;
using Raylib_cs;

namespace PaintPP.App.Input;

public class DraggingPolygonState(Polygon polygon, Vector2 startMouse) : IInteractionState
{
    private Vector2 _lastMouse = startMouse;
    public IInteractionState Update(Scene scene, Vector2 mouse)
    {
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            return new IdleState();

        var delta = mouse - _lastMouse;

        polygon.Translate(delta);
        _lastMouse = mouse;
        return this;
    }
}
