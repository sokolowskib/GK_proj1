using System.Numerics;
using PaintPP.Core.Model;

namespace PaintPP.App;

public static class DemoScene
{
    public static Scene Create()
    {
        var builder = new SceneBuilder();

        builder.Polygon((100, 100), (350, 120), (300, 350), (120, 300));

        builder.Polygon((450, 100), (700, 300), (700, 100), (450, 300));

        builder.Polygon((200, 450), (400, 420), (450, 600), (300, 700), (150, 620));

        return builder.Build();
    }
}
