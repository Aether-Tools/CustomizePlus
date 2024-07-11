using OtterGui.Filesystem;
using OtterGui.Log;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using OtterGui.Classes;
using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;
using Dalamud.Interface.ImGuiNotification;

namespace CustomizePlus.Profiles;

public class ProfileFileSystem : FileSystem<Profile>, IDisposable, ISavable
{
    private readonly ProfileManager _profileManager;
    private readonly SaveService _saveService;
    private readonly ProfileChanged _profileChanged;
    private readonly MessageService _messageService;
    private readonly Logger _logger;

    public ProfileFileSystem(
        ProfileManager profileManager,
        SaveService saveService,
        ProfileChanged profileChanged,
        MessageService messageService,
        Logger logger)
    {
        _profileManager = profileManager;
        _saveService = saveService;
        _profileChanged = profileChanged;
        _messageService = messageService;
        _logger = logger;

        _profileChanged.Subscribe(OnProfileChange, ProfileChanged.Priority.ProfileFileSystem);

        Changed += OnChange;

        Reload();
    }

    public void Dispose()
    {
        _profileChanged.Unsubscribe(OnProfileChange);
    }

    // Search the entire filesystem for the leaf corresponding to a profile.
    public bool FindLeaf(Profile profile, [NotNullWhen(true)] out Leaf? leaf)
    {
        leaf = Root.GetAllDescendants(ISortMode<Profile>.Lexicographical)
            .OfType<Leaf>()
            .FirstOrDefault(l => l.Value == profile);
        return leaf != null;
    }

    private void OnProfileChange(ProfileChanged.Type type, Profile? profile, object? data)
    {
        switch (type)
        {
            case ProfileChanged.Type.Created:
                var parent = Root;
                if (data is string path)
                    try
                    {
                        parent = FindOrCreateAllFolders(path);
                    }
                    catch (Exception ex)
                    {
                        _messageService.NotificationMessage(ex, $"Could not move profile to {path} because the folder could not be created.", NotificationType.Error);
                    }

                CreateDuplicateLeaf(parent, profile.Name.Text, profile);

                return;
            case ProfileChanged.Type.Deleted:
                if (FindLeaf(profile, out var leaf1))
                    Delete(leaf1);
                return;
            case ProfileChanged.Type.ReloadedAll:
                Reload();
                return;
            case ProfileChanged.Type.Renamed when data is string oldName:
                if (!FindLeaf(profile, out var leaf2))
                    return;

                var old = oldName.FixName();
                if (old == leaf2.Name || leaf2.Name.IsDuplicateName(out var baseName, out _) && baseName == old)
                    RenameWithDuplicates(leaf2, profile.Name);
                return;
        }
    }

    private void Reload()
    {
        if (!File.Exists(_saveService.FileNames.ProfileFileSystem))
        {
            _logger.Debug("WORKAROUND: saving filesystem file");
            _saveService.ImmediateSaveSync(this);
        }

        if (Load(new FileInfo(_saveService.FileNames.ProfileFileSystem), _profileManager.Profiles, ProfileToIdentifier, ProfileToName))
            _saveService.ImmediateSave(this);

        _logger.Debug("Reloaded profile filesystem.");
    }

    private void OnChange(FileSystemChangeType type, IPath _1, IPath? _2, IPath? _3)
    {
        if (type != FileSystemChangeType.Reload)
            _saveService.QueueSave(this);
    }

    // Used for saving and loading.
    private static string ProfileToIdentifier(Profile profile)
        => profile.UniqueId.ToString();

    private static string ProfileToName(Profile profile)
        => profile.Name.Text.FixName();

    private static bool ProfileHasDefaultPath(Profile profile, string fullPath)
    {
        var regex = new Regex($@"^{Regex.Escape(ProfileToName(profile))}( \(\d+\))?$");
        return regex.IsMatch(fullPath);
    }

    private static (string, bool) SaveProfile(Profile profile, string fullPath)
        // Only save pairs with non-default paths.
        => ProfileHasDefaultPath(profile, fullPath)
            ? (string.Empty, false)
            : (ProfileToIdentifier(profile), true);

    public string ToFilename(FilenameService fileNames) => fileNames.ProfileFileSystem;

    public void Save(StreamWriter writer)
    {
        SaveToFile(writer, SaveProfile, true);
    }
}
