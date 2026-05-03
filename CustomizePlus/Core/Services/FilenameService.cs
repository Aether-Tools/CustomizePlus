using CustomizePlus.Profiles.Data;
using Dalamud.Plugin;

namespace CustomizePlus.Core.Services;

public class FilenameService(IDalamudPluginInterface pi) : BaseFilePathProvider(pi)
{
    public new readonly string ConfigDirectory = pi.ConfigDirectory.FullName;

    public readonly string ProfileDirectory = Path.Combine(pi.ConfigDirectory.FullName, "profiles");
    public readonly string ProfileOrganization = Path.Combine(pi.ConfigDirectory.FullName, "profile_filesystem", "organization.json");
    public readonly string ProfileLockedNodes = Path.Combine(pi.ConfigDirectory.FullName, "profile_filesystem", "locked_nodes.json");
    public readonly string ProfileSelectedNodes = Path.Combine(pi.ConfigDirectory.FullName, "profile_filesystem", "selected_nodes.json");
    public readonly string ProfileExpandedFolders = Path.Combine(pi.ConfigDirectory.FullName, "profile_filesystem", "expanded_folders.json");

    public readonly string TemplateDirectory = Path.Combine(pi.ConfigDirectory.FullName, "templates");
    public readonly string TemplateOrganization = Path.Combine(pi.ConfigDirectory.FullName, "template_filesystem", "organization.json");
    public readonly string TemplateLockedNodes = Path.Combine(pi.ConfigDirectory.FullName, "template_filesystem", "locked_nodes.json");
    public readonly string TemplateSelectedNodes = Path.Combine(pi.ConfigDirectory.FullName, "template_filesystem", "selected_nodes.json");
    public readonly string TemplateExpandedFolders = Path.Combine(pi.ConfigDirectory.FullName, "template_filesystem", "expanded_folders.json");

    public readonly string UiConfigurationFile = Path.Combine(pi.ConfigDirectory.FullName, "ui_config.json");

    public readonly string MigrationTemplateFileSystemEmptyFolders =
        Path.Combine(pi.ConfigDirectory.FullName, "template_filesystem", "empty_folders.json");

    public readonly string MigrationTemplateFileSystem = Path.Combine(pi.ConfigDirectory.FullName, "template_sort_order.json");

    public readonly string MigrationProfileFileSystemEmptyFolders =
        Path.Combine(pi.ConfigDirectory.FullName, "profile_filesystem", "empty_folders.json");
    public readonly string MigrationProfileFileSystem = Path.Combine(pi.ConfigDirectory.FullName, "profile_sort_order.json");

    public override List<FileInfo> GetBackupFiles()
    {
        var list = new List<FileInfo>()
        {
            new(ConfigurationFile),
            new(TemplateLockedNodes),
            new(ProfileLockedNodes),
            new(UiConfigurationFile),
            new(MigrationTemplateFileSystemEmptyFolders),
            new(MigrationProfileFileSystemEmptyFolders)
        };

        list.AddRange(Templates());
        list.AddRange(Profiles());

        return list;
    }

    public IEnumerable<FileInfo> Templates()
    {
        if (!Directory.Exists(TemplateDirectory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(TemplateDirectory, "*.json", SearchOption.TopDirectoryOnly))
            yield return new FileInfo(file);
    }

    public string TemplateFile(Guid id)
        => Path.Combine(TemplateDirectory, $"{id}.json");

    public string TemplateFile(Templates.Data.Template template)
        => TemplateFile(template.UniqueId);

    public IEnumerable<FileInfo> Profiles()
    {
        if (!Directory.Exists(ProfileDirectory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(ProfileDirectory, "*.json", SearchOption.TopDirectoryOnly))
            yield return new FileInfo(file);
    }

    public string ProfileFile(Guid id)
        => Path.Combine(ProfileDirectory, $"{id}.json");

    public string ProfileFile(Profile profile)
        => ProfileFile(profile.UniqueId);
}
