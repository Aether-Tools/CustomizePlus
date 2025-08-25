namespace CustomizePlus.Core.Helpers;

//todo: better name
internal static class NameParsingHelper
{
    internal static (string Name, string? Path) ParseName(string name, bool handlePath)
    {
        var actualName = name;
        string? path = null;
        if (handlePath)
        {
            var slashPos = name.LastIndexOf('/');
            if (slashPos >= 0)
            {
                path = name[..slashPos];
                actualName = slashPos >= name.Length - 1 ? "<Unnamed>" : name[(slashPos + 1)..];
            }
        }

        return (actualName, path);
    }
}
