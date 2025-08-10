using OtterGui.Classes;
using OtterGui.Log;

namespace CustomizePlusPlus.Core.Services;

/// <summary>
/// Any file type that we want to save via SaveService.
/// </summary>
public interface ISavable : ISavable<FilenameService>
{ }

public sealed class SaveService : SaveServiceBase<FilenameService>
{
    public SaveService(Logger logger, FrameworkManager framework, FilenameService fileNames)
        : base(logger, framework, fileNames)
    { }
}
