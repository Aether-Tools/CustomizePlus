using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Log;
using OtterGui.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;
using CustomizePlus.Configuration.Data;

namespace CustomizePlus.UI.Windows;

public partial class PopupSystem
{
    private readonly Logger _logger;
    private readonly PluginConfiguration _configuration;

    private readonly Dictionary<string, PopupData> _popups = new();
    private readonly List<PopupData> _displayedPopups = new();

    public PopupSystem(Logger logger, PluginConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        RegisterMessages();
    }

    public void RegisterPopup(string name, string text, bool displayOnce = false, Vector2? sizeDividers = null)
    {
        name = name.ToLowerInvariant();

        if (_popups.ContainsKey(name))
            throw new Exception($"Popup \"{name}\" is already registered");

        _popups[name] = new PopupData { Name = name, Text = text, DisplayOnce = displayOnce, SizeDividers = sizeDividers };

        _logger.Debug($"Popup \"{name}\" registered");
    }

    /// <summary>
    /// Show popup. Returns false if popup will not be shown for some reason. (can only be shown once or awaiting to be shown)
    /// </summary>
    public bool ShowPopup(string name)
    {
        name = name.ToLowerInvariant();

        if (!_popups.ContainsKey(name))
            throw new Exception($"Popup \"{name}\" is not registered");

        var popup = _popups[name];

        if (popup.DisplayRequested || (popup.DisplayOnce && _configuration.UISettings.ViewedMessageWindows.Contains(name)))
            return false;

        popup.DisplayRequested = true;

        //_logger.Debug($"Popup \"{name}\" set as requested to be displayed");

        return true;
    }

    public void Draw()
    {
        var viewportSize = ImGui.GetWindowViewport().Size;

        foreach (var popup in _popups.Values)
        {
            if (popup.DisplayRequested)
                _displayedPopups.Add(popup);
        }

        if (_displayedPopups.Count == 0)
            return;

        for (var i = 0; i < _displayedPopups.Count; ++i)
        {
            var popup = _displayedPopups[i];
            if (popup.DisplayRequested)
            {
                ImGui.OpenPopup(popup.Name);
                popup.DisplayRequested = false;
            }

            var xDiv = popup.SizeDividers?.X ?? 5;
            var yDiv = popup.SizeDividers?.Y ?? 12;

            ImGui.SetNextWindowSize(new Vector2(viewportSize.X / xDiv, viewportSize.Y / yDiv));
            ImGui.SetNextWindowPos(viewportSize / 2, ImGuiCond.Always, new Vector2(0.5f));
            using var popupWindow = ImRaii.Popup(popup.Name, ImGuiWindowFlags.Modal);
            if (!popupWindow)
            {
                //fixes bug with windows being closed after going into gpose
                ImGui.OpenPopup(popup.Name);
                continue;
            }

            ImGui.SetCursorPos(new Vector2(10, ImGui.GetWindowHeight() / 4));
            ImGuiUtil.TextWrapped(popup.Text);

            var buttonWidth = new Vector2(150 * ImGuiHelpers.GlobalScale, 0);
            var yPos = ImGui.GetWindowHeight() - 2 * ImGui.GetFrameHeight();
            var xPos = (ImGui.GetWindowWidth() - ImGui.GetStyle().ItemSpacing.X - buttonWidth.X) / 2;
            ImGui.SetCursorPos(new Vector2(xPos, yPos));
            if (ImGui.Button("Ok", buttonWidth))
            {
                ImGui.CloseCurrentPopup();
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
        public string Name { get; set; }

        public string Text { get; set; }

        public bool DisplayRequested { get; set; }

        public bool DisplayOnce { get; set; }

        /// <summary>
        /// Divider values used to divide viewport size when setting window size
        /// </summary>
        public Vector2? SizeDividers { get; set; }
    }
}
