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
    private const string LengthPopup = "length_popup";
    private bool _openLengthPopup;
    private float _lengthInput;
    private bool _lengthRejected;
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
            if (edge != null)
            {
                if (ImGui.MenuItem("Dodaj wierzchołek na środku"))
                {
                    editor.InsertVertexAtEdgeMidpoint(edge.Polygon, edge.Edge);
                }
                if (ImGui.MenuItem("Ustaw długość"))
                {
                    _lengthInput = edge.Edge.Length;
                    _lengthRejected = false;
                    _openLengthPopup = true;
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
                if (editor.SetLength(edge.Edge, _lengthInput))
                    ImGui.CloseCurrentPopup();
                else
                    _lengthRejected = true;
            }
            ImGui.EndDisabled();
            ImGui.EndPopup();
        }
    }
}
