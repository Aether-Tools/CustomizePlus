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
        Add2_0_2_2(Changelog);
        Add2_0_3_0(Changelog);
        Add2_0_4_0(Changelog);
        Add2_0_4_1(Changelog);
    }

    private (int, ChangeLogDisplayType) ConfigData()
        => (_config.ChangelogSettings.LastSeenVersion, _config.ChangelogSettings.ChangeLogDisplayType);

    private void Save(int version, ChangeLogDisplayType type)
    {
        _config.ChangelogSettings.LastSeenVersion = version;
        _config.ChangelogSettings.ChangeLogDisplayType = type;
        _config.Save();
    }

    private static void Add2_0_4_1(Changelog log)
    => log.NextVersion("Version 2.0.4.1")
        .RegisterEntry("Added support for new worlds.")
        .RegisterEntry("Source code maintenance - external libraries update.");

    private static void Add2_0_4_0(Changelog log)
        => log.NextVersion("Version 2.0.4.0")
            .RegisterImportant("Version 3 IPC has been removed, any plugins still relying on it will stop working until updated.")
            .RegisterEntry("Mare Synchronos and Dynamic Bridge are not affected.", 1)
            .RegisterEntry("Added option to configure if profiles should be applied on character select screen during login.")
            .RegisterEntry("Made information level plugin logs less verbose.")
            .RegisterEntry("Source code maintenance - external libraries update.");

    private static void Add2_0_3_0(Changelog log)
        => log.NextVersion("Version 2.0.3.0")
            .RegisterEntry("Added option to configure if profiles should affect various parts of game's user interface:")
            .RegisterEntry("Character window", 1)
            .RegisterEntry("Try-On, Dye Preview, Glamour Plate windows", 1)
            .RegisterEntry("Adventurer Cards (Portraits)", 1)
            .RegisterEntry("Inspect window", 1)
            .RegisterEntry("Added option to configure if template editor preview character should be automatically set to current character when you login. This is disabled by default.")
            .RegisterEntry("Enabled profiles can no longer be set as default profile.")
            .RegisterEntry("Fixed current player character's profile applying to special actors (portraits, etc) of other characters.")
            .RegisterEntry("Fixed temporary profile being removed when closing inspection window of character with active temporary profile.")
            .RegisterEntry("Fixed profile not applying if it was enabled shortly after doing penumbra redraw.")
            .RegisterEntry("Fixed issue when switching to a different profile did not reflect on special actors (portraits, etc).")
            .RegisterEntry("Fixed legacy IPC's RevertCharacter method leaking exceptions. (2.0.2.4)")
            .RegisterEntry("Source code maintenance - external libraries update, refactoring, cleanup.");

    private static void Add2_0_2_2(Changelog log)
        => log.NextVersion("Version 2.0.2.2")
            .RegisterHighlight("Added brand new IPC (version 4) for cross-plugin interraction. (2.0.2.0)")
            .RegisterEntry("Please refer to repository readme on GitHub for information about using it.", 1)
            .RegisterImportant("Old IPC (version 3) is still available, but it will be removed sometime before Dawntrail release. Plugin developers are advised to migrate as soon as possible.", 1)
            .RegisterEntry("Updated to .NET 8. (2.0.2.0)")
            .RegisterEntry("Updated external libraries. (2.0.2.1)")
            .RegisterEntry("Added additional cleanup of user input. (2.0.2.0)")
            .RegisterEntry("Selected default profile can no longer be changed if profile set as default is enabled. (2.0.2.1)")
            .RegisterEntry("Profiles can no longer be enabled/disabled while editor is active. (2.0.2.1)")
            .RegisterEntry("Fixed incorrect warning message priorities in main window. (2.0.2.1)")
            .RegisterEntry("Fixed \"Limit to my creatures\" not ignoring objects other than summons, minions and mounts. (2.0.2.1)")
            .RegisterEntry("Fixed text in various places. (2.0.2.1)");

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
