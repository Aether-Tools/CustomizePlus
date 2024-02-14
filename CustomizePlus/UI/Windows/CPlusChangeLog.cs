using CustomizePlus.Configuration.Data;
using OtterGui.Widgets;

namespace CustomizePlus.UI.Windows;

public class CPlusChangeLog
{
    public const int LastChangelogVersion = 0;
    private readonly PluginConfiguration _config;
    public readonly Changelog Changelog;

    public CPlusChangeLog(PluginConfiguration config)
    {
        _config = config;
        Changelog = new Changelog("Customize+ update history", ConfigData, Save);

        Add2_0_0_0(Changelog);
        Add2_0_1_0(Changelog);
    }

    private (int, ChangeLogDisplayType) ConfigData()
        => (_config.ChangelogSettings.LastSeenVersion, _config.ChangelogSettings.ChangeLogDisplayType);

    private void Save(int version, ChangeLogDisplayType type)
    {
        _config.ChangelogSettings.LastSeenVersion = version;
        _config.ChangelogSettings.ChangeLogDisplayType = type;
        _config.Save();
    }

    private static void Add2_0_1_0(Changelog log)
        => log.NextVersion("Version 2.0.1.0")
            .RegisterHighlight("Added support for legacy clipboard copies.")
            .RegisterEntry("Added setting allowing disabling of confirmation messages for chat commands.")
            .RegisterEntry("Template and profile editing is no longer disabled during GPose.")
            .RegisterImportant("Customize+ is not 100% compatible with posing tools such as Ktisis, Brio and Anamnesis. Some features of those tools might alter Customize+ behavior or prevent it from working.", 1)
            .RegisterHighlight("Fixed crash during \"Duty Complete\" cutscenes.")
            .RegisterEntry("Fixed settings migration failing completely if one of the profiles is corrupted.")
            .RegisterEntry("Improved error handling.")
            .RegisterHighlight("Customize+ window will now display warning message if plugin encounters a critical error.", 1);
   
    private static void Add2_0_0_0(Changelog log)
        => log.NextVersion("Version 2.0.0.0")
            .RegisterHighlight("Major rework of the entire plugin.")
            .RegisterEntry("Settings and profiles from previous version will be automatically converted to new format on the first load.", 1)
            .RegisterImportant("Old version configuration is backed up in case something goes wrong, please report any issues with configuration migration as soon as possible.", 2)
            .RegisterImportant("Clipboard copies from previous versions are not currently supported.", 2)
            .RegisterImportant("Profiles from previous versions will only be loaded during first load.", 2)

            .RegisterHighlight("Major changes:")

            .RegisterEntry("Plugin has been almost completely rewritten from scratch.", 1)

            .RegisterEntry("User interface has been moved to the framework used by Glamourer and Penumbra, so the interface should feel familiar to the users of those plugins.", 1)
            .RegisterEntry("User interface issues related to different resolutions and font sizes should *mostly* not occur anymore.", 2)
            .RegisterImportant("There are several issues with text not fitting in some places depending on your screen resolution and font size. This will be fixed later.", 3)

            .RegisterEntry("Template system has been added", 1)
            .RegisterEntry("All bone edits are now stored in templates which can be used by multiple profiles and single profile can reference unlimited number of templates.", 2)

            .RegisterImportant("Chat commands have been changed, refer to \"/customize help\" for information about available commands.", 1)

            .RegisterEntry("Profiles can be applied to summons, mounts and pets without any limitations.", 1)
            .RegisterImportant("Root scaling of mounts is not available for now.", 2)

            .RegisterEntry("Fixed \"Only owned\" not working properly in various cases and renamed it to \"Limit to my creatures\".", 1)

            .RegisterEntry("Fixed profiles \"leaking\" to other characters due to the way original Mare Synchronos integration implementation was handled.", 1)

            .RegisterEntry("Compatibility with cutscenes is improved, but that was not extensively tested.", 1)

            .RegisterEntry("Plugin configuration is now being regularly backed up, the backup is located in %appdata%\\XIVLauncher\\backups\\CustomizePlus folder", 1);
}
