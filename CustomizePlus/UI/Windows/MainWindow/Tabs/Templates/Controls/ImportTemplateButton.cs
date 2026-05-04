using CustomizePlus.Configuration.Data.Version2;
using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Configuration.Helpers;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;

public sealed class ImportTemplateButton(
    TemplateManager templateManager,
    TemplateEditorManager editorManager,
    PopupSystem popupSystem) : BaseIconButton<AwesomeIcon>
{
    private string _clipboardText = string.Empty;

    public override AwesomeIcon Icon
        => LunaStyle.ImportIcon;

    public override bool HasTooltip
        => true;

    public override void DrawTooltip()
        => Im.Text("Try to import a design from your clipboard."u8);

    public override void OnClick()
    {
        if (editorManager.IsEditorActive)
        {
            popupSystem.ShowPopup(PopupSystem.Messages.TemplateEditorActiveWarning);
            return;
        }

        try
        {
            _clipboardText = Im.Clipboard.GetUtf16();
            Im.Popup.Open("##ImportTemplate"u8);
        }
        catch (Exception)
        {
            popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
        }
    }

    protected override void PostDraw()
    {
        if (!InputPopup.OpenName("##ImportTemplate"u8, out var newName))
            return;

        if (_clipboardText.Length is 0)
            return;

        try
        {
            var importVer = Base64Helper.ImportFromBase64(_clipboardText, out var json);

            var template = Convert.ToInt32(importVer) switch
            {
                2 => GetTemplateFromV2Profile(json),
                3 => GetTemplateFromV3Profile(json),
                4 => JsonConvert.DeserializeObject<Template>(json),
                5 => JsonConvert.DeserializeObject<Template>(json),
                _ => null
            };

            if (template is Template tpl && tpl != null)
                templateManager.Clone(tpl, newName, true);
            else
                popupSystem.ShowPopup(PopupSystem.Messages.ClipboardDataUnsupported);

        }
        catch (Exception ex)
        {
            Logger.GlobalPluginLogger.Error($"Error while performing clipboard/clone/create template action: {ex}");
            popupSystem.ShowPopup(PopupSystem.Messages.ActionError);
        }
        finally
        {
            _clipboardText = string.Empty;
        }
    }

    private Template? GetTemplateFromV2Profile(string json)
    {
        var profile = JsonConvert.DeserializeObject<Version2Profile>(json);
        if (profile != null)
        {
            var v3Profile = V2ProfileToV3Converter.Convert(profile);

            (var _, var template) = V3ProfileToV4Converter.Convert(v3Profile);

            if (template != null)
                return template;
        }

        return null;
    }

    private Template? GetTemplateFromV3Profile(string json)
    {
        var profile = JsonConvert.DeserializeObject<Version3Profile>(json);
        if (profile != null)
        {
            if (profile.ConfigVersion != 3)
                throw new Exception("Incompatible profile version");

            (var _, var template) = V3ProfileToV4Converter.Convert(profile);

            if (template != null)
                return template;
        }

        return null;
    }
}
