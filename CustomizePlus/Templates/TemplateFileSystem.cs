using CustomizePlus.Core.Services;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates.Events;
using Dalamud.Interface.ImGuiNotification;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonItemDetailCompare;

namespace CustomizePlus.Templates;

public sealed class TemplateFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly TemplateFileSystemSaver _saver;
    private readonly TemplateChanged _templateChanged;
    //private readonly TabSelected _tabSelected;

    public TemplateFileSystem(LunaLogger log, SaveService saveService, TemplateManager templateManager, TemplateChanged templateChanged/*, TabSelected tabSelected*/)
        : base("TemplateFileSystem", log, true)
    {
        _templateChanged = templateChanged;
        //_tabSelected = tabSelected;
        _saver = new TemplateFileSystemSaver(log, this, saveService, templateManager);

        _saver.Load();
        _templateChanged.Subscribe(OnDesignChanged, TemplateChanged.Priority.TemplateFileSystem);
        //_tabSelected.Subscribe(OnTabSelected, TabSelected.Priority.DesignSelector);
    }

   /* private void OnTabSelected(in TabSelected.Arguments arguments)
    {
        if (arguments.Design?.Node is { } node)
            Selection.Select(node, true);
    }*/

    private void OnDesignChanged(in TemplateChanged.Arguments arguments)
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
                       /* Glamourer.Messager.NotificationMessage(ex,
                            $"Could not move design to {folder} because the folder could not be created.",
                            NotificationType.Error);*/ //todo
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
       // _tabSelected.Unsubscribe(OnTabSelected);
        _templateChanged.Unsubscribe(OnDesignChanged);
    }
}


/*public sealed class TemplateFileSystem : BaseFileSystem, IDisposable, IRequiredService
{
    private readonly TemplateManager _templateManager;
    private readonly TemplateChanged _templateChanged;
    private readonly MessageService _messageService;
    private readonly FileSystemSaveService<Template> _saver;

    public TemplateFileSystem(
        TemplateManager templateManager,
        SaveService saveService,
        TemplateChanged templateChanged,
        MessageService messageService,
        Logger logger)
        : base("TemplateFileSystem", logger, true)
    {
        _templateManager = templateManager;
        _templateChanged = templateChanged;
        _messageService = messageService;
        _saver = new FileSystemSaveService<Template>(
            logger,
            this,
            saveService,
            _templateManager.Templates,
            TemplateFromIdentifier,
            fileNames => fileNames.TemplateLockedNodes,
            fileNames => fileNames.TemplateExpandedFolders,
            fileNames => fileNames.TemplateSelectedNodes,
            fileNames => fileNames.TemplateOrganization,
            fileNames => fileNames.LegacyTemplateSortOrder);

        _templateChanged.Subscribe(OnTemplateChange, TemplateChanged.Priority.TemplateFileSystem);
        _saver.Load();
    }

    public void Dispose()
    {
        _templateChanged.Unsubscribe(OnTemplateChange);
        _saver.Dispose();
        Selection.Dispose();
    }

    private Template? TemplateFromIdentifier(string identifier)
        => Guid.TryParse(identifier, out var id)
            ? _templateManager.GetTemplate(id)
            : null;

    private void OnTemplateChange(in TemplateChanged.Arguments args)
    {
        var (type, template, data) = args;
        switch (type)
        {
            case TemplateChanged.Type.Created when template is not null:
                var parent = Root;
                if (data is string path)
                {
                    try
                    {
                        parent = FindOrCreateAllFolders(path);
                    }
                    catch (Exception ex)
                    {
                        _messageService.NotificationMessage(ex, $"Could not move template to {path} because the folder could not be created.", NotificationType.Error);
                    }
                }

                CreateDuplicateDataNode(parent, template.Name, template);
                return;
            case TemplateChanged.Type.Deleted when template?.Node is { } node:
                Delete(node);
                return;
            case TemplateChanged.Type.ReloadedAll:
                _saver.Load();
                return;
            case TemplateChanged.Type.Renamed when template?.Node is { } node && data is string oldName:
                var old = oldName.FixName();
                var name = node.Name.ToString();
                if (old == name || (name.IsDuplicateName(out var baseName, out _) && baseName == old))
                    RenameWithDuplicates(node, template.Name);
                return;
        }
    }
}
*/