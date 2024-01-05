using Dalamud.Interface.Internal.Notifications;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.Log;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CustomizePlus.Core.Services;
using CustomizePlus.Templates.Events;
using CustomizePlus.Templates.Data;

namespace CustomizePlus.Templates;

//Adapted from glamourer source code
public sealed class TemplateFileSystem : FileSystem<Template>, IDisposable, ISavable
{
    private readonly TemplateManager _templateManager;
    private readonly SaveService _saveService;
    private readonly TemplateChanged _templateChanged;
    private readonly MessageService _messageService;
    private readonly Logger _logger;

    public TemplateFileSystem(
        TemplateManager templateManager,
        SaveService saveService,
        TemplateChanged templateChanged,
        MessageService messageService,
        Logger logger)
    {
        _templateManager = templateManager;
        _saveService = saveService;
        _templateChanged = templateChanged;
        _messageService = messageService;
        _logger = logger;

        _templateChanged.Subscribe(OnTemplateChange, TemplateChanged.Priority.TemplateFileSystem);

        Changed += OnChange;

        Reload();
    }

    public void Dispose()
    {
        _templateChanged.Unsubscribe(OnTemplateChange);
    }

    // Search the entire filesystem for the leaf corresponding to a template.
    public bool FindLeaf(Template template, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Template>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == template);
        return leaf != null;
    }

    private void OnTemplateChange(TemplateChanged.Type type, Template? template, object? data)
    {
        switch (type)
        {
            case TemplateChanged.Type.Created:
                var parent = Root;
                if (data is string path)
                    try
                    {
                        parent = FindOrCreateAllFolders(path);
                    }
                    catch (Exception ex)
                    {
                        _messageService.NotificationMessage(ex, $"Could not move template to {path} because the folder could not be created.", NotificationType.Error);
                    }

                CreateDuplicateLeaf(parent, template.Name.Text, template);

                return;
            case TemplateChanged.Type.Deleted:
                if (FindLeaf(template, out var leaf1))
                    Delete(leaf1);
                return;
            case TemplateChanged.Type.ReloadedAll:
                Reload();
                return;
            case TemplateChanged.Type.Renamed when data is string oldName:
                if (!FindLeaf(template, out var leaf2))
                    return;

                var old = oldName.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, template.Name);
                return;
        }
    }

    private void Reload()
    {
        if (Load(new FileInfo(_saveService.FileNames.TemplateFileSystem), _templateManager.Templates, TemplateToIdentifier, TemplateToName))
        {
            var shouldReloadAgain = false;

            if (!File.Exists(_saveService.FileNames.TemplateFileSystem))
                shouldReloadAgain = true;

            _saveService.ImmediateSave(this);

            //this is a workaround for FileSystem's weird behavior where it doesn't load objects into itself if its file does not exist
            if (shouldReloadAgain)
            {
                _logger.Debug("BUG WORKAROUND: reloading template filesystem again");
                Reload();
                return;
            }
        }

        _logger.Debug("Reloaded template filesystem.");
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _saveService.QueueSave(this);
    }

    // Used for saving and loading.
    private static string TemplateToIdentifier(Template template)
        => template.UniqueId.ToString();

    private static string TemplateToName(Template template)
        => template.Name.Text.FixName();

    private static bool TemplateHasDefaultPath(Template template, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(TemplateToName(template))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveTemplate(Template template, string fullPath)
        // Only save pairs with non-default paths.
        => TemplateHasDefaultPath(template, fullPath)
            ? (string.Empty, false)
            : (TemplateToIdentifier(template), true);

    public string ToFilename(FilenameService fileNames) => fileNames.TemplateFileSystem;

    public void Save(StreamWriter writer)
    {
        SaveToFile(writer, SaveTemplate, true);
    }
}
