using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace CustomizePlus.Game.Services;

public class ChatService
{
    private readonly IChatGui _chatGui;

    public ChatService(IChatGui chatGui)
    {
        _chatGui = chatGui;
    }

    public void PrintInChat(string message, ChatMessageColor color = ChatMessageColor.Info)
    {
        var stringBuilder = new SeStringBuilder();
        stringBuilder.AddUiForeground((ushort)color);
        stringBuilder.AddText($"[Customize+] {message}");
        stringBuilder.AddUiForegroundOff();
        _chatGui.Print(stringBuilder.BuiltString);
    }

    public void PrintInChat(SeString seString)
    {
        _chatGui.Print(seString);
    }

    public enum ChatMessageColor : ushort
    {
        Info = 45,
        Warning = 500,
        Error = 14
    }
}