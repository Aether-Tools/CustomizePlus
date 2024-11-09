using Dalamud.Interface.Utility;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.UI;
internal class UiHelpers
{
    /// <summary> Vertical spacing between groups. </summary>
    public static Vector2 DefaultSpace;

    /// <summary> Multiples of the current Global Scale </summary>
    public static float Scale;

    /// <summary> Draw default vertical space. </summary>
    public static void DefaultLineSpace()
        => ImGui.Dummy(DefaultSpace);

    public static void SetupCommonSizes()
    {
        if (ImGuiHelpers.GlobalScale != Scale)
        {
            Scale = ImGuiHelpers.GlobalScale;
            DefaultSpace = new Vector2(0, 10 * Scale);
        }
    }
}
