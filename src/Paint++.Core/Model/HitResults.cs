namespace PaintPP.Core.Model;

/// <summary>
/// wynik trafienia na ekranie w punkt
/// </summary>
public record PointHit(Polygon Polygon, ScenePoint Point);
/// <summary>
/// wynik trafienia na ekranie w krawedz
/// </summary>
public record EdgeHit(Polygon Polygon, Edge Edge);
