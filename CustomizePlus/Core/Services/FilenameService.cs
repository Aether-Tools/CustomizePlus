using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using CustomizePlus.Profiles.Data;

namespace CustomizePlus.Core.Services;

public class FilenameService
{
    public readonly string ConfigDirectory;
    public readonly string ConfigFile;
    public readonly string ProfileDirectory;
    public readonly string ProfileFileSystem;
    public readonly string TemplateDirectory;
    public readonly string TemplateFileSystem;

    public FilenameService(DalamudPluginInterface pi)
    {
        ConfigDirectory = pi.ConfigDirectory.FullName;
        ConfigFile = pi.ConfigFile.FullName;
        ProfileDirectory = Path.Combine(ConfigDirectory, "profiles");
        ProfileFileSystem = Path.Combine(ConfigDirectory, "profile_sort_order.json");
        TemplateDirectory = Path.Combine(ConfigDirectory, "templates");
        TemplateFileSystem = Path.Combine(ConfigDirectory, "template_sort_order.json");
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
