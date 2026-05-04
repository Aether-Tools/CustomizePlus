using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Templates.Data;
using System.Diagnostics.CodeAnalysis;

namespace CustomizePlus.Templates;

public sealed class TemplateFileSystemSaver(LunaLogger log, BaseFileSystem fileSystem, SaveService saveService, TemplateManager templateManager)
    : FileSystemSaver<SaveService, FilenameService>(log, fileSystem, saveService)
{
    protected override void SaveDataValue(IFileSystemValue value)
    {
        if (value is Template template)
            SaveService.QueueSave(template);
    }

    protected override string LockedFile(FilenameService provider)
        => provider.TemplateLockedNodes;

    protected override string ExpandedFile(FilenameService provider)
        => provider.TemplateExpandedFolders;

    protected override string OrganizationFile(FilenameService provider)
        => provider.TemplateOrganization;

    protected override string EmptyFoldersMigrationFile(FilenameService provider)
        => provider.MigrationTemplateFileSystemEmptyFolders;

    protected override string SelectionFile(FilenameService provider)
        => provider.TemplateSelectedNodes;

    protected override string MigrationFile(FilenameService provider)
        => provider.MigrationTemplateFileSystem;

    protected override ISortMode? ParseSortMode(string name)
        => ISortMode.Valid.GetValueOrDefault(name);

    protected override bool GetValueFromIdentifier(ReadOnlySpan<char> identifier, [NotNullWhen(true)] out IFileSystemValue? value)
    {
        if (!Guid.TryParse(identifier, out var guid))
        {
            value = null;
            return false;
        }

        value = templateManager.Templates.FirstOrDefault(d => d.UniqueId == guid);
        return value is not null;
    }

    protected override void CreateDataNodes()
    {
        foreach (var template in templateManager.Templates)
        {
            try
            {
                var folder = template.Path.Folder.Length is 0 ? FileSystem.Root : FileSystem.FindOrCreateAllFolders(template.Path.Folder);
                FileSystem.CreateDuplicateDataNode(folder, template.Path.SortName ?? template.Name, template);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not create folder structure for template {template.Name} at path {template.Path.Folder}: {ex}");
            }
        }
    }
}
