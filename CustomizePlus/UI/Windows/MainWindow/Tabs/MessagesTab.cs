using OtterGui.Classes;

namespace CustomizePlusPlus.UI.Windows.MainWindow.Tabs;

public class MessagesTab
{
    private readonly MessageService _messages;

    public MessagesTab(MessageService messages)
        => _messages = messages;

    public bool IsVisible
        => _messages.Count > 0;

    public void Draw() => _messages.Draw();
}
