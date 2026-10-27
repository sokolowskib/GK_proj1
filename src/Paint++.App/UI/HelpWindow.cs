using System.Numerics;
using System.Reflection;
using ImGuiNET;
using Raylib_cs;
namespace PaintPP.App.UI;

// Wyświetla README.md osadzony w pliku wykonywalnym (EmbeddedResource w .csproj).
public class HelpWindow
{
    private readonly string[] _lines = LoadReadme();
    private bool _open;

    public void Toggle() => _open = !_open;

    public void Draw()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
            Toggle();
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Pomoc", ref _open))
        {
            foreach (var line in _lines)
                DrawLine(line);
        }
        ImGui.End();
    }

    // Uproszczony Markdown: nagłówki jako separatory, wiersze tabel jako "a - b", reszta jako zawijany tekst.
    private static void DrawLine(string line)
    {
        if (line.StartsWith("```") || line.StartsWith("|---"))
            return;
        if (line.StartsWith('#'))
        {
            ImGui.SeparatorText(line.TrimStart('#', ' '));
            return;
        }
        if (line.StartsWith('|'))
        {
            var cells = line.Trim('|').Split('|').Select(c => c.Trim());
            ImGui.TextWrapped(string.Join(" - ", cells));
            return;
        }
        ImGui.TextWrapped(line.Replace("**", "").Replace("`", ""));
    }

    private static string[] LoadReadme()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("README.md");
        if (stream == null)
            return ["Brak dokumentacji (README.md nie został osadzony)."];
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split('\n');
    }
}
