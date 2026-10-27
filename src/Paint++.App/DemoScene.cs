using PaintPP.Core.Model;

namespace PaintPP.App;

public static class DemoScene
{
    public static Scene Create()
    {
        var builder = new SceneBuilder();

        // Wielokąt z przykładu w treści zadania: dwie krzywe Béziera, dwa łuki, G1 i C1.
        var shape = builder.Polygon((250, 395), (342, 402), (406, 530), (775, 514), (775, 697), (735, 662),
                                    (644, 710), (506, 642), (377, 690), (124, 479));
        builder.Bezier(shape.Edges[2], (426, 572), (775, 454));
        builder.Bezier(shape.Edges[8], (332, 706), (82, 505));
        builder.SetContinuity(shape.Vertices[2], Continuity.G1);
        builder.SetContinuity(shape.Vertices[3], Continuity.G1);
        builder.SetContinuity(shape.Vertices[8], Continuity.G1);
        builder.Arc(shape.Edges[5]);
        builder.Arc(shape.Edges[6]);
        builder.SetContinuity(shape.Vertices[5], Continuity.G1);
        builder.Length(shape.Edges[0], 100);
        builder.Length(shape.Edges[3], 180);

        // Prostopadłość między wielokątami i wewnątrz jednego.
        var quad = builder.Polygon((80, 80), (330, 80), (330, 250), (80, 250));
        builder.Perpendicular(quad.Edges[2], shape.Edges[3]);
        builder.Perpendicular(quad.Edges[1], quad.Edges[0]);  // żadna krawędź nie ma dwóch prostopadłości
        builder.Length(quad.Edges[1], 170);

        // Samoprzecięcie.
        builder.Polygon((450, 80), (700, 280), (700, 80), (450, 280));

        return builder.Build();
    }
}
