using CustomizePlus.Core.Services;
using OtterGui.Log;
using System.IO;

namespace CustomizePlus.Configuration.Services.Temporary;

internal class FantasiaPlusConfigMover
{
    private readonly Logger _logger;
    private readonly FilenameService _filenameService;
    private readonly BackupService _backupService;

    public FantasiaPlusConfigMover(
        BackupService backupService,
        Logger logger,
        FilenameService filenameService
        )
    {
        _backupService = backupService;
        _logger = logger;
        _filenameService = filenameService;
    }

    public void MoveConfigsIfNeeded()
    {
        string fantasiaPlusConfig = _filenameService.ConfigFile.Replace("CustomizePlus.json", "FantasiaPlus.json");
        if (!File.Exists(_filenameService.ConfigFile.Replace("CustomizePlus.json", "FantasiaPlus.json")))
            return;

        _logger.Information("Found FantasiaPlus configuration, moving it to CustomizePlus folders");
        if(File.Exists(_filenameService.ConfigFile) || Directory.Exists(_filenameService.ConfigDirectory))
        {
            _logger.Debug("Creating a backup of current c+ config");
            _backupService.CreateV3Backup("fantasia_plus_migration");
        }

        _logger.Debug("Removing current c+ data");
        File.Delete(_filenameService.ConfigFile);
        Directory.Delete(_filenameService.ConfigDirectory, true);

        _logger.Debug("Copying fantasia+ data");
        File.Copy(fantasiaPlusConfig, _filenameService.ConfigFile);

        string fantasiaPlusDirectory = _filenameService.ConfigDirectory.Replace("CustomizePlus", "FantasiaPlus");
        CopyDirectory(fantasiaPlusDirectory, _filenameService.ConfigDirectory, true);

        _logger.Debug("Deleting fantasia+ data");
        File.Delete(fantasiaPlusConfig);
        Directory.Delete(fantasiaPlusDirectory, true);

        _logger.Information("Done moving fantasia+ configuration");
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        if (recursive)
        {
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }
}
