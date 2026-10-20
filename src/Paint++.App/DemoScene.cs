using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.App;

public static class DemoScene
{
    public static Scene Create()
    {
        var builder = new SceneBuilder();

        var p1 = builder.Polygon((100, 100), (350, 120), (300, 350), (120, 300));

        var p2 = builder.Polygon((450, 100), (700, 300), (700, 100), (450, 300));

        var p3 = builder.Polygon((200, 450), (400, 420), (450, 600), (300, 700), (150, 620));

        builder.Length(p1.Edges[0], 100);
        builder.Length(p2.Edges[0], 50);
        builder.Length(p3.Edges[0], 150);

        return builder.Build();
    }
}
