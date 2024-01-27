using CustomizePlus.Core.Services;
using OtterGui.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Configuration.Services.Temporary;

//V3 has bug when it doesn't create config file. We need to have one to migrate stuff properly.
internal class Version3ConfigFixer
{
    private readonly Logger _logger;
    private readonly FilenameService _filenameService;

    public Version3ConfigFixer(
        Logger logger,
        FilenameService filenameService)
    {
        _logger = logger;
        _filenameService = filenameService;
    }

    public void FixV3ConfigIfNeeded()
    {
        var oldVersionProfiles = Directory.EnumerateFiles(_filenameService.ConfigDirectory, "*.profile", SearchOption.TopDirectoryOnly);
        if (oldVersionProfiles.Count() > 0 && !File.Exists(_filenameService.ConfigFile))
        {
            _logger.Warning("V3 config not found while profiles are available, creating dummy V3 config");
            File.WriteAllText(_filenameService.ConfigFile, "{\r\n  \"ViewedMessageWindows\": [],\r\n  \"Version\": 3,\r\n  \"PluginEnabled\": true,\r\n  \"DebuggingModeEnabled\": false,\r\n  \"RootPositionEditingEnabled\": false\r\n}");
        }
    }
}
