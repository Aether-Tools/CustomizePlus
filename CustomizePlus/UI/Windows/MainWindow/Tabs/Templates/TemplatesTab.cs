﻿using Dalamud.Interface.Utility;
using ImGuiNET;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates;

public class TemplatesTab
{
    private readonly TemplateFileSystemSelector _selector;
    private readonly TemplatePanel _panel;

    public TemplatesTab(TemplateFileSystemSelector selector, TemplatePanel panel)
    {
        _selector = selector;
        _panel = panel;
    }

    public void Draw()
    {
        _selector.Draw(200f * ImGuiHelpers.GlobalScale);
        ImGui.SameLine();
        _panel.Draw();
    }
}
