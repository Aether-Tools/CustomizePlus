using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Data;
using System.Diagnostics.CodeAnalysis;

namespace CustomizePlus.Profiles;

public sealed class ProfileFileSystemSaver(LunaLogger log, BaseFileSystem fileSystem, SaveService saveService, ProfileManager profileManager)
    : FileSystemSaver<SaveService, FilenameService>(log, fileSystem, saveService)
{
    protected override void SaveDataValue(IFileSystemValue value)
    {
        if (value is Profile profile)
            SaveService.QueueSave(profile);
    }

    protected override string LockedFile(FilenameService provider)
        => provider.ProfileLockedNodes;

    protected override string ExpandedFile(FilenameService provider)
        => provider.ProfileExpandedFolders;

    protected override string OrganizationFile(FilenameService provider)
        => provider.ProfileOrganization;
    protected override string EmptyFoldersMigrationFile(FilenameService provider)
        => provider.MigrationProfileFileSystemEmptyFolders;

    protected override string SelectionFile(FilenameService provider)
        => provider.ProfileSelectedNodes;

    protected override string MigrationFile(FilenameService provider)
        => provider.MigrationProfileFileSystem;

    protected override ISortMode? ParseSortMode(string name)
        => ISortMode.Valid.GetValueOrDefault(name);

    protected override bool GetValueFromIdentifier(ReadOnlySpan<char> identifier, [NotNullWhen(true)] out IFileSystemValue? value)
    {
        if (!Guid.TryParse(identifier, out var guid))
        {
            value = null;
            return false;
        }

        value = profileManager.Profiles.FirstOrDefault(d => d.UniqueId == guid);
        return value is not null;
    }

    protected override void CreateDataNodes()
    {
        foreach (var profile in profileManager.Profiles)
        {
            try
            {
                var folder = profile.Path.Folder.Length is 0 ? FileSystem.Root : FileSystem.FindOrCreateAllFolders(profile.Path.Folder);
                FileSystem.CreateDuplicateDataNode(folder, profile.Path.SortName ?? profile.Name, profile);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not create folder structure for profile {profile.Name} at path {profile.Path.Folder}: {ex}");
            }
        }
    }
}

