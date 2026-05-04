using CustomizePlus.Configuration.Data;

namespace CustomizePlus.UI.Windows;

//Versioning concept (X.Y.Z.W):
//X - major version, changes only during major rewrites of the plugin
//Y - major feature version, changes when new major features are introduced (also can be changed with major game patches)
//Z - minor feature version, changes when new minor features are introduced
//W - bugfix version, changes when the update only contains bugfixes.

public class CPlusChangeLog
{
    public const int LastChangelogVersion = 0;
    private readonly PluginConfiguration _configuration;
    public readonly Changelog Changelog;

    public CPlusChangeLog(PluginConfiguration configuration)
    {
        _configuration = configuration;
        Changelog = new Changelog("Customize+ update history", ConfigData, Save);

        Add2_0_0_0(Changelog);
        Add2_0_1_0(Changelog);
        Add2_0_2_2(Changelog);
        Add2_0_3_0(Changelog);
        Add2_0_4_0(Changelog);
        Add2_0_4_1(Changelog);
        Add2_0_4_4(Changelog);
        Add2_0_5_0(Changelog);
        Add2_0_6_0(Changelog);
        Add2_0_6_3(Changelog);
        Add2_0_7_0(Changelog);
        Add2_0_7_2(Changelog);
        Add2_0_7_9(Changelog);
        Add2_0_7_15(Changelog);
        Add2_0_7_16(Changelog);
        Add2_0_7_23(Changelog);
        Add2_0_7_27(Changelog);
        Add2_0_8_0(Changelog);
        Add2_0_8_2(Changelog);
        Add2_0_8_4(Changelog);
        Add2_0_9_0(Changelog);
        Add2_1_0_0(Changelog);
        Add2_1_1_0(Changelog);
    }

    private (int, ChangeLogDisplayType) ConfigData()
        => (_configuration.ChangelogSettings.LastSeenVersion, _configuration.ChangelogSettings.ChangeLogDisplayType);

    private void Save(int version, ChangeLogDisplayType type)
    {
        _configuration.ChangelogSettings.LastSeenVersion = version;
        _configuration.ChangelogSettings.ChangeLogDisplayType = type;
        _configuration.Save();
    }

    // i know it's ugly but luna changelog api does expect u8 !!
    private static void Add2_1_1_0(Changelog log)
        => log.NextVersion("Version 2.1.1.0"u8)
        .RegisterEntry("Added button to export the entire profile as a single template. (by MBadea21)"u8);

    private static void Add2_1_0_0(Changelog log)
        => log.NextVersion("Version 2.1.0.0"u8)
        .RegisterImportant("Support for 7.4 and Dalamud API 14. (by Risa)"u8);

    private static void Add2_0_9_0(Changelog log)
        => log.NextVersion("Version 2.0.9.0"u8)
        .RegisterEntry("Added ability to apply separate scaling to child bones when propagation is enabled on a bone. (by Midona)"u8)
        .RegisterEntry("When enabling transformations propagation on \"Scale\" option you will now see an additional \"Child Bones\" entry appear which gives you finer control over scale of the bones. This can be especially useful for modifying shape of character tail."u8, 1);

    private static void Add2_0_8_4(Changelog log)
        => log.NextVersion("Version 2.0.8.4"u8)
        .RegisterEntry("Accessories can now be manipulated by Customize+. (by Caraxi)"u8)
        .RegisterEntry("The extent of possible manipulations depends on the chosen accessory."u8, 1)
        .RegisterEntry("Bones with their values set to 0 will no longer be removed from the editor when \"Show Live Bones\" is off and option to apply transformation to children is on. (by Caraxi and Risa) (2.0.8.3)"u8);

    private static void Add2_0_8_2(Changelog log)
        => log.NextVersion("Version 2.0.8.2"u8)
        .RegisterEntry("Improved stability of Penumbra PCP integration. (by abelfreyja)"u8)
        .RegisterEntry("Customize+ will now show warning in the menu bar if it cannot connect to Penumbra. (by Risa)"u8, 1)
        .RegisterEntry("Fixed root position reset applying when it shouldn't. (by abelfreyja)"u8)
        .RegisterEntry("Fixed profile folders resetting. (by Risa)"u8);

    private static void Add2_0_8_0(Changelog log)
        => log.NextVersion("Version 2.0.8.0"u8)
        .RegisterHighlight("Added support for Penumbra PCP files. (by abelfreyja)"u8)
        .RegisterEntry("This feature is enabled by default and can be disabled in Settings -> Integrations menu."u8, 1)
        .RegisterHighlight("Added bone edits propagation. (by d87)"u8)
        .RegisterEntry("This feature might not work correctly with some bones and with some combinations of bone edits."u8, 1)
        .RegisterEntry("Added search filter and undo/redo functionality during bone editing. (by abelfreyja)"u8)
        .RegisterEntry("Added the ability to copy bone groups to clipboard and import them. (by abelfreyja)"u8)
        .RegisterEntry("Right click on group name to access this functionality."u8, 1)
        .RegisterEntry("Added the ability to have favorite bones. (by abelfreyja and Risa)"u8)
        .RegisterEntry("IPC version updated to 6.3."u8)
        .RegisterEntry("Added Profile.SetPriorityByUniqueId IPC endpoint. (by CordeliaMist)"u8, 1)
        .RegisterEntry("Bone propagation settings are now returned where applicable. (by abelfreyja)"u8, 1);

    private static void Add2_0_7_27(Changelog log)
        => log.NextVersion("Version 2.0.7.27"u8)
        .RegisterEntry("Added ability to toggle template in a profile without removing it. (by Caraxi)"u8)
        .RegisterEntry("IPC version updated to 6.2."u8)
        .RegisterEntry("Added Profile.GetTemplates, Profile.EnableTemplateByUniqueId, Profile.DisableTemplateByUniqueId IPC endpoints. (by Caraxi)"u8, 1)
        .RegisterEntry("Fixed crash when trying to open template/profile tab if there is a template/profile with empty name. (2.0.7.25)"u8);

    private static void Add2_0_7_23(Changelog log)
        => log.NextVersion("Version 2.0.7.23"u8)
        .RegisterImportant("Support for 7.3 and Dalamud API 13."u8)
        .RegisterEntry("IPC version updated to 6.1. (2.0.7.20)"u8)
        .RegisterEntry("Added Profile.AddPlayerCharacter and Profile.RemovePlayerCharacter IPC endpoints. (by Caraxi)"u8, 1)
        .RegisterEntry("Left side selectors in \"Templates\" and \"Profiles\" tabs can now be resized."u8)
        .RegisterEntry("Fixed crashes on login/logout."u8)
        .RegisterEntry("This usually happened when \"Apply Profiles on Character Select Screen\" and/or \"Automatically Set Current Character as Editor Preview Character\" options are enabled in settings."u8, 1)
        .RegisterEntry("Fixed root transforms sometimes not resetting when toggling between profiles until character is moved."u8)
        .RegisterEntry("Fixed an issue where profiles would attempt to be applied to objects not currently drawn on the screen."u8)
        .RegisterEntry("Slight refactoring of user interface code."u8);

    private static void Add2_0_7_16(Changelog log)
        => log.NextVersion("Version 2.0.7.16"u8)
        .RegisterImportant("Support for update 7.2 and Dalamud API 12."u8);

    private static void Add2_0_7_15(Changelog log)
        => log.NextVersion("Version 2.0.7.15"u8)
        .RegisterEntry("Optimized JSON payload returned by Profile.GetByUniqueId IPC method. (requested by Mare Synchronos)"u8)
        .RegisterEntry("Values which are considered default are no longer returned, this drastically reduces the size of returned data."u8, 1)
        .RegisterEntry("Fixed clipboard copies missing version data."u8)
        .RegisterEntry("You do not need to do anything about this. This just fixes format inconsistencies between clipboard copies and on-disk data."u8, 1)
        .RegisterEntry("Fixed (some?) \"ImGui assertation failed\" errors (2.0.7.14)"u8)
        .RegisterEntry("Improved support log contents (2.0.7.13)"u8)
        .RegisterEntry("Fixed skeleton changes not being detected when changing between hairstyles with the same amount of bones. (2.0.7.11)"u8)
        .RegisterEntry("Fixed character flashing in GPose. (Root bone position edits are no longer applied in GPose) (2.0.7.10)"u8)

        .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_7_9(Changelog log)
        => log.NextVersion("Version 2.0.7.9"u8)
        .RegisterEntry("Added donation button to settings tab."u8)
        .RegisterEntry("Saving changes to the template used in the active profile will now tell other plugins that the profile was changed by sending OnProfileUpdate IPC event. (2.0.7.8)"u8)
        .RegisterEntry("Root bone position edits no longer require character to move in order to apply. (2.0.7.6)"u8)

        .RegisterEntry("Fixed \"Apply to any character you are logged in with\" profile option being ignored by Profile.GetActiveProfileIdOnCharacter IPC function preventing other plugins from being able to detect active profile with this option enabled. (2.0.7.8)"u8)

        .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_7_2(Changelog log)
        => log.NextVersion("Version 2.0.7.2"u8)
        .RegisterImportant("Support for 7.1 and Dalamud API 11."u8)
        .RegisterHighlight("Fixed an issue which prevented owned characters (such as Carbuncles and Trust NPCs) from being detected. (2.0.7.1)"u8)

        .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_7_0(Changelog log)
        => log.NextVersion("Version 2.0.7.0"u8)
        .RegisterImportant("Some parts of Customize+ have been considerably rewritten in this update. If you encounter any issues please report them."u8)

        .RegisterHighlight("Character management has been rewritten."u8)
        .RegisterImportant("Customize+ will do its best to automatically migrate your profiles to new system but in some rare cases it is possible that you will have to add characters again for some of your profiles."u8, 1)
        .RegisterEntry("Character selection user interface has been redesigned."u8, 1)
        .RegisterEntry("It is now possible to assign several characters to a single profile."u8, 2)
        .RegisterEntry("The way console commands work has not changed. This means that the commands will affect profiles the same way as before, even if profile affects multiple characters."u8, 3)
        .RegisterEntry("\"Limit to my creatures\" option has been removed as it is now obsolete."u8, 2)
        .RegisterEntry("It is now possible to choose profile which will be applied to any character you login with."u8, 2)
        .RegisterEntry("Player-owned NPCs (minions, mounts) should now correctly synchronize via Mare Synchronos."u8, 1)
        .RegisterEntry("It is possible that non-english character names are now working properly. Please note that this is a side effect and CN/KR clients are still not officially supported."u8, 1)

        .RegisterHighlight("Added profile priority system."u8)
        .RegisterEntry("When several active profiles affect the same character, profile priority will be used to determine which profile will be applied to said character."u8, 1)

        .RegisterEntry("Added additional options to configure how Customize+ window behaves."u8)
        .RegisterEntry("Added option to configure if Customize+ windows will be hidden when you hide game UI or not."u8, 1)
        .RegisterEntry("Added option to configure if Customize+ windows will be hidden when you enter GPose or not."u8, 1)
        .RegisterEntry("Added option to configure if Customize+ main window will be automatically opened when you launch the game or not."u8, 1)

        .RegisterImportant("Added warning for custom skeleton bones. If you have custom skeleton installed - read it. Seriously. It's a wrench icon near the name of those bones."u8)
        .RegisterEntry("Added several warnings when testing build of Customize+ is being used."u8)

        .RegisterHighlight("Fixed issue when Customize+ did not detect changes in character skeleton. This mostly happened when altering character appearance via Glamourer and other plugins/tools."u8)

        .RegisterEntry("Dropped support for upgrading from Customize+ 1.0. Clipboard copies are not affected by this change."u8)

        .RegisterEntry("IPC notes, developers only."u8)
        .RegisterImportant("IPC version is now 6.0."u8, 1)
        .RegisterEntry("Profile.GetList has been updated to include profile priority as well as list of characters with their metadata. Please refer to Customize+ IPC source code files for additional information."u8, 1)
        .RegisterEntry("Profile.OnUpdate event is now being triggered for profiles with \"Apply to all players and retainers\" and \"Apply to any character you are logged in with\" options enabled."u8, 1)
        .RegisterEntry("Format of the profile json expected by Profile.SetTemporaryProfileOnCharacter has been updated."u8, 1)
        .RegisterEntry("CharacterName field removed."u8, 2)
        .RegisterEntry("Added few fields reserved for the future functionality."u8, 2)
        .RegisterEntry("Temporary profiles should now apply correctly to owned characters like minions."u8, 1)

        .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_6_3(Changelog log)
        => log.NextVersion("Version 2.0.6.3"u8)
            .RegisterEntry("Added new IPC methods: GameState.GetCutsceneParentIndex, GameState.SetCutsceneParentIndex."u8)
            .RegisterImportant("Those methods were requested by Ktisis developer. Other developers are advised to not use them unless absolutely sure what they are doing."u8, 1)
            .RegisterEntry("Improved support logs. (2.0.6.2)"u8)
            .RegisterEntry("Tweaked logging a bit to be less spammy in \"Debug+\" mode."u8)
            .RegisterEntry("Made Character Select Screen handling more reliable. (2.0.6.1, 2.0.6.3)"u8)
            .RegisterEntry("Fixed incorrect handling of GPose actors."u8)
            .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_6_0(Changelog log)
        => log.NextVersion("Version 2.0.6.0"u8)
        .RegisterHighlight("IPC has been re-enabled."u8)
        .RegisterImportant("If you are regular user you have to wait until other plugins implement necessary changes. Please ask developers of those plugins for further information."u8, 1)
        .RegisterImportant("Breaking change: IPC version has been bumped to 5.0"u8, 1)
        .RegisterImportant("Breaking change: All functions now operate using object table indices. This has been made in order to be more in line with how this is being handled by other major plugins and to try to minimize the chances of being affected by broken things in Dalamud again."u8, 1)
        .RegisterHighlight("Dawntrail facial bones have been categorized. Contribution by Kaze. (2.0.5.1)"u8)
        .RegisterEntry("Renamed all mentions of IVCS to \"IVCS Compatible\" to reflect that it is now possible to use alternative IVCS-compatible skeletons for IVCS mods."u8)
        .RegisterEntry("Fixed negative values not working with Root bone."u8)
        .RegisterEntry("Fixed issues caused by opening Adventurer Plate window."u8);

    private static void Add2_0_5_0(Changelog log)
        => log.NextVersion("Version 2.0.5.0"u8)
        .RegisterHighlight("Customize+ has been updated to support Dawntrail."u8)
        .RegisterImportant("If you edited any facial bones it is possible that you will have to make adjustments to your edits."u8, 1)
        .RegisterImportant("Known issues:"u8, 1)
        .RegisterImportant("Profiles are not applied on Character Select Screen."u8, 2)
        .RegisterImportant("All new Dawntrail bones are placed into the \"Unknown\" category."u8, 2)
        .RegisterImportant("IPC needs additional work and has been disabled for now. Expect issues if you decide to call it anyway."u8, 2)
        .RegisterEntry("Added \"Copy Support Info to Clipboard\" button to Settings tab."u8)
        .RegisterEntry("Renamed \"Default profile\" to \"Apply to all players and retainers\" to try and improve understanding of the function by the users. (2.0.4.5)"u8)
        .RegisterEntry("Improved UI behavior when \"Apply to all players and retainers\" is enabled. (2.0.4.5)"u8);

    private static void Add2_0_4_4(Changelog log)
        => log.NextVersion("Version 2.0.4.4"u8)
        .RegisterHighlight("Added edit button to the template selector in the profile editor which allows to quickly begin editing associated template."u8)
        .RegisterEntry("Fixed \"Limit to my creatures\" setting not working correctly. (2.0.4.2)"u8)
        .RegisterEntry("Added additional logging. (2.0.4.2)"u8);

    private static void Add2_0_4_1(Changelog log)
        => log.NextVersion("Version 2.0.4.1"u8)
            .RegisterEntry("Added support for new worlds."u8)
            .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_4_0(Changelog log)
        => log.NextVersion("Version 2.0.4.0"u8)
            .RegisterImportant("Version 3 IPC has been removed, any plugins still relying on it will stop working until updated."u8)
            .RegisterEntry("Mare Synchronos and Dynamic Bridge are not affected."u8, 1)
            .RegisterEntry("Added option to configure if profiles should be applied on character select screen during login."u8)
            .RegisterEntry("Made information level plugin logs less verbose."u8)
            .RegisterEntry("Source code maintenance - external libraries update."u8);

    private static void Add2_0_3_0(Changelog log)
        => log.NextVersion("Version 2.0.3.0"u8)
            .RegisterEntry("Added option to configure if profiles should affect various parts of game's user interface:"u8)
            .RegisterEntry("Character window"u8, 1)
            .RegisterEntry("Try-On, Dye Preview, Glamour Plate windows"u8, 1)
            .RegisterEntry("Adventurer Cards (Portraits)"u8, 1)
            .RegisterEntry("Inspect window"u8, 1)
            .RegisterEntry("Added option to configure if template editor preview character should be automatically set to current character when you login. This is disabled by default."u8)
            .RegisterEntry("Enabled profiles can no longer be set as default profile."u8)
            .RegisterEntry("Fixed current player character's profile applying to special actors (portraits, etc) of other characters."u8)
            .RegisterEntry("Fixed temporary profile being removed when closing inspection window of character with active temporary profile."u8)
            .RegisterEntry("Fixed profile not applying if it was enabled shortly after doing penumbra redraw."u8)
            .RegisterEntry("Fixed issue when switching to a different profile did not reflect on special actors (portraits, etc)."u8)
            .RegisterEntry("Fixed legacy IPC's RevertCharacter method leaking exceptions. (2.0.2.4)"u8)
            .RegisterEntry("Source code maintenance - external libraries update, refactoring, cleanup."u8);

    private static void Add2_0_2_2(Changelog log)
        => log.NextVersion("Version 2.0.2.2"u8)
            .RegisterHighlight("Added brand new IPC (version 4) for cross-plugin interraction. (2.0.2.0)"u8)
            .RegisterEntry("Please refer to repository readme on GitHub for information about using it."u8, 1)
            .RegisterImportant("Old IPC (version 3) is still available, but it will be removed sometime before Dawntrail release. Plugin developers are advised to migrate as soon as possible."u8, 1)
            .RegisterEntry("Updated to .NET 8. (2.0.2.0)"u8)
            .RegisterEntry("Updated external libraries. (2.0.2.1)"u8)
            .RegisterEntry("Added additional cleanup of user input. (2.0.2.0)"u8)
            .RegisterEntry("Selected default profile can no longer be changed if profile set as default is enabled. (2.0.2.1)"u8)
            .RegisterEntry("Profiles can no longer be enabled/disabled while editor is active. (2.0.2.1)"u8)
            .RegisterEntry("Fixed incorrect warning message priorities in main window. (2.0.2.1)"u8)
            .RegisterEntry("Fixed \"Limit to my creatures\" not ignoring objects other than summons, minions and mounts. (2.0.2.1)"u8)
            .RegisterEntry("Fixed text in various places. (2.0.2.1)"u8);

    private static void Add2_0_1_0(Changelog log)
        => log.NextVersion("Version 2.0.1.0"u8)
            .RegisterHighlight("Added support for legacy clipboard copies."u8)
            .RegisterEntry("Added setting allowing disabling of confirmation messages for chat commands."u8)
            .RegisterEntry("Template and profile editing is no longer disabled during GPose."u8)
            .RegisterImportant("Customize+ is not 100% compatible with posing tools such as Ktisis, Brio and Anamnesis. Some features of those tools might alter Customize+ behavior or prevent it from working."u8, 1)
            .RegisterHighlight("Fixed crash during \"Duty Complete\" cutscenes."u8)
            .RegisterEntry("Fixed settings migration failing completely if one of the profiles is corrupted."u8)
            .RegisterEntry("Improved error handling."u8)
            .RegisterHighlight("Customize+ window will now display warning message if plugin encounters a critical error."u8, 1);

    private static void Add2_0_0_0(Changelog log)
        => log.NextVersion("Version 2.0.0.0"u8)
            .RegisterHighlight("Major rework of the entire plugin."u8)
            .RegisterEntry("Settings and profiles from previous version will be automatically converted to new format on the first load."u8, 1)
            .RegisterImportant("Old version configuration is backed up in case something goes wrong, please report any issues with configuration migration as soon as possible."u8, 2)
            .RegisterImportant("Clipboard copies from previous versions are not currently supported."u8, 2)
            .RegisterImportant("Profiles from previous versions will only be loaded during first load."u8, 2)

            .RegisterHighlight("Major changes:"u8)

            .RegisterEntry("Plugin has been almost completely rewritten from scratch."u8, 1)

            .RegisterEntry("User interface has been moved to the framework used by Glamourer and Penumbra, so the interface should feel familiar to the users of those plugins."u8, 1)
            .RegisterEntry("User interface issues related to different resolutions and font sizes should *mostly* not occur anymore."u8, 2)
            .RegisterImportant("There are several issues with text not fitting in some places depending on your screen resolution and font size. This will be fixed later."u8, 3)

            .RegisterEntry("Template system has been added"u8, 1)
            .RegisterEntry("All bone edits are now stored in templates which can be used by multiple profiles and single profile can reference unlimited number of templates."u8, 2)

            .RegisterImportant("Chat commands have been changed, refer to \"/customize help\" for information about available commands."u8, 1)

            .RegisterEntry("Profiles can be applied to summons, mounts and pets without any limitations."u8, 1)
            .RegisterImportant("Root scaling of mounts is not available for now."u8, 2)

            .RegisterEntry("Fixed \"Only owned\" not working properly in various cases and renamed it to \"Limit to my creatures\"."u8, 1)

            .RegisterEntry("Fixed profiles \"leaking\" to other characters due to the way original Mare Synchronos integration implementation was handled."u8, 1)

            .RegisterEntry("Compatibility with cutscenes is improved, but that was not extensively tested."u8, 1)

            .RegisterEntry("Plugin configuration is now being regularly backed up, the backup is located in %appdata%\\XIVLauncher\\backups\\CustomizePlus folder"u8, 1);
}
