using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using PaintPP.App;
using PaintPP.App.Rendering;
using PaintPP.App.Input;
using PaintPP.Core.Editing;
using PaintPP.App.UI;

const int PanelWidth = 300;

Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint | ConfigFlags.VSyncHint);
Raylib.InitWindow(1280, 800, "Paint++");
rlImGui.BeginInitImGui();
ImGui.StyleColorsDark();
LoadFont("JetBrainsMono-Regular.ttf", 16f);
rlImGui.EndInitImGui();

var scene = DemoScene.Create();

var renderer = new SceneRenderer();
IInteractionState state = new IdleState();
var editor = new SceneEditor(scene);
var selection = new Selection();
var notification = new Notification();
var ctx = new EditorContext(scene, editor, selection, notification);
var contextMenu = new ContextMenu(ctx);
var helpWindow = new HelpWindow();

while (!Raylib.WindowShouldClose())
{
    Vector2 mouse = Raylib.GetMousePosition();
    bool mouseOnCanvas = !ImGui.GetIO().WantCaptureMouse;
    if (mouseOnCanvas)
        state = state.Update(ctx, mouse);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.RayWhite);

    selection.RemoveMissing(scene);
    renderer.Draw(scene, selection);
    Raylib.DrawCircleV(Raylib.GetMousePosition(), 5, Color.Red);

    rlImGui.Begin();
    if (mouseOnCanvas && Raylib.IsMouseButtonPressed(MouseButton.Right))
        contextMenu.Open(scene, mouse);
    contextMenu.Draw();

    DrawSidePanel(renderer, ctx, helpWindow);
    helpWindow.Draw();
    rlImGui.End();

    Raylib.EndDrawing();
}

renderer.Unload();
rlImGui.Shutdown();
Raylib.CloseWindow();

static void DrawSidePanel(SceneRenderer renderer, EditorContext ctx, HelpWindow helpWindow)
{
    int screenWidth = Raylib.GetScreenWidth();
    int screenHeight = Raylib.GetScreenHeight();

    ImGui.SetNextWindowPos(new Vector2(screenWidth - PanelWidth, 0));
    ImGui.SetNextWindowSize(new Vector2(PanelWidth, screenHeight));
    ImGui.Begin("Paint++", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

    Vector2 mouse = Raylib.GetMousePosition();
    ImGui.Text($"FPS: {Raylib.GetFPS()}");
    ImGui.Text($"Mysz: ({mouse.X:0}, {mouse.Y:0})");
    if (ImGui.Button("Pomoc (F1)"))
        helpWindow.Toggle();

    ImGui.SeparatorText("Rysowanie odcinków");
    bool isLibrary = renderer.Rasterizer is LibraryLineRasterizer;
    if (ImGui.RadioButton("Biblioteczny", isLibrary))
        renderer.Rasterizer = new LibraryLineRasterizer();
    bool isBresenham = renderer.Rasterizer is BresenhamLineRasterizer;
    if (ImGui.RadioButton("Bresenham", isBresenham))
        renderer.Rasterizer = new BresenhamLineRasterizer();

    ImGui.Text($"Zaznaczone: {ctx.Selection.Edges.Count}");
    ctx.Notification.Draw();

    ImGui.End();
}

// Domyślna czcionka ImGui ma tylko ASCII — ładujemy TTF z zakresem obejmującym polskie litery (Latin Extended-A),
// greckie litery (ε w dokumentacji) i interpunkcję typograficzną (półpauza, cudzysłowy „”).
static void LoadFont(string fileName, float size)
{
    ushort[] glyphRanges = [0x0020, 0x017F, 0x0370, 0x03FF, 0x2000, 0x206F, 0];
    // ImGui (C++) trzyma wskaźnik do tej tablicy, więc przypinamy ją na cały czas działania programu.
    var handle = GCHandle.Alloc(glyphRanges, GCHandleType.Pinned);
    string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", fileName);
    ImGui.GetIO().Fonts.AddFontFromFileTTF(path, size, null, handle.AddrOfPinnedObject());
}
