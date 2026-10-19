
namespace PaintPP.Core.Rasterization;

public static class Bresenham
{
    public static IEnumerable<(int X, int Y)> Line(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);

        int sx = (x1 >= x0) ? +1 : -1;
        int sy = (y1 >= y0) ? +1 : -1;

        bool steep = dy > dx;
        int long_axis = steep ? dy : dx;
        int short_axis = steep ? dx : dy;

        int D = 2 * short_axis - long_axis;
        int x = x0;
        int y = y0;

        for (int i = 0; i <= long_axis; i++)
        {
            yield return (x, y);
            if (D > 0)
            {
                if (steep)
                    x += sx;
                else
                    y += sy;
                D -= 2 * long_axis;
            }
            D += 2 * short_axis;
            if (steep)
                y += sy;
            else
                x += sx;
        }
    }
}
