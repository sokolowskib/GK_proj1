using System.Numerics;
using ImGuiNET;
using PaintPP.Core.Model;
using PaintPP.Core.Editing;

namespace PaintPP.App.UI;

public class ContextMenu(SceneEditor editor)
{
    private const float Tolerance = 8f;
    private const string VertexMenu = "vertex_menu";
    private const string EdgeMenu = "edge_menu";
    private PointHit? point;
    private EdgeHit? edge;
    public void Open(Scene scene, Vector2 mouse)
    {
        var p = scene.HitTestPoint(mouse, Tolerance);
        if (p != null)
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
                    editor.DeleteVertex(point.Polygon, vertex);
            }
            ImGui.EndPopup();
        }
        if (ImGui.BeginPopup(EdgeMenu))
        {
            if (edge != null && ImGui.MenuItem("Dodaj wierzchołek na środku"))
                editor.InsertVertexAtEdgeMidpoint(edge.Polygon, edge.Edge);
            ImGui.EndPopup();
        }
    }
}
