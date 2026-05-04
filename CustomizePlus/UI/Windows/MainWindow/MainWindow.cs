using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Events;
using CustomizePlus.UI.Windows.Controls;
using ECommonsLite.Schedulers;
using LunaWindow = Luna.Window;
using WindowSizeConstraints = Dalamud.Interface.Windowing.WindowSizeConstraints;

namespace CustomizePlus.UI.Windows.MainWindow;

public class MainWindow : LunaWindow, IDisposable
{
    private readonly PluginStateBlock _pluginStateBlock;

    private readonly TemplateEditorManager _templateEditorManager;
    private readonly HookingService _hookingService;

    private readonly TemplateEditorEvent _templateEditorEvent;

    private readonly MainTabBar _mainTabBar;

    /// <summary>
    /// Used to force the main window to switch to specific tab
    /// </summary>
    private MainTabType? _switchToTab = null;

    private Action? _actionAfterTabSwitch = null;

    public MainWindow(
        MainTabBar mainTabBar,
        PluginStateBlock pluginStateBlock,
        TemplateEditorManager templateEditorManager,
        PluginConfiguration configuration,
        HookingService hookingService,
        TemplateEditorEvent templateEditorEvent
        ) : base($"Customize+ {VersionHelper.Version}###CPlusMainWindow")
    {
        _mainTabBar = mainTabBar;

        _pluginStateBlock = pluginStateBlock;

        _templateEditorManager = templateEditorManager;
        _hookingService = hookingService;

        _templateEditorEvent = templateEditorEvent;

        _templateEditorEvent.Subscribe(OnTemplateEditorEvent, TemplateEditorEvent.Priority.MainWindow);

        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(700, 675),
            MaximumSize = Im.Viewport.Main.Size,
        };

        IsOpen = configuration.UISettings.OpenWindowAtStart;
    }

    public void Dispose()
    {
        _templateEditorEvent.Unsubscribe(OnTemplateEditorEvent);
    }

    public override void Draw()
    {
        using (var disabled = Im.Disabled(_hookingService.RenderHookFailed || _hookingService.MovementHookFailed))
        {
            LockWindowClosureIfNeeded();

            if (_switchToTab != null)
            {
                _mainTabBar.NextTab = _switchToTab;
                _switchToTab = null;
            }

            _mainTabBar.Draw();

            if (_actionAfterTabSwitch != null)
            {
                _actionAfterTabSwitch();
                _actionAfterTabSwitch = null;
            }
        }

        Im.Line.Same();
        var yPos = Im.Cursor.Position.Y - 5;
        _pluginStateBlock.Draw(yPos, CalculatePluginStateLeftEdge(_mainTabBar.Tabs));
    }

    public void OpenSettings()
    {
        IsOpen = true;
        _switchToTab = MainTabType.Settings;
    }

    private static float CalculatePluginStateLeftEdge(IEnumerable<ITab> tabs)
    {
        var leftEdge = Im.Window.MinimumContentRegion.X;
        foreach (var tab in tabs)
        {
            leftEdge += CalculateTabWidth(tab.Label);
        }

        return leftEdge + Im.Style.ItemSpacing.X;
    }

    private static float CalculateTabWidth(Utf8StringHandler<ImSharp.TextStringHandlerBuffer> label)
        => Im.Font.CalculateSize(label, false).X + (2 * Im.Style.FramePadding.X) + Im.Style.ItemInnerSpacing.X;

    private void LockWindowClosureIfNeeded()
    {
        if (_templateEditorManager.IsEditorActive)
        {
            ShowCloseButton = false;
            RespectCloseHotkey = false;
        }
        else
        {
            ShowCloseButton = true;
            RespectCloseHotkey = true;
        }
    }

    private void OnTemplateEditorEvent(in TemplateEditorEvent.Arguments args)
    {
        var (type, template) = args;
        if (type != TemplateEditorEvent.Type.EditorEnableRequested)
            return;

        if (template == null)
            return;

        if (!template.IsWriteProtected && !_templateEditorManager.IsEditorActive)
        {
            new TickScheduler(() =>
            {
                _switchToTab = MainTabType.Templates;

                //To make sure the tab has switched, ugly but imgui is shit and I don't trust it.
                _actionAfterTabSwitch = () => { _templateEditorEvent.Invoke(new TemplateEditorEvent.Arguments(TemplateEditorEvent.Type.EditorEnableRequestedStage2, template)); };
            });
        }
    }
}

public enum MainTabType
{
    None = -1,
    Settings = 0,
    Templates = 1,
    Profiles = 2,
    IPCTest = 3,
    StateMonitoring = 4,
}
