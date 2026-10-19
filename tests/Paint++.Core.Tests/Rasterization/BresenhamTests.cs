using PaintPP.Core.Rasterization;

namespace PaintPP.Core.Tests.Rasterization;

public class BresenhamTests
{
    // --- Konkretne przykłady rozpisane ręcznie (tabelki z rozmowy) ---

    [Fact]
    public void GentleLine_MatchesHandTrace()
    {
        var pixels = Bresenham.Line(0, 0, 8, 3).ToList();

        Assert.Equal(
            [(0, 0), (1, 0), (2, 1), (3, 1), (4, 1), (5, 2), (6, 2), (7, 3), (8, 3)],
            pixels);
    }

    [Fact]
    public void GentleLine_5x2_MatchesHandTrace()
    {
        var pixels = Bresenham.Line(0, 0, 5, 2).ToList();

        Assert.Equal([(0, 0), (1, 0), (2, 1), (3, 1), (4, 2), (5, 2)], pixels);
    }

    [Fact]
    public void SteepLine_SwapsRolesOfXAndY()
    {
        var pixels = Bresenham.Line(0, 0, 3, 8).ToList();

        Assert.Equal(
            [(0, 0), (0, 1), (1, 2), (1, 3), (1, 4), (2, 5), (2, 6), (3, 7), (3, 8)],
            pixels);
    }

    // --- Przypadki szczególne ---

    [Fact]
    public void SinglePoint_ReturnsThatPixel()
    {
        var pixels = Bresenham.Line(4, 7, 4, 7).ToList();

        Assert.Equal([(4, 7)], pixels);
    }

    [Theory]
    [InlineData(2, 5, 6, 5)]   // poziomo w prawo
    [InlineData(6, 5, 2, 5)]   // poziomo w lewo
    public void HorizontalLine_KeepsYConstant(int x0, int y0, int x1, int y1)
    {
        var pixels = Bresenham.Line(x0, y0, x1, y1).ToList();

        Assert.Equal(5, pixels.Count);
        Assert.All(pixels, p => Assert.Equal(5, p.Y));
    }

    [Theory]
    [InlineData(3, 1, 3, 6)]   // pionowo w dół ekranu
    [InlineData(3, 6, 3, 1)]   // pionowo w górę ekranu
    public void VerticalLine_KeepsXConstant(int x0, int y0, int x1, int y1)
    {
        var pixels = Bresenham.Line(x0, y0, x1, y1).ToList();

        Assert.Equal(6, pixels.Count);
        Assert.All(pixels, p => Assert.Equal(3, p.X));
    }

    [Fact]
    public void DiagonalLine_StepsBothAxesEveryTime()
    {
        var pixels = Bresenham.Line(0, 0, 4, -4).ToList();

        Assert.Equal([(0, 0), (1, -1), (2, -2), (3, -3), (4, -4)], pixels);
    }

    // --- Wszystkie 8 oktantów: własności, które musi spełniać każda poprawna linia ---
    // Start celowo nie w (0,0), żeby złapać pomylenie współrzędnych względnych z bezwzględnymi.

    public static TheoryData<int, int> Octants => new()
    {
        { 8, 3 },   // prawo, płasko, w dół ekranu
        { 3, 8 },   // prawo, stromo, w dół
        { -3, 8 },  // lewo, stromo, w dół
        { -8, 3 },  // lewo, płasko, w dół
        { -8, -3 }, // lewo, płasko, w górę
        { -3, -8 }, // lewo, stromo, w górę
        { 3, -8 },  // prawo, stromo, w górę
        { 8, -3 },  // prawo, płasko, w górę
    };

    private const int StartX = 10;
    private const int StartY = 20;

    [Theory]
    [MemberData(nameof(Octants))]
    public void AnyOctant_StartsAndEndsAtEndpoints(int dx, int dy)
    {
        var pixels = Bresenham.Line(StartX, StartY, StartX + dx, StartY + dy).ToList();

        Assert.Equal((StartX, StartY), pixels[0]);
        Assert.Equal((StartX + dx, StartY + dy), pixels[^1]);
    }

    [Theory]
    [MemberData(nameof(Octants))]
    public void AnyOctant_HasOnePixelPerStepOfLongerAxis(int dx, int dy)
    {
        var pixels = Bresenham.Line(StartX, StartY, StartX + dx, StartY + dy).ToList();

        int expected = Math.Max(Math.Abs(dx), Math.Abs(dy)) + 1;
        Assert.Equal(expected, pixels.Count);
    }

    [Theory]
    [MemberData(nameof(Octants))]
    public void AnyOctant_ConsecutivePixelsTouch(int dx, int dy)
    {
        var pixels = Bresenham.Line(StartX, StartY, StartX + dx, StartY + dy).ToList();

        for (int i = 1; i < pixels.Count; i++)
        {
            int stepX = Math.Abs(pixels[i].X - pixels[i - 1].X);
            int stepY = Math.Abs(pixels[i].Y - pixels[i - 1].Y);
            Assert.True(stepX <= 1 && stepY <= 1, $"Dziura między {pixels[i - 1]} a {pixels[i]}");
            Assert.True(stepX + stepY > 0, $"Powtórzony piksel {pixels[i]}");
        }
    }

    [Theory]
    [MemberData(nameof(Octants))]
    public void AnyOctant_StaysWithinHalfPixelOfTrueLine(int dx, int dy)
    {
        var pixels = Bresenham.Line(StartX, StartY, StartX + dx, StartY + dy).ToList();

        bool xMajor = Math.Abs(dx) >= Math.Abs(dy);
        foreach (var (x, y) in pixels)
        {
            // Błąd mierzony wzdłuż krótszej osi: dla danego x (lub y) gdzie leży prawdziwa prosta.
            double error = xMajor
                ? Math.Abs(y - (StartY + (double)(x - StartX) * dy / dx))
                : Math.Abs(x - (StartX + (double)(y - StartY) * dx / dy));
            Assert.True(error <= 0.5 + 1e-9, $"Piksel ({x}, {y}) za daleko od prostej (błąd {error:0.###})");
        }
    }
}
