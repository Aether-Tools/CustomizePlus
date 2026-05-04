using CustomizePlus.Configuration.Data;
using Dalamud.Interface.Utility;
using LunaWindow = Luna.Window;
using WindowSizeConstraints = Dalamud.Interface.Windowing.WindowSizeConstraints;

namespace CustomizePlus.UI.Windows;

public partial class PopupSystem : LunaWindow
{
    private const WindowFlags PopupWindowFlags = WindowFlags.NoResize | WindowFlags.NoMove | WindowFlags.NoSavedSettings;

    private readonly Logger _logger;
    private readonly PluginConfiguration _configuration;

    private readonly Dictionary<string, PopupData> _popups = new();
    private readonly List<PopupData> _displayedPopups = new();

    public PopupSystem(Logger logger, PluginConfiguration configuration)
        : base("Customize+ Popups###CPlusPopupSystem",
            WindowFlags.NoCollapse
          | WindowFlags.NoDecoration
          | WindowFlags.NoBackground
          | WindowFlags.NoMove
          | WindowFlags.NoInputs
          | WindowFlags.NoNavFocus
          | WindowFlags.NoFocusOnAppearing
          | WindowFlags.NoBringToFrontOnFocus
          | WindowFlags.NoDocking
          | WindowFlags.NoTitleBar,
            true)
    {
        _logger = logger;
        _configuration = configuration;

        IsOpen = true;
        RespectCloseHotkey = false;
        Collapsed = false;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = Vector2.Zero,
            MaximumSize = Vector2.Zero,
        };

        RegisterMessages();
    }

    public void RegisterPopup(string name, string title, string text, bool displayOnce = false, Vector2? sizeDividers = null)
    {
        name = name.ToLowerInvariant();

        if (_popups.ContainsKey(name))
            throw new Exception($"Popup \"{name}\" is already registered");

        _popups[name] = new PopupData
        {
            Name = name,
            Title = title,
            Text = text,
            DisplayOnce = displayOnce,
            SizeDividers = sizeDividers
        };

        _logger.Debug($"Popup \"{name}\" registered");
    }

    /// <summary> show popup if it is registered and not already queued or displayed </summary>
    public bool ShowPopup(string name)
    {
        name = name.ToLowerInvariant();

        if (!_popups.ContainsKey(name))
            throw new Exception($"Popup \"{name}\" is not registered");

        var popup = _popups[name];

        if (popup.DisplayRequested || _displayedPopups.Contains(popup) || (popup.DisplayOnce && _configuration.UISettings.ViewedMessageWindows.Contains(name)))
            return false;

        popup.DisplayRequested = true;

        //_logger.Debug($"Popup \"{name}\" set as requested to be displayed");

        return true;
    }

    public override void PreOpenCheck()
        => IsOpen = true;

    public override void Draw()
    {
        var viewport = Im.Viewport.Main;

        foreach (var popup in _popups.Values)
        {
            if (popup.DisplayRequested && !_displayedPopups.Contains(popup))
                _displayedPopups.Add(popup);
        }

        if (_displayedPopups.Count == 0)
            return;

        for (var i = 0; i < _displayedPopups.Count; ++i)
        {
            var popup = _displayedPopups[i];
            if (popup.DisplayRequested)
            {
                Im.Popup.Open(popup.ImGuiLabel);
                popup.DisplayRequested = false;
            }

            var xDiv = popup.SizeDividers?.X ?? 5;
            var yDiv = popup.SizeDividers?.Y ?? 8;
            var minWidth = 360 * ImGuiHelpers.GlobalScale;
            var minHeight = 150 * ImGuiHelpers.GlobalScale;
            var size = new Vector2(
                MathF.Max(minWidth, viewport.Size.X / xDiv),
                MathF.Max(minHeight, viewport.Size.Y / yDiv));

            Im.Window.SetNextSize(size, Condition.Appearing);
            Im.Window.SetNextPosition(viewport.Center, Condition.Always, new Vector2(0.5f));
            using var popupWindow = Im.Popup.BeginModal(popup.ImGuiLabel, PopupWindowFlags);
            if (!popupWindow)
            {
                //fixes bug with windows being closed after going into gpose
                Im.Popup.Open(popup.ImGuiLabel);
                continue;
            }

            Im.Cursor.Y += Im.Style.ItemSpacing.Y;
            Im.TextWrapped(popup.Text);
            Im.Line.Spacing();
            Im.Separator();
            Im.Line.Spacing();

            var buttonWidth = new Vector2(150 * ImGuiHelpers.GlobalScale, 0);
            var yPos = Im.Window.Height - Im.Style.FrameHeight - Im.Style.WindowPadding.Y;
            var xPos = (Im.Window.Width - Im.Style.ItemSpacing.X - buttonWidth.X) / 2;
            Im.Cursor.Position = new Vector2(xPos, yPos);
            if (Im.Button("OK"u8, buttonWidth))
            {
                Im.Popup.CloseCurrent();
                _displayedPopups.RemoveAt(i--);

                if (popup.DisplayOnce)
                {
                    _configuration.UISettings.ViewedMessageWindows.Add(popup.Name);
                    _configuration.Save();
                }
            }
        }
    }

    private class PopupData
    {
        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string ImGuiLabel
            => $"{Title}##{Name}";

        public string Text { get; set; } = string.Empty;

        public bool DisplayRequested { get; set; }

        public bool DisplayOnce { get; set; }

        /// <summary>
        /// Divider values used to divide viewport size when setting window size
        /// </summary>
        public Vector2? SizeDividers { get; set; }
    }
}
