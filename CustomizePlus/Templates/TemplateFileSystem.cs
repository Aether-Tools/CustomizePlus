using CustomizePlus.Core.Services;
using CustomizePlus.Templates.Events;
using Dalamud.Interface.ImGuiNotification;

namespace CustomizePlus.Templates;

public sealed class TemplateFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly TemplateFileSystemSaver _saver;
    private readonly TemplateChanged _templateChanged;

    public TemplateFileSystem(LunaLogger log, SaveService saveService, TemplateManager templateManager, TemplateChanged templateChanged)
        : base("TemplateFileSystem", log, true)
    {
        _templateChanged = templateChanged;
        _saver = new TemplateFileSystemSaver(log, this, saveService, templateManager);

        _saver.Load();
        _templateChanged.Subscribe(OnTemplateChanged, TemplateChanged.Priority.TemplateFileSystem);
    }

    private void OnTemplateChanged(in TemplateChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case TemplateChanged.Type.ReloadedAll: _saver.Load(); break;
            case TemplateChanged.Type.Created:
                var parent = Root;
                var folder = arguments.Template!.Path.Folder;
                if (folder.Length > 0)
                    try
                    {
                        parent = FindOrCreateAllFolders(folder);
                    }
                    catch (Exception ex)
                    {
                        CustomizePlus.Messager.NotificationMessage(ex,
                            $"Could not move template to {folder} because the folder could not be created.",
                            NotificationType.Error);
                    }

                var (data, _) = CreateDuplicateDataNode(parent, arguments.Template!.Path.SortName ?? arguments.Template.Name, arguments.Template);
                Selection.Select(data, true);
                break;
            case TemplateChanged.Type.Deleted:
                if (arguments.Template!.Node is { } node)
                {
                    if (node.Selected)
                        Selection.UnselectAll();
                    Delete(node);
                }

                break;
            case TemplateChanged.Type.Renamed when arguments.Template!.Path.SortName is null:
                RenameWithDuplicates(arguments.Template.Node!, arguments.Template.Path.GetIntendedName(arguments.Template.Name));
                break;
                // TODO: Maybe add path changes?
        }
    }

    public void Dispose()
    {
        _templateChanged.Unsubscribe(OnTemplateChanged);
    }
}