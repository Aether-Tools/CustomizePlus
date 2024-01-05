using System;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;

namespace CustomizePlus.Core.Helpers;

public static class CtrlHelper
{
    /// <summary>
    /// Gets the width of an icon button, checkbox, etc...
    /// </summary>
    /// per https://github.com/ocornut/imgui/issues/3714#issuecomment-759319268
    public static float IconButtonWidth => ImGui.GetFrameHeight() + 2 * ImGui.GetStyle().ItemInnerSpacing.X;

    public static bool TextBox(string label, ref string value)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        return ImGui.InputText(label, ref value, 1024);
    }

    public static bool TextPropertyBox(string label, Func<string> get, Action<string> set)
    {
        var temp = get();
        var result = TextBox(label, ref temp);
        if (result)
        {
            set(temp);
        }

        return result;
    }

    public static bool Checkbox(string label, ref bool value)
    {
        return ImGui.Checkbox(label, ref value);
    }

    public static bool CheckboxWithTextAndHelp(string label, string text, string helpText, ref bool value)
    {
        var checkBoxState = ImGui.Checkbox(label, ref value);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
        ImGui.PopFont();
        AddHoverText(helpText);
        ImGui.SameLine();
        ImGui.Text(text);

        AddHoverText(helpText);

        return checkBoxState;
    }

    public static bool CheckboxToggle(string label, in bool shown, Action<bool> toggle)
    {
        var temp = shown;
        var toggled = ImGui.Checkbox(label, ref temp);

        if (toggled)
        {
            toggle(temp);
        }

        return toggled;
    }

    public static bool ArrowToggle(string label, ref bool value)
    {
        var toggled = ImGui.ArrowButton(label, value ? ImGuiDir.Down : ImGuiDir.Right);

        if (toggled)
        {
            value = !value;
        }

        return value;
    }

    public static void AddHoverText(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }
    }

    public enum TextAlignment { Left, Center, Right };
    public static void StaticLabel(string? text, TextAlignment align = TextAlignment.Left, string tooltip = "")
    {
        if (text != null)
        {
            if (align == TextAlignment.Center)
            {
                ImGui.Dummy(new System.Numerics.Vector2((ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2, 0));
                ImGui.SameLine();
            }
            else if (align == TextAlignment.Right)
            {
                ImGui.Dummy(new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X, 0));
                ImGui.SameLine();
            }

            ImGui.Text(text);
            if (!tooltip.IsNullOrWhitespace())
            {
                AddHoverText(tooltip);
            }
        }
    }

    public static void LabelWithIcon(FontAwesomeIcon icon, string text, bool isSameLine = true)
    {
        if (isSameLine)
            ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(icon.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextWrapped(text);
    }
}