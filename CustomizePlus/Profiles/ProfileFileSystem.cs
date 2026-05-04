using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Events;
using Dalamud.Interface.ImGuiNotification;

namespace CustomizePlus.Profiles;

public sealed class ProfileFileSystem : BaseFileSystem, IDisposable
{
    private readonly ProfileChanged _profileChanged;
    private readonly ProfileFileSystemSaver _saver;

    public ProfileFileSystem(
        LunaLogger log,
        SaveService saveService,
        ProfileManager profileManager,
        ProfileChanged profileChanged)
        : base("ProfileFileSystem", log, true)
    {
        _profileChanged = profileChanged;

        _saver = new ProfileFileSystemSaver(log, this, saveService, profileManager);

        _profileChanged.Subscribe(OnProfileChange, ProfileChanged.Priority.ProfileFileSystem);
        _saver.Load();
    }

    public void Dispose()
    {
        _profileChanged.Unsubscribe(OnProfileChange);
        _saver.Dispose();
        Selection.Dispose();
    }

    private void OnProfileChange(in ProfileChanged.Arguments arguments)
    {
        switch (arguments.Type)
        {
            case ProfileChanged.Type.ReloadedAll: _saver.Load(); break;
            case ProfileChanged.Type.Created:
                var parent = Root;
                var folder = arguments.Profile!.Path.Folder;
                if (folder.Length > 0)
                    try
                    {
                        parent = FindOrCreateAllFolders(folder);
                    }
                    catch (Exception ex)
                    {
                        CustomizePlus.Messager.NotificationMessage(ex,
                            $"Could not move profile to {folder} because the folder could not be created.",
                            NotificationType.Error);
                    }

                var (data, _) = CreateDuplicateDataNode(parent, arguments.Profile!.Path.SortName ?? arguments.Profile.Name, arguments.Profile);
                Selection.Select(data, true);
                break;
            case ProfileChanged.Type.Deleted:
                if (arguments.Profile!.Node is { } node)
                {
                    if (node.Selected)
                        Selection.UnselectAll();
                    Delete(node);
                }

                break;
            case ProfileChanged.Type.Renamed when arguments.Profile!.Path.SortName is null:
                RenameWithDuplicates(arguments.Profile.Node!, arguments.Profile.Path.GetIntendedName(arguments.Profile.Name));
                break;
                // TODO: Maybe add path changes?
        }
    }
}
