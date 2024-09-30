using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Utility;
using Newtonsoft.Json.Linq;
using OtterGui.Log;
using OtterGui.Filesystem;
using Penumbra.GameData.Actors;
using CustomizePlus.Core.Services;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Armatures.Events;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Core.Events;
using CustomizePlus.Templates;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Events;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.GameData.Data;
using CustomizePlus.GameData.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles.Enums;
using CustomizePlus.Profiles.Exceptions;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using System.Runtime.Serialization;
using CustomizePlus.Game.Services;
using ObjectManager = CustomizePlus.GameData.Services.ObjectManager;

namespace CustomizePlus.Profiles;

/// <summary>
///     Container class for administrating <see cref="Profile" />s during runtime.
/// </summary>
public class ProfileManager : IDisposable
{
    private readonly TemplateManager _templateManager;
    private readonly TemplateEditorManager _templateEditorManager;
    private readonly SaveService _saveService;
    private readonly Logger _logger;
    private readonly PluginConfiguration _configuration;
    private readonly ActorManager _actorManager;
    private readonly GameObjectService _gameObjectService;
    private readonly ObjectManager _objectManager;
    private readonly ProfileChanged _event;
    private readonly TemplateChanged _templateChangedEvent;
    private readonly ReloadEvent _reloadEvent;
    private readonly ArmatureChanged _armatureChangedEvent;

    public readonly List<Profile> Profiles = new();

    public Profile? DefaultProfile { get; private set; }

    public ProfileManager(
        TemplateManager templateManager,
        TemplateEditorManager templateEditorManager,
        SaveService saveService,
        Logger logger,
        PluginConfiguration configuration,
        ActorManager actorManager,
        GameObjectService gameObjectService,
        ObjectManager objectManager,
        ProfileChanged @event,
        TemplateChanged templateChangedEvent,
        ReloadEvent reloadEvent,
        ArmatureChanged armatureChangedEvent)
    {
        _templateManager = templateManager;
        _templateEditorManager = templateEditorManager;
        _saveService = saveService;
        _logger = logger;
        _configuration = configuration;
        _actorManager = actorManager;
        _gameObjectService = gameObjectService;
        _objectManager = objectManager;
        _event = @event;
        _templateChangedEvent = templateChangedEvent;
        _templateChangedEvent.Subscribe(OnTemplateChange, TemplateChanged.Priority.ProfileManager);
        _reloadEvent = reloadEvent;
        _reloadEvent.Subscribe(OnReload, ReloadEvent.Priority.ProfileManager);
        _armatureChangedEvent = armatureChangedEvent;
        _armatureChangedEvent.Subscribe(OnArmatureChange, ArmatureChanged.Priority.ProfileManager);

        CreateProfileFolder(saveService);

        LoadProfiles();
    }

    public void Dispose()
    {
        _templateChangedEvent.Unsubscribe(OnTemplateChange);
    }

    public void LoadProfiles()
    {
        _logger.Information("Loading profiles...");

        //todo: hot reload was not tested
        //save temp profiles
        var temporaryProfiles = Profiles.Where(x => x.IsTemporary).ToList();

        Profiles.Clear();
        List<(Profile, string)> invalidNames = new();
        foreach (var file in _saveService.FileNames.Profiles())
        {
            _logger.Debug($"Reading profile {file.FullName}");

            try
            {
                var text = File.ReadAllText(file.FullName);
                var data = JObject.Parse(text);
                var profile = Profile.Load(_templateManager, data);
                if (profile.UniqueId.ToString() != Path.GetFileNameWithoutExtension(file.Name))
                    invalidNames.Add((profile, file.FullName));
                if (Profiles.Any(f => f.UniqueId == profile.UniqueId))
                    throw new Exception($"ID {profile.UniqueId} was not unique.");

                Profiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not load profile, skipped:\n{ex}");
                //++skipped;
            }
        }

        foreach (var profile in Profiles)
        {
            //This will solve any issues if file on disk was manually edited and we have more than a single active profile
            if (profile.Enabled)
                SetEnabled(profile, true, true);

            if (_configuration.DefaultProfile == profile.UniqueId)
                DefaultProfile = profile;
        }

        //insert temp profiles back into profile list
        if (temporaryProfiles.Count > 0)
        {
            Profiles.AddRange(temporaryProfiles);
            Profiles.Sort((x, y) => y.IsTemporary.CompareTo(x.IsTemporary));
        }

        var failed = MoveInvalidNames(invalidNames);
        if (invalidNames.Count > 0)
            _logger.Information(
                $"Moved {invalidNames.Count - failed} profiles to correct names.{(failed > 0 ? $" Failed to move {failed} profiles to correct names." : string.Empty)}");

        _logger.Information("Profiles load complete");
        _event.Invoke(ProfileChanged.Type.ReloadedAll, null, null);
    }

    /// <summary>
    /// Main rendering function, called from rendering hook after calling ArmatureManager.OnRender
    /// </summary>
    public void OnRender()
    {

    }

    public Profile Create(string name, bool handlePath)
    {
        var (actualName, path) = NameParsingHelper.ParseName(name, handlePath);
        var profile = new Profile
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = CreateNewGuid(),
            Name = actualName
        };

        Profiles.Add(profile);
        _logger.Debug($"Added new profile {profile.UniqueId}.");

        _saveService.ImmediateSave(profile);

        _event.Invoke(ProfileChanged.Type.Created, profile, path);

        return profile;
    }

    /// <summary>
    /// Create a new profile by cloning passed profile
    /// </summary>
    /// <returns></returns>
    public Profile Clone(Profile clone, string name, bool handlePath)
    {
        var (actualName, path) = NameParsingHelper.ParseName(name, handlePath);
        var profile = new Profile(clone)
        {
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            UniqueId = CreateNewGuid(),
            Name = actualName,
            Enabled = false
        };

        Profiles.Add(profile);
        _logger.Debug($"Added new profile {profile.UniqueId} by cloning.");

        _saveService.ImmediateSave(profile);

        _event.Invoke(ProfileChanged.Type.Created, profile, path);

        return profile;
    }

    /// <summary>
    /// Rename profile
    /// </summary>
    public void Rename(Profile profile, string newName)
    {
        newName = newName.Trim();

        var oldName = profile.Name.Text;
        if (oldName == newName)
            return;

        profile.Name = newName;

        SaveProfile(profile);

        _logger.Debug($"Renamed profile {profile.UniqueId}.");
        _event.Invoke(ProfileChanged.Type.Renamed, profile, oldName);
    }

    /// <summary>
    /// Change character name for profile
    /// </summary>
    public void ChangeCharacterName(Profile profile, string newName)
    {
        newName = newName.Trim();

        var oldName = profile.CharacterName.Text;
        if (oldName == newName)
            return;

        profile.CharacterName = newName;

        //Called so all other active profiles for new character name get disabled
        //saving is performed there
        SetEnabled(profile, profile.Enabled, true);

        SaveProfile(profile);

        _logger.Debug($"Changed character name for profile {profile.UniqueId}.");
        _event.Invoke(ProfileChanged.Type.ChangedCharacterName, profile, oldName);
    }

    /// <summary>
    /// Delete profile
    /// </summary>
    /// <param name="profile"></param>
    public void Delete(Profile profile)
    {
        Profiles.Remove(profile);
        _saveService.ImmediateDelete(profile);
        _event.Invoke(ProfileChanged.Type.Deleted, profile, null);
    }

    /// <summary>
    /// Set write protection state for profile
    /// </summary>
    public void SetWriteProtection(Profile profile, bool value)
    {
        if (profile.IsWriteProtected == value)
            return;

        profile.IsWriteProtected = value;

        SaveProfile(profile);

        _logger.Debug($"Set profile {profile.UniqueId} to {(value ? string.Empty : "no longer be ")} write-protected.");
        _event.Invoke(ProfileChanged.Type.WriteProtection, profile, value);
    }

    public void SetEnabled(Profile profile, bool value, bool force = false)
    {
        if (profile.Enabled == value && !force)
            return;

        var oldValue = profile.Enabled;

        if (value)
        {
            _logger.Debug($"Setting {profile} as enabled...");

            foreach (var otherProfile in Profiles
                         .Where(x => x.CharacterName == profile.CharacterName && x != profile && x.Enabled && !x.IsTemporary))
            {
                _logger.Debug($"\t-> {otherProfile} disabled");
                SetEnabled(otherProfile, false);
            }
        }

        if (oldValue != value)
        {
            profile.Enabled = value;

            SaveProfile(profile);

            _event.Invoke(ProfileChanged.Type.Toggled, profile, value);
        }
    }
    
    public void SetEnabled(Guid guid, bool value)
    {
        var profile = Profiles.FirstOrDefault(x => x.UniqueId == guid && x.ProfileType == ProfileType.Normal);
        if (profile != null)
        {
            SetEnabled(profile, value);
        }
        else
            throw new ProfileNotFoundException();
    }
    public void SetLimitLookupToOwned(Profile profile, bool value)
    {
        if (profile.LimitLookupToOwnedObjects != value)
        {
            profile.LimitLookupToOwnedObjects = value;

            SaveProfile(profile);

            _event.Invoke(ProfileChanged.Type.LimitLookupToOwnedChanged, profile, value);
        }
    }

    public void DeleteTemplate(Profile profile, int templateIndex)
    {
        _logger.Debug($"Deleting template #{templateIndex} from {profile}...");

        var template = profile.Templates[templateIndex];
        profile.Templates.RemoveAt(templateIndex);

        SaveProfile(profile);

        _event.Invoke(ProfileChanged.Type.RemovedTemplate, profile, template);
    }

    public void AddTemplate(Profile profile, Template template)
    {
        if (profile.Templates.Contains(template))
            return;

        profile.Templates.Add(template);

        SaveProfile(profile);

        _logger.Debug($"Added template: {template.UniqueId} to {profile.UniqueId}");

        _event.Invoke(ProfileChanged.Type.AddedTemplate, profile, template);
    }

    public void ChangeTemplate(Profile profile, int index, Template newTemplate)
    {
        if (index >= profile.Templates.Count || index < 0)
            return;

        if (profile.Templates[index] == newTemplate)
            return;

        var oldTemplate = profile.Templates[index];
        profile.Templates[index] = newTemplate;

        SaveProfile(profile);

        _logger.Debug($"Changed template on profile {profile.UniqueId} from {oldTemplate.UniqueId} to {newTemplate.UniqueId}");
        _event.Invoke(ProfileChanged.Type.ChangedTemplate, profile, (index, oldTemplate, newTemplate));
    }

    public void MoveTemplate(Profile profile, int fromIndex, int toIndex)
    {
        if (!profile.Templates.Move(fromIndex, toIndex))
            return;

        SaveProfile(profile);

        _logger.Debug($"Moved template {fromIndex + 1} to position {toIndex + 1}.");
        _event.Invoke(ProfileChanged.Type.MovedTemplate, profile, (fromIndex, toIndex));
    }

    public void SetDefaultProfile(Profile? profile)
    {
        if (profile == null)
        {
            if (DefaultProfile == null)
                return;
        }
        else if (!Profiles.Contains(profile))
            return;

        var previousProfile = DefaultProfile;

        DefaultProfile = profile;
        _configuration.DefaultProfile = profile?.UniqueId ?? Guid.Empty;
        _configuration.Save();

        _logger.Debug($"Set profile {profile?.Incognito ?? "no profile"} as default");
        _event.Invoke(ProfileChanged.Type.ChangedDefaultProfile, profile, previousProfile);
    }

    public void AddTemporaryProfile(Profile profile, Actor actor/*, Template template*/)
    {
        if (!actor.Identifier(_actorManager, out var identifier))
            throw new ActorNotFoundException();

        profile.Enabled = true;
        profile.ProfileType = ProfileType.Temporary;
        profile.TemporaryActor = identifier;
        profile.CharacterName = identifier.ToNameWithoutOwnerName();
        profile.LimitLookupToOwnedObjects = false;

        var existingProfile = Profiles.FirstOrDefault(x => x.CharacterName.Lower == profile.CharacterName.Lower && x.IsTemporary);
        if (existingProfile != null)
        {
            _logger.Debug($"Temporary profile for {existingProfile.CharacterName} already exists, removing...");
            Profiles.Remove(existingProfile);
            _event.Invoke(ProfileChanged.Type.TemporaryProfileDeleted, existingProfile, null);
        }

        Profiles.Add(profile);

        //Make sure temporary profiles come first, so they are returned by all other methods first
        Profiles.Sort((x, y) => y.IsTemporary.CompareTo(x.IsTemporary));

        _logger.Debug($"Added temporary profile for {identifier}");
        _event.Invoke(ProfileChanged.Type.TemporaryProfileAdded, profile, null);
    }

    public void RemoveTemporaryProfile(Profile profile)
    {
        if (!Profiles.Remove(profile))
            throw new ProfileNotFoundException();

        _logger.Debug($"Removed temporary profile for {profile.CharacterName}");

        _event.Invoke(ProfileChanged.Type.TemporaryProfileDeleted, profile, null);
    }

    public void RemoveTemporaryProfile(Guid profileId)
    {
        var profile = Profiles.FirstOrDefault(x => x.UniqueId == profileId && x.IsTemporary);
        if (profile == null)
            throw new ProfileNotFoundException();

        RemoveTemporaryProfile(profile);
    }

    public void RemoveTemporaryProfile(Actor actor)
    {
        if (!actor.Identifier(_actorManager, out var identifier))
            throw new ActorNotFoundException();

        var profile = Profiles.FirstOrDefault(x => x.TemporaryActor == identifier && x.IsTemporary);
        if (profile == null)
            throw new ProfileNotFoundException();

        RemoveTemporaryProfile(profile);
    }

    /// <summary>
    /// Return profile by character name, does not return temporary profiles
    /// </summary>
    /// <param name="name"></param>
    /// <param name="enabledOnly"></param>
    /// <returns></returns>
    public Profile? GetProfileByCharacterName(string name, bool enabledOnly = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var query = Profiles.Where(x => x.CharacterName == name);
        if (enabledOnly)
            query = query.Where(x => x.Enabled);

        return query.FirstOrDefault();
    }

    //todo: replace with dictionary
    /// <summary>
    /// Returns all enabled profiles which might apply to the given object, prioritizing temporary profiles and editor profile.
    /// </summary>
    public IEnumerable<Profile> GetEnabledProfilesByActor(ActorIdentifier actorIdentifier)
    {
        //performance: using textual override for ProfileAppliesTo here to not call
        //GetGameObjectName every time we are trying to check object against profiles

        if (_objectManager.IsInLobby && !_configuration.ProfileApplicationSettings.ApplyInLobby)
            yield break;

        (actorIdentifier, _) = _gameObjectService.GetTrueActorForSpecialTypeActor(actorIdentifier);

        if (!actorIdentifier.IsValid)
            yield break;

        var name = actorIdentifier.ToNameWithoutOwnerName();

        if (name.IsNullOrWhitespace())
            yield break;

        bool IsProfileAppliesToCurrentActor(Profile profile)
        {
            //default profile check is done later
            if (profile == DefaultProfile)
                return false;

            return profile.CharacterName.Text == name &&
                (!profile.LimitLookupToOwnedObjects ||
                    (actorIdentifier.Type == IdentifierType.Owned &&
                    actorIdentifier.PlayerName == _actorManager.GetCurrentPlayer().PlayerName));
        }

        if (_templateEditorManager.IsEditorActive && _templateEditorManager.EditorProfile.Enabled && IsProfileAppliesToCurrentActor(_templateEditorManager.EditorProfile))
            yield return _templateEditorManager.EditorProfile;

        foreach (var profile in Profiles)
        {
            if (IsProfileAppliesToCurrentActor(profile) && profile.Enabled)
                yield return profile;
        }

        if (DefaultProfile != null &&
            DefaultProfile.Enabled &&
            (actorIdentifier.Type == IdentifierType.Player || actorIdentifier.Type == IdentifierType.Retainer))
            yield return DefaultProfile;
    }

    public IEnumerable<Profile> GetProfilesUsingTemplate(Template template)
    {
        if (template == null)
            yield break;

        foreach (var profile in Profiles)
            if (profile.Templates.Contains(template))
                yield return profile;

        if (_templateEditorManager.EditorProfile.Templates.Contains(template))
            yield return _templateEditorManager.EditorProfile;
    }

    private void SaveProfile(Profile profile)
    {
        //disallow saving special profiles
        if (profile.ProfileType != ProfileType.Normal)
            return;

        profile.ModifiedDate = DateTimeOffset.UtcNow;
        _saveService.QueueSave(profile);
    }

    private void OnTemplateChange(TemplateChanged.Type type, Template? template, object? arg3)
    {
        if (type is not TemplateChanged.Type.Deleted)
            return;

        foreach (var profile in Profiles)
        {
            for (var i = 0; i < profile.Templates.Count; ++i)
            {
                if (profile.Templates[i] != template)
                    continue;

                profile.Templates.RemoveAt(i--);

                _event.Invoke(ProfileChanged.Type.RemovedTemplate, profile, template);

                SaveProfile(profile);

                _logger.Debug($"Removed template {template.UniqueId} from {profile.UniqueId} because template was deleted");
            }
        }

        return;
    }

    private void OnReload(ReloadEvent.Type type)
    {
        if (type != ReloadEvent.Type.ReloadProfiles &&
            type != ReloadEvent.Type.ReloadAll)
            return;

        _logger.Debug("Reload event received");
        LoadProfiles();
    }


    private void OnArmatureChange(ArmatureChanged.Type type, Armature armature, object? arg3)
    {
        if (type == ArmatureChanged.Type.Deleted)
        {
            //hack: sending TemporaryProfileDeleted will result in OnArmatureChange being sent
            //so we need to make sure that we do not end up with endless loop here
            //the whole reason DeletionReason exists is this
            if ((ArmatureChanged.DeletionReason)arg3 != ArmatureChanged.DeletionReason.Gone)
                return;

            var profile = armature!.Profile;

            if (!profile.IsTemporary)
                return;

            //Do not proceed unless there are no armatures left
            //because this might be the case of examine window actor being gone.
            //Profiles for those are shared with the original actor.
            if (profile.Armatures.Count > 0)
                return;

            //todo: TemporaryProfileDeleted ends up calling this again, fix this.
            //Profiles.Remove check won't allow for infinite loop but this isn't good anyway
            if (!Profiles.Remove(profile))
                return;

            _logger.Debug($"ProfileManager.OnArmatureChange: Removed unused temporary profile for {profile.CharacterName}");

            _event.Invoke(ProfileChanged.Type.TemporaryProfileDeleted, profile, null);
        }
    }

    private static void CreateProfileFolder(SaveService service)
    {
        var ret = service.FileNames.ProfileDirectory;
        if (Directory.Exists(ret))
            return;

        try
        {
            Directory.CreateDirectory(ret);
        }
        catch (Exception ex)
        {
            Plugin.Logger.Error($"Could not create profile directory {ret}:\n{ex}");
        }
    }

    /// <summary> Move all files that were discovered to have names not corresponding to their identifier to correct names, if possible. </summary>
    /// <returns>The number of files that could not be moved.</returns>
    private int MoveInvalidNames(IEnumerable<(Profile, string)> invalidNames)
    {
        var failed = 0;
        foreach (var (profile, name) in invalidNames)
        {
            try
            {
                var correctName = _saveService.FileNames.ProfileFile(profile);
                File.Move(name, correctName, false);
                _logger.Information($"Moved invalid profile file from {Path.GetFileName(name)} to {Path.GetFileName(correctName)}.");
            }
            catch (Exception ex)
            {
                ++failed;
                _logger.Error($"Failed to move invalid profile file from {Path.GetFileName(name)}:\n{ex}");
            }
        }

        return failed;
    }

    /// <summary>
    /// Create new guid until we find one which isn't used by existing profile
    /// </summary>
    /// <returns></returns>
    private Guid CreateNewGuid()
    {
        while (true)
        {
            var guid = Guid.NewGuid();
            if (Profiles.All(d => d.UniqueId != guid))
                return guid;
        }
    }
}