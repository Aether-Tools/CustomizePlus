using CustomizePlus.Templates.Data;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Templates.Controls;

public sealed class RenameTemplateInput(TemplateFileSystemDrawer fileSystem) : BaseButton<IFileSystemData>
{
    /// <inheritdoc/>
    public override ReadOnlySpan<byte> Label(in IFileSystemData _)
        => "##Rename"u8;

    /// <summary> Replaces the normal menu item handling for a text input, so the other fields are not used. </summary>
    /// <inheritdoc/>
    public override bool DrawMenuItem(in IFileSystemData data)
    {
        var template = (Template)data.Value;
        var currentName = template.Name;
        using var style = Im.Style.PushDefault(ImStyleDouble.FramePadding);
        MenuSeparator.DrawSeparator();
        Im.Text("Rename Template:"u8);
        if (Im.Window.Appearing)
            Im.Keyboard.SetFocusHere();
        var ret = Im.Input.Text(Label(data), ref currentName, flags: InputTextFlags.EnterReturnsTrue);
        Im.Tooltip.OnHover("Enter a new name here to rename the changed template."u8);
        if (!ret)
            return false;

        fileSystem.TemplateManager.Rename(template, currentName);
        Im.Popup.CloseCurrent();

        return ret;
    }
}
