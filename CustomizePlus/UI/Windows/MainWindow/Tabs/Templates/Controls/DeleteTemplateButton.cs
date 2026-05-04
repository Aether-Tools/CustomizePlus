using CustomizePlus.Configuration.Data;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;

public sealed class DeleteTemplateButton(
    TemplateFileSystem fileSystem,
    TemplateManager templateManager,
    TemplateEditorManager editorManager,
    PopupSystem popupSystem, PluginConfiguration config)
    : BaseIconButton<AwesomeIcon>
{
    /// <inheritdoc/>
    public override AwesomeIcon Icon
        => LunaStyle.DeleteIcon;

    /// <inheritdoc/>
    public override bool HasTooltip
        => true;

    /// <inheritdoc/>
    public override void DrawTooltip()
    {
        var anySelected = fileSystem.Selection.DataNodes.Count > 0;
        var modifier = Enabled;

        Im.Text(anySelected
            ? "Delete the currently selected templates entirely from your drive\nThis can not be undone."u8
            : "No templates selected."u8);
        if (!modifier)
            Im.Text($"\nHold {config.UISettings.DeleteModifier} while clicking to delete the templates.");
    }

    /// <inheritdoc/>
    public override bool Enabled
        => config.UISettings.DeleteModifier.IsActive() && fileSystem.Selection.DataNodes.Count > 0;

    /// <inheritdoc/>
    public override void OnClick()
    {
        if (editorManager.IsEditorActive)
        {
            popupSystem.ShowPopup(PopupSystem.Messages.TemplateEditorActiveWarning);
            return;
        }

        var templates = fileSystem.Selection.DataNodes.Select(n => n.Value).OfType<Template>().ToList();
        fileSystem.Selection.UnselectAll();
        foreach (var template in templates)
            templateManager.Delete(template);
    }
}
