using System.Numerics;
using Raylib_cs;
namespace PaintPP.App.Rendering;

public interface ILineRasterizer
{
    public void DrawLine(Vector2 a, Vector2 b, Color color);
}
