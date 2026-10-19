using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.App.Input;
public interface IInteractionState
{
    IInteractionState Update(Scene scene, Vector2 mouse);
}
