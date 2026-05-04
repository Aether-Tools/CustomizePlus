using CustomizePlus.Anamnesis;
using CustomizePlus.Templates;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.ImGuiNotification;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;

public sealed class AnamnesisImportButton(
    TemplateManager templateManager,
    TemplateEditorManager editorManager,
    PopupSystem popupSystem,
    MessageService messageService,
    PoseFileBoneLoader poseFileBoneLoader,
    FileDialogManager fileDialogManager) : BaseIconButton<AwesomeIcon>
{
    public override AwesomeIcon Icon
        => LunaStyle.DuplicateIcon;

    public override bool HasTooltip
        => true;

    public override bool Enabled
        => true;

    public override void DrawTooltip()
        => Im.Text("Import a template from anamnesis pose file (scaling only)"u8);

    public override void OnClick()
    {
        if (editorManager.IsEditorActive)
        {
            popupSystem.ShowPopup(PopupSystem.Messages.TemplateEditorActiveWarning);
            return;
        }

        fileDialogManager.OpenFileDialog("Import Pose File", ".pose", (isSuccess, path) =>
        {
            if (isSuccess)
            {
                var selectedFilePath = path.FirstOrDefault();
                if (selectedFilePath is null)
                    return;

                var bones = poseFileBoneLoader.LoadBoneTransformsFromFile(selectedFilePath);

                if (bones != null)
                {
                    if (bones.Count == 0)
                    {
                        messageService.NotificationMessage("Selected anamnesis pose file doesn't contain any scaled bones", NotificationType.Error);
                        return;
                    }

                    templateManager.Create(Path.GetFileNameWithoutExtension(selectedFilePath), bones, false);
                }
                else
                {
                    messageService.NotificationMessage(
                        $"Error parsing anamnesis pose file at '{path}'", NotificationType.Error);
                }
            }
            else
            {
                Logger.GlobalPluginLogger.Debug(isSuccess + " NO valid file has been selected. " + path);
            }
        }, 1, null, true);
    }
}