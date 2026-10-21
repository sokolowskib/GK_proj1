using PaintPP.Core.Model;
using Raylib_cs;
using ImGuiNET;
using System.Numerics;
namespace PaintPP.App.UI;

public class Notification
{
    private string _text = "";
    private double _until;
    public void Show(string text, double seconds = 2.0)
    {
        _until = Raylib.GetTime() + seconds;
        _text = text;
    }
    public void Draw()
    {
        if (Raylib.GetTime() < _until)
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), _text);
        }
    }
}
