using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using Penumbra.GameData.Interop;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Profiles;

public sealed class ProfileHeader : SplitButtonHeader, IDisposable
{
    private readonly ProfileFileSystem _fileSystem;
    private readonly ProfileChanged _profileChanged;
    private readonly PluginConfiguration _config;

    private StringU8 _header = new("No Selection"u8);
    private StringU8 _incognito = new("No Selection"u8);

    public ProfileHeader(
        ProfileFileSystem fileSystem,
        IncognitoButton incognito,
        ProfileChanged profileChanged,
        PluginConfiguration config,
        ProfileManager manager,
        ActorObjectManager objects,
        PopupSystem popupSystem)
    {
        _fileSystem = fileSystem;
        _profileChanged = profileChanged;
        _config = config;

        RightButtons.AddButton(incognito, 50);
        RightButtons.AddButton(new LockedButton(fileSystem, manager), 100);
        _fileSystem.Selection.Changed += OnSelectionChanged;
        OnSelectionChanged();
        _profileChanged.Subscribe(OnProfileChanged, ProfileChanged.Priority.DesignHeader);
    }

    private void OnProfileChanged(in ProfileChanged.Arguments arguments)
    {
        if (arguments.Type is not ProfileChanged.Type.Renamed)
            return;

        if (arguments.Profile != _fileSystem.Selection.Selection?.Value)
            return;

        _header = new StringU8(arguments.Profile.Name);
    }

    private void OnSelectionChanged()
    {
        if (_fileSystem.Selection.Selection?.GetValue<Template>() is { } selection)
        {
            _header = new StringU8(selection.Name);
            _incognito = new StringU8(selection.Incognito);
        }
        else if (_fileSystem.Selection.OrderedNodes.Count > 0)
        {
            _header = new StringU8($"{_fileSystem.Selection.OrderedNodes.Count} Objects Selected");
            _incognito = _header;
        }
        else
        {
            _header = new StringU8("No Selection"u8);
            _incognito = _header;
        }
    }

    public override void Draw(Vector2 size)
    {
        var color = ColorId.HeaderButtons.Value();
        using var _ = ImGuiColor.Text.Push(color).Push(ImGuiColor.Border, color);
        base.Draw(size with { Y = Im.Style.FrameHeight });
    }

    public override ReadOnlySpan<byte> Text
        => _config.UISettings.IncognitoMode ? _incognito : _header;

    private sealed class LockedButton(ProfileFileSystem fileSystem, ProfileManager manager) : BaseIconButton<AwesomeIcon>
    {
        public override bool IsVisible
            => fileSystem.Selection.Selection is not null;

        public override AwesomeIcon Icon
            => ((Profile)fileSystem.Selection.Selection!.Value).IsWriteProtected ? LunaStyle.LockedIcon : LunaStyle.UnlockedIcon;

        public override bool HasTooltip
            => true;

        public override void DrawTooltip()
            => Im.Text(((Profile)fileSystem.Selection.Selection!.Value).IsWriteProtected
                ? "Make this profile editable."u8
                : "Write-protect this profile."u8);

        public override void OnClick()
            => manager.SetWriteProtection((Profile)fileSystem.Selection.Selection!.Value,
                !((Profile)fileSystem.Selection.Selection!.Value).IsWriteProtected);
    }

    public void Dispose()
    {
        _fileSystem.Selection.Changed -= OnSelectionChanged;
        _profileChanged.Unsubscribe(OnProfileChanged);
    }
}

