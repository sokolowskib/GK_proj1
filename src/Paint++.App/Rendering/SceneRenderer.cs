using Raylib_cs;
using PaintPP.Core.Model;

namespace PaintPP.App.Rendering;

public class SceneRenderer
{
    private const float VertexRadius = 5f;

    private static readonly Color EdgeColor = Color.DarkBlue;
    private static readonly Color VertexColor = Color.Red;
    public ILineRasterizer Rasterizer { get; set; } = new LibraryLineRasterizer();

    public void Draw(Scene scene)
    {
        foreach (var poly in scene.Polygons)
        {
            DrawPolygon(poly);
        }
    }

    public void DrawPolygon(Polygon polygon)
    {
        foreach (var edge in polygon.Edges)
        {
            Rasterizer.DrawLine(edge.Start.Position, edge.End.Position, EdgeColor);
        }
        foreach (var v in polygon.Vertices)
        {
            Raylib.DrawCircleV(v.Position, VertexRadius, VertexColor);
        }
    }
}
