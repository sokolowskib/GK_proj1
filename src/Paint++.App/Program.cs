using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using PaintPP.App;
using PaintPP.App.Rendering;

const int PanelWidth = 300;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
Raylib.InitWindow(1280, 800, "Paint++");
rlImGui.Setup(darkTheme: true);

var scene = DemoScene.Create();

var renderer = new SceneRenderer();

while (!Raylib.WindowShouldClose())
{
    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);

    renderer.Draw(scene);
    Raylib.DrawCircleV(Raylib.GetMousePosition(), 5, Color.Red);

    rlImGui.Begin();
    DrawSidePanel();
    rlImGui.End();

    Raylib.EndDrawing();
}

rlImGui.Shutdown();
Raylib.CloseWindow();

static void DrawSidePanel()
{
    int screenWidth = Raylib.GetScreenWidth();
    int screenHeight = Raylib.GetScreenHeight();

    ImGui.SetNextWindowPos(new Vector2(screenWidth - PanelWidth, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, screenHeight));
    ImGui.Begin("Paint++", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

    Vector2 mouse = Raylib.GetMousePosition();
    ImGui.Text($"FPS: {Raylib.GetFPS()}");
    ImGui.Text($"Mysz: ({mouse.X:0}, {mouse.Y:0})");

    ImGui.End();
}
