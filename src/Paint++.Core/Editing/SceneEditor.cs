using PaintPP.Core.Model;
namespace PaintPP.Core.Editing;

public class SceneEditor(Scene scene)
{
    public Vertex InsertVertexAtEdgeMidpoint(Polygon polygon, Edge edge)
    {
        var mid = edge.Midpoint;

        return polygon.InsertVertex(edge, mid);
    }
    public bool DeleteVertex(Polygon polygon, Vertex vertex)
    {
        if (polygon.Vertices.Count <= 3)
            return false;
        polygon.RemoveVertex(vertex);
        return true;
    }
}
