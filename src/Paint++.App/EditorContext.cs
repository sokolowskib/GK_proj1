using PaintPP.App.UI;
using PaintPP.Core.Editing;
using PaintPP.Core.Model;

public record EditorContext(Scene Scene, SceneEditor Editor, Selection Selection, Notification Notification);
