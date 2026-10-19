using System.Numerics;
using Raylib_cs;
namespace PaintPP.App.Rendering;

public class LibraryLineRasterizer : ILineRasterizer
{
    public void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        Raylib.DrawLineV(a, b, color);
    }
}
