using System.Numerics;
using ImGuiNET;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;
using PaintPP.Core.Constraints;

namespace PaintPP.App.UI;

public class ContextMenu(EditorContext ctx)
{
    private const float Tolerance = 8f;
    private const string VertexMenu = "vertex_menu";
    private const string EdgeMenu = "edge_menu";
    private const string LengthPopup = "length_popup";
    private bool _openLengthPopup;
    private float _lengthInput;
    private bool _lengthRejected;
    private PointHit? point;
    private EdgeHit? edge;
    public void Open(Scene scene, Vector2 mouse)
    {
        var p = scene.HitTestPoint(mouse, Tolerance);
        if (p != null && p.Point is Vertex)
        {
            point = p;
            edge = null;
            ImGui.OpenPopup(VertexMenu);
            return;
        }
        var e = scene.HitTestEdge(mouse, Tolerance);
        if (e != null)
        {
            point = null;
            edge = e;
            ImGui.OpenPopup(EdgeMenu);
            return;
        }
        point = null;
        edge = null;
    }
    public void Draw()
    {
        if (ImGui.BeginPopup(VertexMenu))
        {
            if (point?.Point is Vertex vertex)
            {
                bool canDelete = point.Polygon.Vertices.Count > 3;
                if (ImGui.MenuItem("Usuń wierzchołek", null, false, canDelete))
                    ctx.Editor.DeleteVertex(point.Polygon, vertex);
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopup(EdgeMenu))
        {
            if (edge != null)
            {
                bool isBezier = edge.Edge.Shape is BezierShape;
                if (ImGui.MenuItem("Dodaj wierzchołek na środku"))
                {
                    ctx.Editor.InsertVertexAtEdgeMidpoint(edge.Polygon, edge.Edge);
                }
                if (isBezier)
                {
                    if (ImGui.MenuItem("Zmień na odcinek"))
                    {
                        ctx.Editor.ConvertToLine(edge.Edge);
                    }
                }
                else
                {
                    if (ImGui.MenuItem("Zmień na krzywą Beziera"))
                    {
                        ctx.Editor.ConvertToBezier(edge.Edge);
                    }
                }
                if (ImGui.MenuItem("Ustaw długość", null, false, !isBezier))
                {
                    _lengthInput = edge.Edge.Length;
                    _lengthRejected = false;
                    _openLengthPopup = true;
                }
                bool canPerpendicular = ctx.Selection.Edges.Count == 1 && ctx.Selection.Edges[0] != edge.Edge &&
                                        edge.Edge.Shape is not BezierShape && ctx.Selection.Edges[0].Shape is LineShape;
                if (ImGui.MenuItem("Prostopadła do zaznaczonej", null, false, canPerpendicular))
                {
                    if (!ctx.Editor.AddPerpendicular(ctx.Selection.Edges[0], edge.Edge))
                        ctx.Notification.Show("Nie można dodać prostopadłości");
                }
                var constraints = ctx.Scene.Constraints.Where(c => c.Edges.Contains(edge.Edge)).ToList();
                if (constraints.Count > 0)
                {
                    ImGui.Separator();
                }
                var perpendiculars = ctx.Scene.Constraints.OfType<PerpendicularConstraint>().ToList();

                string text = "";

                for (int i = 0; i < constraints.Count; ++i)
                {
                    switch (constraints[i])
                    {
                        case LengthConstraint l:
                            text = $"Usuń: długość {l.Length:0}##{i}";
                            break;
                        case PerpendicularConstraint perp:
                            text = $"Usuń: prostopadłość x{perpendiculars.IndexOf(perp) + 1}##{i}";
                            break;
                        default:
                            continue;
                    }
                    if (ImGui.MenuItem(text))
                        ctx.Editor.RemoveConstraint(constraints[i]);
                }
            }
            ImGui.EndPopup();
        }
        if (_openLengthPopup)
        {
            ImGui.OpenPopup(LengthPopup);
            _openLengthPopup = false;
        }
        if (ImGui.BeginPopup(LengthPopup))
        {
            if (ImGui.InputFloat("Długość", ref _lengthInput))
                _lengthRejected = false;

            bool valid = _lengthInput > 0;
            if (!valid)
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "Długość musi być dodatnia");
            if (_lengthRejected)
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), "Sprzeczne z innymi ograniczeniami");

            ImGui.BeginDisabled(!valid);
            if (ImGui.Button("OK") && edge != null)
            {
                if (ctx.Editor.SetLength(edge.Edge, _lengthInput))
                    ImGui.CloseCurrentPopup();
                else
                    _lengthRejected = true;
            }
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
    }
}
