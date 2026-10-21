using System.Numerics;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

namespace PaintPP.App.Input;
public interface IInteractionState
{
    IInteractionState Update(EditorContext ctx, Vector2 mouse);
}
