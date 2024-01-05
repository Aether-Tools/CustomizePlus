using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using CustomizePlus.Configuration.Data;
using CustomizePlus.UI.Windows.MainWindow;
using CustomizePlus.UI.Windows;

namespace CustomizePlus.UI;

public class CPlusWindowSystem : IDisposable
{
    private readonly WindowSystem _windowSystem = new("Customize+");
    private readonly UiBuilder _uiBuilder;
    private readonly MainWindow _mainWindow;
    private readonly PopupSystem _popupSystem;

    public CPlusWindowSystem(
        UiBuilder uiBuilder,
        MainWindow mainWindow,
        CPlusChangeLog changelog,
        PopupSystem popupSystem,
        PluginConfiguration configuration)
    {
        _uiBuilder = uiBuilder;
        _mainWindow = mainWindow;
        _popupSystem = popupSystem;

        _windowSystem.AddWindow(mainWindow);
        _windowSystem.AddWindow(changelog.Changelog);
        _uiBuilder.Draw += OnDraw;
        _uiBuilder.OpenConfigUi += _mainWindow.Toggle;

        _uiBuilder.DisableGposeUiHide = true;
        _uiBuilder.DisableCutsceneUiHide = !configuration.UISettings.HideWindowInCutscene;
    }

    private void OnDraw()
    {
        _windowSystem.Draw();
        _popupSystem.Draw();
    }

    public void Dispose()
    {
        _uiBuilder.Draw -= _windowSystem.Draw;
        _uiBuilder.OpenConfigUi -= _mainWindow.Toggle;
    }
}
