using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;
using OtterGui;
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
    protected float InnerWidth;

    protected TemplateComboBase(
        Func<IReadOnlyList<Tuple<Template, string>>> generator,
        Logger logger,
        TemplateChanged templateChanged,
        //TabSelected tabSelected,
        PluginConfiguration configuration)
        : base(generator, logger)
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
        var ret = base.DrawSelectable(globalIdx, selected);
        var (design, path) = Items[globalIdx];
        if (path.Length > 0 && design.Name != path)
        {
            var start = ImGui.GetItemRectMin();
            var pos = start.X + ImGui.CalcTextSize(design.Name).X;
            var maxSize = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
            var remainingSpace = maxSize - pos;
            var requiredSize = ImGui.CalcTextSize(path).X + ImGui.GetStyle().ItemInnerSpacing.X;
            var offset = remainingSpace - requiredSize;
            if (ImGui.GetScrollMaxY() == 0)
                offset -= ImGui.GetStyle().ItemInnerSpacing.X;

            if (offset < ImGui.GetStyle().ItemSpacing.X)
                ImGuiUtil.HoverTooltip(path);
            else
                ImGui.GetWindowDrawList().AddText(start with { X = pos + offset },
                    ImGui.GetColorU32(ImGuiCol.TextDisabled), path);
        }

        return ret;
    }

    protected bool Draw(Template? currentTemplate, string? label, float width)
    {
        InnerWidth = 400 * ImGuiHelpers.GlobalScale;
        CurrentSelectionIdx = Math.Max(Items.IndexOf(p => currentTemplate == p.Item1), 0);
        CurrentSelection = Items[CurrentSelectionIdx];
        var name = label ?? "Select Template Here...";
        var ret = Draw("##template", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection != null;

        return ret;
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
        switch (type)
        {
            case TemplateChanged.Type.Created:
            case TemplateChanged.Type.Renamed:
                Cleanup();
                break;
            case TemplateChanged.Type.Deleted:
                Cleanup();
                if (CurrentSelection?.Item1 == template)
                {
                    CurrentSelectionIdx = -1;
                    CurrentSelection = null;
                }

                break;
        }
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
                .Select(d => new Tuple<Template, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
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
