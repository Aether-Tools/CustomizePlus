using OtterGui.Classes;
using OtterGui.Log;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CustomizePlusPlus.Core.Services;

public class BackupService
{
    private readonly Logger _logger;
    private readonly FilenameService _filenameService;
    private readonly DirectoryInfo _configDirectory;
    private readonly IReadOnlyList<FileInfo> _fileNames;

    public BackupService(Logger logger, FilenameService filenameService)
    {
        _logger = logger;
        _filenameService = filenameService;
        _fileNames = PluginFiles(_filenameService);
        _configDirectory = new DirectoryInfo(_filenameService.ConfigDirectory);
        Backup.CreateAutomaticBackup(logger, _configDirectory, _fileNames);
    }

    /// <summary>
    /// Create a permanent backup with a given name for migrations. 
    /// </summary>
    public void CreateMigrationBackup(string name)
        => Backup.CreatePermanentBackup(_logger, _configDirectory, _fileNames, name);

    /// <summary>
    /// Create backup for all version 3 configuration files
    /// </summary>
    public void CreateV3Backup(string name = "v3_to_v4_migration")
    {
        var list = new List<FileInfo>(16) { new(_filenameService.ConfigFile) };
        list.AddRange(Directory.EnumerateFiles(_filenameService.ConfigDirectory, "*.profile", SearchOption.TopDirectoryOnly).Select(x => new FileInfo(x)));

        Backup.CreatePermanentBackup(_logger, _configDirectory, list, name);
    }

    /// <summary>
    /// Collect all relevant files for plugin configuration.
    /// </summary>
    private static IReadOnlyList<FileInfo> PluginFiles(FilenameService fileNames)
    {
        var list = new List<FileInfo>(16)
        {
            new(fileNames.ConfigFile),
            new(fileNames.ProfileFileSystem),
            new(fileNames.TemplateFileSystem)
        };

        list.AddRange(fileNames.Profiles());
        list.AddRange(fileNames.Templates());

        return list;
    }
}
