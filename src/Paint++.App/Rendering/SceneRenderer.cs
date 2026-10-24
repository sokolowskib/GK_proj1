using Raylib_cs;
using PaintPP.Core.Model;
using PaintPP.Core.Constraints;
using PaintPP.App.UI;
using System.Numerics;
using ImGuiNET;
using PaintPP.Core.Rasterization;
using System.Reflection.Emit;
namespace PaintPP.App.Rendering;

public class SceneRenderer
{
    private const float VertexRadius = 5f;
    private const float LabelFontSize = 16f;
    private const float LabelGap = 6f;
    private const int Dash = 6;
    private const int Gap = 4;

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
            var color = selection.Edges.Contains(edge) ? SelectedEdgeColor : EdgeColor;
            switch (edge.Shape)
            {
                case LineShape:
                    Rasterizer.DrawLine(edge.Start.Position, edge.End.Position, color);
                    break;
                case BezierShape bezier:
                    DrawBezier(edge, bezier, color);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }
        foreach (var v in polygon.Vertices)
        {
            Raylib.DrawCircleV(v.Position, VertexRadius, VertexColor);
            var nearBezier =
                polygon.IncomingEdge(v).Shape is BezierShape || polygon.OutgoingEdge(v).Shape is BezierShape;
            if (v.Continuity != Continuity.G0)
            {
                Raylib.DrawTextEx(_font, v.Continuity.ToString(), v.Position + new Vector2(8, -20), LabelFontSize, 1f,
                                  Color.DarkGreen);
            }
        }
    }
    public void DrawBezier(Edge edge, BezierShape bezier, Color color)
    {
        var p0 = edge.Start.Position;
        var p1 = bezier.C1.Position;
        var p2 = bezier.C2.Position;
        var p3 = edge.End.Position;

        float len = Vector2.Distance(p0, p1) + Vector2.Distance(p1, p2) + Vector2.Distance(p2, p3);
        int steps = Math.Max(8, (int)(len / 5));

        DrawDashedLine(p0, p1, Color.Gray);
        DrawDashedLine(p1, p2, Color.Gray);
        DrawDashedLine(p2, p3, Color.Gray);

        var sample = BezierCurve.Sample(p0, p1, p2, p3, steps).ToList();

        for (int i = 0; i < sample.Count - 1; ++i)
        {
            Raylib.DrawLineV(sample[i], sample[i + 1], color);
        }
        Raylib.DrawCircleV(p1, VertexRadius, Color.DarkGreen);
        Raylib.DrawCircleV(p2, VertexRadius, Color.DarkGreen);
    }
    public static void DrawDashedLine(Vector2 a, Vector2 b, Color color)
    {
        var length = Vector2.Distance(a, b);

        if (length < 1e-5)
            return;

        var dir = (b - a) / length;
        for (int s = 0; s < length; s += Dash + Gap)
        {
            var from = a + dir * s;
            var to = a + dir * Math.Min(s + Dash, length);
            Raylib.DrawLineV(from, to, color);
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
                    // środek napisu: odsunięty od linii o połowę napisu + odstęp, więc brzeg zawsze LabelGap od
                    // krawędzi
                    var center = e.Midpoint + normal * (halfAcross + LabelGap) + dir * (along + halfAlong);
                    Raylib.DrawTextEx(_font, text, center - size / 2f, LabelFontSize, 1f, color);
                    along += 2f * halfAlong + LabelGap;
                }
            }
        }
    }
}
