using System.Numerics;

namespace PaintPP.Core.Model;

public class Vertex(Vector2 position) : ScenePoint
(position)
{
    public Continuity Continuity { get; set; } = Continuity.G0;
    public bool ContinuityExplicit { get; set; }
};
