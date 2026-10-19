using System.Numerics;
using PaintPP.Core.Model;
using Raylib_cs;

namespace PaintPP.App.Input;

public class DraggingPointState(ScenePoint point) : IInteractionState
{
    public IInteractionState Update(Scene scene, Vector2 mouse)
    {
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            return new IdleState();
        point.Position = mouse;
        return this;
    }
}
