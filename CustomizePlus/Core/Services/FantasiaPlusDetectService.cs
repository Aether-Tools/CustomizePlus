using CustomizePlus.UI.Windows;
using Dalamud.Plugin;
using OtterGui.Log;
using System;
using System.Linq;
using System.Timers;

namespace CustomizePlus.Core.Services;

/// <summary>
/// Detects is Fantasia+ is installed and shows a message if it is. The check is performed every 15 seconds.
/// </summary>
public class FantasiaPlusDetectService : IDisposable
{
    private readonly DalamudPluginInterface _pluginInterface;
    private readonly PopupSystem _popupSystem;
    private readonly Logger _logger;

    private Timer? _checkTimer = null;

    /// <summary>
    /// Note: if this is set to true then this is locked until the plugin or game is restarted
    /// </summary>
    public bool IsFantasiaPlusInstalled { get; private set; }

    public FantasiaPlusDetectService(DalamudPluginInterface pluginInterface, PopupSystem popupSystem, Logger logger)
    {
        _pluginInterface = pluginInterface;
        _popupSystem = popupSystem;
        _logger = logger;

        if (CheckFantasiaPlusPresence())
        {
            _popupSystem.ShowPopup(PopupSystem.Messages.FantasiaPlusDetected);
            _logger.Error("Fantasia+ detected during startup, plugin will be locked");
        }
        else
        {
            _checkTimer = new Timer(15 * 1000);
            _checkTimer.Elapsed += CheckTimerOnElapsed;
            _checkTimer.Start();
        }
    }

    /// <summary>
    /// Returns true if Fantasia+ is installed and loaded
    /// </summary>
    /// <returns></returns>
    private bool CheckFantasiaPlusPresence()
    {
        if (IsFantasiaPlusInstalled)
            return true;

        IsFantasiaPlusInstalled = _pluginInterface.InstalledPlugins.Any(pluginInfo => pluginInfo is { InternalName: "FantasiaPlus", IsLoaded: true });

        return IsFantasiaPlusInstalled;
    }

    private void CheckTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (CheckFantasiaPlusPresence())
        {
            _popupSystem.ShowPopup(PopupSystem.Messages.FantasiaPlusDetected);
            _checkTimer!.Stop();
            _checkTimer?.Dispose();
            _logger.Error("Fantasia+ detected by timer, plugin will be locked");
        }
    }

    public void Dispose()
    {
        if (_checkTimer != null)
            _checkTimer.Dispose();
    }
}
