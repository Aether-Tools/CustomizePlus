using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;
using OtterGui.Extensions;
using OtterGui;
using OtterGui.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CustomizePlus.Templates;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Events;
using CustomizePlus.Templates.Data;

namespace CustomizePlus.UI.Windows.Controls;

public abstract class TemplateComboBase : FilterComboCache<Tuple<Template, string>>, IDisposable
{
    private readonly PluginConfiguration _configuration;
    private readonly TemplateChanged _templateChanged;
    // protected readonly TabSelected TabSelected;

    private bool _isCurrentSelectionDirty;
    private Template? _currentTemplate;

    protected float InnerWidth;

    protected TemplateComboBase(
        Func<IReadOnlyList<Tuple<Template, string>>> generator,
        Logger logger,
        TemplateChanged templateChanged,
        //TabSelected tabSelected,
        PluginConfiguration configuration)
        : base(generator, MouseWheelType.Control, logger)
    {
        _templateChanged = templateChanged;
        //TabSelected = tabSelected;
        _configuration = configuration;
        _templateChanged.Subscribe(OnTemplateChange, TemplateChanged.Priority.TemplateCombo);
    }

    public bool Incognito
        => _configuration.UISettings.IncognitoMode;

    void IDisposable.Dispose()
        => _templateChanged.Unsubscribe(OnTemplateChange);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (template, path) = Items[globalIdx];
        bool ret;

        using var color = ImRaii.PushColor(ImGuiCol.Text, ColorId.UsedTemplate.Value());
        ret = base.DrawSelectable(globalIdx, selected);
        DrawPath(path, template);

        return ret;
    }

    private static void DrawPath(string path, Template template)
    {
        if (path.Length <= 0 || template.Name == path)
            return;

        DrawRightAligned(template.Name, path, ImGui.GetColorU32(ImGuiCol.TextDisabled));
    }

    protected bool Draw(Template? currentTemplate, string? label, float width)
    {
        _currentTemplate = currentTemplate;
        UpdateCurrentSelection();

        InnerWidth = 400 * ImGuiHelpers.GlobalScale;

        if(Items.Count > 0)
        {
            CurrentSelectionIdx = Math.Max(Items.IndexOf(p => currentTemplate == p.Item1), 0);
            CurrentSelection = Items[CurrentSelectionIdx];
        }

        var name = label ?? "Select Template Here...";
        var ret = Draw("##template", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection != null;

        _currentTemplate = null;

        return ret;
    }

    protected override void OnMouseWheel(string preview, ref int _2, int steps)
    {
        if (!ReferenceEquals(_currentTemplate, CurrentSelection?.Item1))
            CurrentSelectionIdx = -1;

        base.OnMouseWheel(preview, ref _2, steps);
    }

    private void UpdateCurrentSelection()
    {
        if (!_isCurrentSelectionDirty)
            return;

        var priorState = IsInitialized;
        if (priorState)
            Cleanup();
        CurrentSelectionIdx = Items.IndexOf(s => ReferenceEquals(s.Item1, CurrentSelection?.Item1));
        if (CurrentSelectionIdx >= 0)
        {
            UpdateSelection(Items[CurrentSelectionIdx]);
        }
        else if (Items.Count > 0)
        {
            CurrentSelectionIdx = 0;
            UpdateSelection(Items[0]);
        }
        else
        {
            UpdateSelection(null);
        }

        if (!priorState)
            Cleanup();
        _isCurrentSelectionDirty = false;
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        CurrentSelectionIdx = Items.IndexOf(p => _currentTemplate == p.Item1);
        UpdateSelection(CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : null);
        return CurrentSelectionIdx;
    }

    protected override string ToString(Tuple<Template, string> obj)
        => obj.Item1.Name.Text;

    protected override float GetFilterWidth()
        => InnerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || design.Name.Lower.Contains(filter.Lower);
    }

    private void OnTemplateChange(TemplateChanged.Type type, Template template, object? data = null)
    {
        _isCurrentSelectionDirty = type switch
        {
            TemplateChanged.Type.Created => true,
            TemplateChanged.Type.Renamed => true,
            TemplateChanged.Type.Deleted => true,
            _ => _isCurrentSelectionDirty,
        };
    }

    private static void DrawRightAligned(string leftText, string text, uint color)
    {
        var start = ImGui.GetItemRectMin();
        var pos = start.X + ImGui.CalcTextSize(leftText).X;
        var maxSize = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        var remainingSpace = maxSize - pos;
        var requiredSize = ImGui.CalcTextSize(text).X + ImGui.GetStyle().ItemInnerSpacing.X;
        var offset = remainingSpace - requiredSize;
        if (ImGui.GetScrollMaxY() == 0)
            offset -= ImGui.GetStyle().ItemInnerSpacing.X;

        if (offset < ImGui.GetStyle().ItemSpacing.X)
            ImGuiUtil.HoverTooltip(text);
        else
            ImGui.GetWindowDrawList().AddText(start with { X = pos + offset },
                color, text);
    }
}

public sealed class TemplateCombo : TemplateComboBase
{
    private readonly ProfileManager _profileManager;

    public TemplateCombo(
        TemplateManager templateManager,
        ProfileManager profileManager,
        TemplateFileSystem fileSystem,
        Logger logger,
        TemplateChanged templateChanged,
        //TabSelected tabSelected,
        PluginConfiguration configuration)
        : base(
            () => templateManager.Templates
                .Select(d => new Tuple<Template, string>(d, fileSystem.TryGetValue(d, out var l) ? l.FullName() : string.Empty))
                .OrderBy(d => d.Item2)
                .ToList(), logger, templateChanged,/* tabSelected, */configuration)
    {
        _profileManager = profileManager;
    }

    public Template? Template
        => CurrentSelection?.Item1;

    public void Draw(Profile profile, Template? template, int templateIndex)
    {
        if (!Draw(template, Incognito ? template?.Incognito : template?.Name, ImGui.GetContentRegionAvail().X))
            return;

        if (templateIndex >= 0)
            _profileManager.ChangeTemplate(profile, templateIndex, CurrentSelection!.Item1);
        else
            _profileManager.AddTemplate(profile, CurrentSelection!.Item1);
    }
}
