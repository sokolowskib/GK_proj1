using System.Numerics;
namespace PaintPP.Core.Geometry;

public static class Geometry2D
{
    ///< summary>
    /// Funkcja zwraca odległość punktu point od odcinka ab. Jeśli długość odcinka jest pomijalnie mała, zwracana jest
    /// odległość punktu point do punktu a. Liczony jest iloczyn skalarny miedzy wektorami point - a i b - a oraz
    /// normalizowany, co wyznacza przeciecie  prostej prostopadlej do ab przechodzacej przez point. Jeśli prosta ta nie
    /// przechodzi przez odcinek ab, zwracana jest zclampowana wartosc do 0 lub 1. Jesli nie, liczona jest odleglosc
    /// punktu point to punktu x, wyznaczonego przez przeciecie prostej prostopadlej do wektora ab.
    ///< summary>
    public static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;

        float length_squared = ab.LengthSquared();
        if (length_squared < 1e-12f)
        {
            return Vector2.Distance(point, a);
        }
        float t = Vector2.Dot(point - a, ab) / length_squared;

        t = Math.Clamp(t, 0f, 1f);
        Vector2 x = a + t * ab;
        return Vector2.Distance(point, x);
    }
}
