using CustomizePlus.Armatures.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using CustomizePlus.UI.Windows.Controls;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public partial class BoneEditorPanel
{
    public void Draw()
    {
        _isUnlocked = IsCharacterFound && IsEditorActive && !IsEditorPaused;

        DrawEditorConfirmationPopup();

        Im.Separator();

        using (var style = Im.Style.Push(ImStyleDouble.ButtonTextAlign, new Vector2(0, 0.5f)))
        {
            if (!DrawEditorHeader())
                return;

            if (!DrawBoneTable())
                return;
        }

        FlushPendingClipboard();
    }

    private void DrawEditorConfirmationPopup()
    {
        ReadOnlySpan<byte> popupName = "Unsaved Changes##SavePopup"u8;
        const WindowFlags popupFlags = WindowFlags.NoResize | WindowFlags.NoMove | WindowFlags.NoSavedSettings;

        if (_openSavePopup)
        {
            Im.Popup.Open(popupName);
            _openSavePopup = false;
        }

        var viewportSize = Im.Window.Viewport.Size;
        var scale = ImGuiHelpers.GlobalScale;
        var style = Im.Style;
        var popupWidth = MathF.Min(
            660 * scale,
            viewportSize.X * 0.95f);
        var buttonWidth = MathF.Min(
            150 * scale,
            (popupWidth - (2 * style.WindowPadding.X) - (3 * style.ItemSpacing.X)) / 4);
        var buttonSize = new Vector2(buttonWidth, 0);
        var totalButtonsWidth = (4 * buttonWidth) + (3 * style.ItemSpacing.X);

        Im.Window.SetNextSize(new Vector2(popupWidth, 0), Condition.Always);
        Im.Window.SetNextPosition(viewportSize / 2, Condition.Always, new Vector2(0.5f));
        using var popup = Im.Popup.BeginModal(popupName, popupFlags);
        if (!popup)
            return;

        Im.Cursor.Y = Im.Cursor.Y + style.ItemSpacing.Y;
        Im.TextWrapped("You have unsaved changes in current template, what would you like to do?"u8);
        Im.Line.Spacing();
        Im.Separator();
        Im.Line.Spacing();

        var exitedEditor = false;
        Im.Cursor.X = (Im.Window.Width - totalButtonsWidth) / 2;

        if (Im.Button("Save"u8, buttonSize))
        {
            _editorManager.SaveChangesAndDisableEditor();
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Save as a copy"u8, buttonSize))
        {
            _editorManager.SaveChangesAndDisableEditor(true);
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Do not save"u8, buttonSize))
        {
            _editorManager.DisableEditor();
            exitedEditor = true;
            Im.Popup.CloseCurrent();
        }

        Im.Line.Same();
        if (Im.Button("Keep editing"u8, buttonSize))
        {
            Im.Popup.CloseCurrent();
        }

        if (exitedEditor)
            _openSavePopup = false;
    }
}
