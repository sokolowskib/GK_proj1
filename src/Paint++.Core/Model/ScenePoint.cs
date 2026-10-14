using System.Numerics;

namespace PaintPP.Core.Model;

/// <summary>
/// Jakikolwiek przeciągalny punkt na obrazie.
/// </summary>

public class ScenePoint(Vector2 position)
{
    public Vector2 Position { get; set; } = position;
}
