using System.Numerics;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using Raylib_cs;

namespace PaintPP.App.Input;

public class DraggingPointState(ScenePoint point, Polygon polygon) : IInteractionState
{
    public IInteractionState Update(Scene scene, Vector2 mouse, SceneEditor editor)
    {
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            return new IdleState();
        editor.MovePoint(polygon, point, mouse);

        return this;
    }
}
