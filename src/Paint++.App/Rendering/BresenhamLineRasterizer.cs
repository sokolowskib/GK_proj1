using System.Numerics;
using Raylib_cs;
using PaintPP.Core.Rasterization;
namespace PaintPP.App.Rendering;

public class BresenhamLineRasterizer : ILineRasterizer
{
    public void DrawLine(Vector2 a, Vector2 b, Color color)
    {
        int ax = (int)MathF.Round(a.X);
        int ay = (int)MathF.Round(a.Y);
        int bx = (int)MathF.Round(b.X);
        int by = (int)MathF.Round(b.Y);
        foreach ((int x, int y) in Bresenham.Line(ax, ay, bx, by))
        {
            Raylib.DrawPixel(x, y, color);
        }
    }
}
