using CustomizePlus.Core.Helpers;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;

public sealed class ExportToClipboardButton(TemplateFileSystem fileSystem, PopupSystem popupSystem) : BaseIconButton<AwesomeIcon>
{
    public override bool IsVisible
        => fileSystem.Selection.Selection is not null;

    public override AwesomeIcon Icon
        => LunaStyle.ToClipboardIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Copy the current template to your clipboard."u8);

    public override void OnClick()
    {
        var template = (Template)fileSystem.Selection.Selection!.Value;

        try
        {
            var text = Base64Helper.ExportTemplateToBase64(template);
            Im.Clipboard.Set(text);
            popupSystem.ShowPopup(PopupSystem.Messages.ClipboardDataNotLongTerm);
        }
        catch (Exception ex)
        {
            CustomizePlus.Logger.Error($"Could not copy data from template {template.UniqueId} to clipboard: {ex}");
            popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
        }
    }
}
