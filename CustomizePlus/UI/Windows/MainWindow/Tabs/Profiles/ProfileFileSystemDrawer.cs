using CustomizePlus.Anamnesis;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Game.Services;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Events;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.Controls;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;
using CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public sealed class ProfileFileSystemDrawer : FileSystemDrawer<ProfileFileSystemCache.ProfileData>, IDisposable
{
    internal readonly ProfileChanged ProfileChanged;
    internal readonly ProfileManager ProfileManager;
    internal readonly PluginConfiguration Configuration;
    internal readonly IClientState ClientState;
    internal readonly ColorsService ColorsService;

    public ProfileFileSystemDrawer(MessageService messager,
        ProfileFileSystem fileSystem,
        ProfileChanged profileChanged,
        ProfileManager profileManager,
        PluginConfiguration configuration,
        IClientState clientState,
        ColorsService colorsService)
        : base(messager, fileSystem, new ProfileFilter(configuration))
    {
        ProfileChanged = profileChanged;
        ProfileManager = profileManager;
        Configuration = configuration;
        ClientState = clientState;
        ColorsService = colorsService;

        Footer.Buttons.AddButton(new NewProfileButton(profileManager), 1000);
        Footer.Buttons.AddButton(new DuplicateProfileButton(fileSystem, profileManager), 700);
        Footer.Buttons.AddButton(new DeleteProfileButton(fileSystem, profileManager, configuration), -100);

        DataContext.AddButton(new RenameProfileInput(this), -1001);
        DataContext.AddButton(new MoveProfileInput(this), -1000);

        SortMode = configuration.SortMode;
    }

    public void Dispose()
    {

    }

    public override Vector4 ExpandedFolderColor
        => ColorId.FolderExpanded.Value().ToVector();

    public override Vector4 CollapsedFolderColor
        => ColorId.FolderCollapsed.Value().ToVector();

    public override Vector4 FolderLineColor
        => ColorId.FolderLine.Value().ToVector();

    public override IEnumerable<ISortMode> ValidSortModes
        => ISortMode.Valid.Values;

    public override ReadOnlySpan<byte> Id
        => "Templates"u8;

    protected override FileSystemCache<ProfileFileSystemCache.ProfileData> CreateCache()
        => new ProfileFileSystemCache(this);
}
