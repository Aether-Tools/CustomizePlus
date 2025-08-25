﻿using CustomizePlus.Configuration.Data;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.FileSystem.Selector;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGui.Text;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using static CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles.ProfileFileSystemSelector;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public class ProfileFileSystemSelector : FileSystemSelector<Profile, ProfileState>
{
    private readonly PluginConfiguration _configuration;
    private readonly ProfileManager _profileManager;
    private readonly ProfileChanged _event;
    private readonly GameObjectService _gameObjectService;
    private readonly IClientState _clientState;

    private Profile? _cloneProfile;
    private string _newName = string.Empty;

    public bool IncognitoMode
    {
        get => _configuration.UISettings.IncognitoMode;
        set
        {
            _configuration.UISettings.IncognitoMode = value;
            _configuration.Save();
        }
    }

    public struct ProfileState
    {
        public ColorId Color;
    }

    protected override float CurrentWidth
		=> _configuration.UISettings.CurrentProfileSelectorWidth * ImUtf8.GlobalScale;

    protected override float MinimumAbsoluteRemainder
        => 470 * ImUtf8.GlobalScale;

    protected override float MinimumScaling
        => _configuration.UISettings.ProfileSelectorMinimumScale;

    protected override float MaximumScaling
        => _configuration.UISettings.ProfileSelectorMaximumScale;

    protected override void SetSize(Vector2 size)
    {
        base.SetSize(size);
        var adaptedSize = MathF.Round(size.X / ImUtf8.GlobalScale);
        if (adaptedSize == _configuration.UISettings.CurrentProfileSelectorWidth)
            return;

        _configuration.UISettings.CurrentProfileSelectorWidth = adaptedSize;
        _configuration.Save();
    }


    public ProfileFileSystemSelector(
        ProfileFileSystem fileSystem,
        IKeyState keyState,
        Logger logger,
        PluginConfiguration configuration,
        ProfileManager profileManager,
        ProfileChanged @event,
        GameObjectService gameObjectService,
        IClientState clientState)
        : base(fileSystem, keyState, logger, allowMultipleSelection: true)
    {
        _configuration = configuration;
        _profileManager = profileManager;
        _event = @event;
        _gameObjectService = gameObjectService;
        _clientState = clientState;

        _event.Subscribe(OnProfileChange, ProfileChanged.Priority.ProfileFileSystemSelector);

        _clientState.Login += OnLogin;
        _clientState.Logout += OnLogout;

        AddButton(NewButton, 0);
        AddButton(CloneButton, 20);
        AddButton(DeleteButton, 1000);
        SetFilterTooltip();
    }

    public void Dispose()
    {
        base.Dispose();
        _event.Unsubscribe(OnProfileChange);
        _clientState.Login -= OnLogin;
        _clientState.Logout -= OnLogout;
    }

    protected override uint ExpandedFolderColor
        => ColorId.FolderExpanded.Value();

    protected override uint CollapsedFolderColor
        => ColorId.FolderCollapsed.Value();

    protected override uint FolderLineColor
        => ColorId.FolderLine.Value();

    protected override bool FoldersDefaultOpen
        => _configuration.UISettings.FoldersDefaultOpen;

    protected override void DrawLeafName(FileSystem<Profile>.Leaf leaf, in ProfileState state, bool selected)
    {
        var flag = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
        var name = IncognitoMode ? leaf.Value.Incognito : leaf.Value.Name.Text;
        using var color = ImRaii.PushColor(ImGuiCol.Text, state.Color.Value());
        using var _ = ImUtf8.TreeNode(name, flag);
    }

    protected override void DrawPopups()
    {
        DrawNewProfilePopup();
    }

    private void DrawNewProfilePopup()
    {
        if (!ImGuiUtil.OpenNameField("##NewProfile", ref _newName))
            return;

        if (_cloneProfile != null)
        {
            _profileManager.Clone(_cloneProfile, _newName, true);
            _cloneProfile = null;
        }
        else
        {
            _profileManager.Create(_newName, true);
        }

        _newName = string.Empty;
    }

    private void OnProfileChange(ProfileChanged.Type type, Profile? profile, object? arg3 = null)
    {
        switch (type)
        {
            case ProfileChanged.Type.Created:
            case ProfileChanged.Type.Deleted:
            case ProfileChanged.Type.Renamed:
            case ProfileChanged.Type.Toggled:
            case ProfileChanged.Type.AddedCharacter:
            case ProfileChanged.Type.RemovedCharacter:
            case ProfileChanged.Type.ReloadedAll:
                SetFilterDirty();
                break;
        }
    }

    private void OnLogin()
    {
        SetFilterDirty();
    }

    private void OnLogout(int type, int code)
    {
        SetFilterDirty();
    }

    private void NewButton(Vector2 size)
    {
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), size, "Create a new profile with default configuration.", false,
                true))
            return;

        ImGui.OpenPopup("##NewProfile");
    }

    private void CloneButton(Vector2 size)
    {
        var tt = SelectedLeaf == null
            ? "No profile selected."
            : "Clone the currently selected profile to a duplicate";
        if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Clone.ToIconString(), size, tt, SelectedLeaf == null, true))
            return;

        _cloneProfile = Selected!;
        ImGui.OpenPopup("##NewProfile");
    }

    private void DeleteButton(Vector2 size)
        => DeleteSelectionButton(size, _configuration.UISettings.DeleteTemplateModifier, "profile", "profiles", _profileManager.Delete);

    #region Filters

    private const StringComparison IgnoreCase = StringComparison.OrdinalIgnoreCase;
    private LowerString _filter = LowerString.Empty;
    private int _filterType = -1;

    private void SetFilterTooltip()
    {
        FilterTooltip = "Filter profiles for those where their full paths or names contain the given substring.\n"
          + "Enter n:[string] to filter only for profile names and no paths.";
    }

    /// <summary> Appropriately identify and set the string filter and its type. </summary>
    protected override bool ChangeFilter(string filterValue)
    {
        (_filter, _filterType) = filterValue.Length switch
        {
            0 => (LowerString.Empty, -1),
            > 1 when filterValue[1] == ':' =>
                filterValue[0] switch
                {
                    'n' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 1),
                    'N' => filterValue.Length == 2 ? (LowerString.Empty, -1) : (new LowerString(filterValue[2..]), 1),
                    _ => (new LowerString(filterValue), 0),
                },
            _ => (new LowerString(filterValue), 0),
        };

        return true;
    }

    /// <summary>
    /// The overwritten filter method also computes the state.
    /// Folders have default state and are filtered out on the direct string instead of the other options.
    /// If any filter is set, they should be hidden by default unless their children are visible,
    /// or they contain the path search string.
    /// </summary>
    protected override bool ApplyFiltersAndState(FileSystem<Profile>.IPath path, out ProfileState state)
    {
        if (path is ProfileFileSystem.Folder f)
        {
            state = default;
            return FilterValue.Length > 0 && !f.FullName().Contains(FilterValue, IgnoreCase);
        }

        return ApplyFiltersAndState((ProfileFileSystem.Leaf)path, out state);
    }

    /// <summary> Apply the string filters. </summary>
    private bool ApplyStringFilters(ProfileFileSystem.Leaf leaf, Profile profile)
    {
        return _filterType switch
        {
            -1 => false,
            0 => !(_filter.IsContained(leaf.FullName()) || profile.Name.Contains(_filter)),
            1 => !profile.Name.Contains(_filter),
            _ => false, // Should never happen
        };
    }

    /// <summary> Combined wrapper for handling all filters and setting state. </summary>
    private bool ApplyFiltersAndState(ProfileFileSystem.Leaf leaf, out ProfileState state)
    {
        state = default;

        if (leaf == null || leaf.Value == null)
            return true;

        //Do not display temporary profiles;
        if (leaf.Value.IsTemporary)
        {
            state.Color = ColorId.DisabledProfile;
            return false;
        }

        //todo: priority check
        var identifier = _gameObjectService.GetCurrentPlayerActorIdentifier();
        if (leaf.Value.Enabled)
            state.Color = leaf.Value.Characters.Any(x => x.MatchesIgnoringOwnership(identifier)) ? ColorId.LocalCharacterEnabledProfile : ColorId.EnabledProfile;
        else
            state.Color = leaf.Value.Characters.Any(x => x.MatchesIgnoringOwnership(identifier)) ? ColorId.LocalCharacterDisabledProfile : ColorId.DisabledProfile;

        //todo: missing actor color

        return ApplyStringFilters(leaf, leaf.Value);
    }

    #endregion
}
