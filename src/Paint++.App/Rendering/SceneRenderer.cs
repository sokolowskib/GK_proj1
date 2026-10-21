using Raylib_cs;
using PaintPP.Core.Model;
using PaintPP.Core.Constraints;
using PaintPP.App.UI;
using System.Numerics;
using ImGuiNET;
namespace PaintPP.App.Rendering;

public class SceneRenderer
{
    private const float VertexRadius = 5f;
    private const float LabelFontSize = 16f;
    private const float LabelGap = 6f;

    private static readonly Color EdgeColor = Color.DarkBlue;
    private static readonly Color SelectedEdgeColor = Color.Orange;
    private static readonly Color VertexColor = Color.Red;
    public ILineRasterizer Rasterizer { get; set; } = new LibraryLineRasterizer();

    private readonly Font _font;

    public SceneRenderer()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "JetBrainsMono-Regular.ttf");
        _font = Raylib.LoadFontEx(path, 32, null, 0);
        Raylib.SetTextureFilter(_font.Texture, TextureFilter.Bilinear);
    }

    public void Unload() => Raylib.UnloadFont(_font);

    public void Draw(Scene scene, Selection selection)
    {
        foreach (var poly in scene.Polygons)
        {
            DrawPolygon(poly, selection);
        }
        DrawConstraintsIcon(scene);
    }

    public void DrawPolygon(Polygon polygon, Selection selection)
    {
        foreach (var edge in polygon.Edges)
        {
            if (selection.Edges.Contains(edge))
            {
                Rasterizer.DrawLine(edge.Start.Position, edge.End.Position, SelectedEdgeColor);
            }
            else
            {
                Rasterizer.DrawLine(edge.Start.Position, edge.End.Position, EdgeColor);
            }
        }
        foreach (var v in polygon.Vertices)
        {
            Raylib.DrawCircleV(v.Position, VertexRadius, VertexColor);
        }
    }
    public void DrawConstraintsIcon(Scene scene)
    {
        var perpendiculars = scene.Constraints.OfType<PerpendicularConstraint>().ToList();
        foreach (var p in scene.Polygons)
        {
            foreach (var e in p.Edges)
            {
                if (e.Length < 1e-5)
                    continue;
                var dir = Vector2.Normalize(e.End.Position - e.Start.Position);
                var normal = new Vector2(-dir.Y, dir.X);

                var labels = new List<(string Text, Color Color, Vector2 Size)>();
                foreach (var constraint in scene.Constraints.Where(c => c.Edges.Contains(e)))
                {
                    string text;
                    Color color;
                    switch (constraint)
                    {
                        case LengthConstraint l:
                            text = $"{l.Length:0}";
                            color = Color.Red;
                            break;
                        case PerpendicularConstraint perp:
                            text = $"x{perpendiculars.IndexOf(perp) + 1}";
                            color = Color.Purple;
                            break;
                        default:
                            continue;
                    }
                    labels.Add((text, color, Raylib.MeasureTextEx(_font, text, LabelFontSize, 1f)));
                }
                if (labels.Count == 0)
                    continue;

                // Ile napis (prostokąt w × h) wystaje w danym kierunku od swojego środka — zależy od kąta krawędzi.
                static float HalfExtent(Vector2 size, Vector2 direction) =>
                    (MathF.Abs(direction.X) * size.X + MathF.Abs(direction.Y) * size.Y) / 2f;

                // Napisy obok siebie wzdłuż krawędzi, cała grupa wyśrodkowana na Midpoint.
                float total = labels.Sum(l => 2f * HalfExtent(l.Size, dir)) + LabelGap * (labels.Count - 1);
                float along = -total / 2f;
                foreach (var (text, color, size) in labels)
                {
                    float halfAlong = HalfExtent(size, dir);
                    float halfAcross = HalfExtent(size, normal);
                    // środek napisu: odsunięty od linii o połowę napisu + odstęp, więc brzeg zawsze LabelGap od krawędzi
                    var center = e.Midpoint + normal * (halfAcross + LabelGap) + dir * (along + halfAlong);
                    Raylib.DrawTextEx(_font, text, center - size / 2f, LabelFontSize, 1f, color);
                    along += 2f * halfAlong + LabelGap;
                }
            }
        }
    }
}
