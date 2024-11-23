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
using System.Threading.Tasks;
using OtterGui.Classes;

namespace CustomizePlus.Profiles;

/// <summary>
///     Container class for administrating <see cref="Profile" />s during runtime.
/// </summary>
public partial class ProfileManager : IDisposable
{
    private readonly TemplateManager _templateManager;
    private readonly TemplateEditorManager _templateEditorManager;
    private readonly SaveService _saveService;
    private readonly Logger _logger;
    private readonly PluginConfiguration _configuration;
    private readonly ActorManager _actorManager;
    private readonly GameObjectService _gameObjectService;
    private readonly ObjectManager _objectManager;
    private readonly ReverseNameDicts _reverseNameDicts;
    private readonly MessageService _messageService;
    private readonly ProfileChanged _event;
    private readonly TemplateChanged _templateChangedEvent;
    private readonly ReloadEvent _reloadEvent;
    private readonly ArmatureChanged _armatureChangedEvent;

    public readonly List<Profile> Profiles = new();

    public Profile? DefaultProfile { get; private set; }
    public Profile? DefaultLocalPlayerProfile { get; private set; }

    public ProfileManager(
        TemplateManager templateManager,
        TemplateEditorManager templateEditorManager,
        SaveService saveService,
        Logger logger,
        PluginConfiguration configuration,
        ActorManager actorManager,
        GameObjectService gameObjectService,
        ObjectManager objectManager,
        ReverseNameDicts reverseNameDicts,
        MessageService messageService,
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
        _reverseNameDicts = reverseNameDicts;
        _messageService = messageService;
        _event = @event;
        _templateChangedEvent = templateChangedEvent;
        _templateChangedEvent.Subscribe(OnTemplateChange, TemplateChanged.Priority.ProfileManager);
        _reloadEvent = reloadEvent;
        _reloadEvent.Subscribe(OnReload, ReloadEvent.Priority.ProfileManager);
        _armatureChangedEvent = armatureChangedEvent;
        _armatureChangedEvent.Subscribe(OnArmatureChange, ArmatureChanged.Priority.ProfileManager);

        CreateProfileFolder(saveService);

        _reverseNameDicts.Awaiter.ContinueWith(_ => LoadProfiles(), TaskScheduler.Default);
    }

    public void Dispose()
    {
        _templateChangedEvent.Unsubscribe(OnTemplateChange);
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
    /// Add character to profile
    /// </summary>
    public void AddCharacter(Profile profile, ActorIdentifier actorIdentifier)
    {
        if (!actorIdentifier.IsValid || profile.Characters.Any(x => actorIdentifier.MatchesIgnoringOwnership(x)) || profile.IsTemporary)
            return;

        profile.Characters.Add(actorIdentifier);

        SaveProfile(profile);

        _logger.Debug($"Add character for profile {profile.UniqueId}.");
        _event.Invoke(ProfileChanged.Type.AddedCharacter, profile, actorIdentifier);
    }

    /// <summary>
    /// Delete character from profile
    /// </summary>
    public void DeleteCharacter(Profile profile, ActorIdentifier actorIdentifier)
    {
        if (!actorIdentifier.IsValid || !profile.Characters.Any(x => actorIdentifier.MatchesIgnoringOwnership(x)) || profile.IsTemporary)
            return;

        profile.Characters.Remove(actorIdentifier);

        SaveProfile(profile);

        _logger.Debug($"Removed character from profile {profile.UniqueId}.");
        _event.Invoke(ProfileChanged.Type.RemovedCharacter, profile, actorIdentifier);
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

        profile.Enabled = value;

        SaveProfile(profile);

        _event.Invoke(ProfileChanged.Type.Toggled, profile, value);
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

    public void SetPriority(Profile profile, int value)
    {
        if (profile.Priority == value)
            return;

        if (value > int.MaxValue || value < int.MinValue)
            return;

        profile.Priority = value;

        SaveProfile(profile);

        _event.Invoke(ProfileChanged.Type.PriorityChanged, profile, value);
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

    public void SetDefaultLocalPlayerProfile(Profile? profile)
    {
        if (profile == null)
        {
            if (DefaultLocalPlayerProfile == null)
                return;
        }
        else if (!Profiles.Contains(profile))
            return;

        var previousProfile = DefaultLocalPlayerProfile;

        DefaultLocalPlayerProfile = profile;
        _configuration.DefaultLocalPlayerProfile = profile?.UniqueId ?? Guid.Empty;
        _configuration.Save();

        _logger.Debug($"Set profile {profile?.Incognito ?? "no profile"} as default local player profile");
        _event.Invoke(ProfileChanged.Type.ChangedDefaultLocalPlayerProfile, profile, previousProfile);
    }

    //warn: temporary profile system does not support any world identifiers
    public void AddTemporaryProfile(Profile profile, Actor actor)
    {
        if (!actor.Identifier(_actorManager, out var identifier))
            throw new ActorNotFoundException();

        profile.Enabled = true;
        profile.ProfileType = ProfileType.Temporary;
        profile.Priority = int.MaxValue; //Make sure temporary profile is always at max priority

        var permanentIdentifier = identifier.CreatePermanent();
        profile.Characters.Clear();
        profile.Characters.Add(permanentIdentifier); //warn: identifier must not be AnyWorld or stuff will break!

        var existingProfile = Profiles.FirstOrDefault(p => p.Characters.Count == 1 && p.Characters[0].Matches(permanentIdentifier) && p.IsTemporary);
        if (existingProfile != null)
        {
            _logger.Debug($"Temporary profile for {permanentIdentifier.Incognito(null)} already exists, removing...");
            Profiles.Remove(existingProfile);
            _event.Invoke(ProfileChanged.Type.TemporaryProfileDeleted, existingProfile, null);
        }

        Profiles.Add(profile);

        _logger.Debug($"Added temporary profile for {permanentIdentifier}");
        _event.Invoke(ProfileChanged.Type.TemporaryProfileAdded, profile, null);
    }

    public void RemoveTemporaryProfile(Profile profile)
    {
        if (!profile.IsTemporary)
            return;

        if (!Profiles.Remove(profile))
            throw new ProfileNotFoundException();

        _logger.Debug($"Removed temporary profile for {profile.Characters[0].Incognito(null)}");

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

        var profile = Profiles.FirstOrDefault(x => x.Characters.Count == 1 && x.Characters[0] == identifier && x.IsTemporary);
        if (profile == null)
            throw new ProfileNotFoundException();

        RemoveTemporaryProfile(profile);
    }

    /// <summary>
    /// Return profile by actor identifier, does not return temporary profiles.
    /// </summary>
    /// todo: use GetEnabledProfilesByActor
    public Profile? GetActiveProfileByActor(Actor actor)
    {
        var actorIdentifier = actor.GetIdentifier(_actorManager);

        if (!actorIdentifier.IsValid)
            return null;

        if (actorIdentifier.Type == IdentifierType.Owned && !actorIdentifier.IsOwnedByLocalPlayer())
            return null;

        var query = Profiles.Where(p => p.Characters.Any(x => x.MatchesIgnoringOwnership(actorIdentifier)) && !p.IsTemporary && p.Enabled);

        var profile = query.OrderByDescending(x => x.Priority).FirstOrDefault();

        if(profile == null)
        {
            if (DefaultLocalPlayerProfile?.Enabled == true)
                return DefaultLocalPlayerProfile;

            return null;
        }

        return profile;
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

        bool IsProfileAppliesToCurrentActor(Profile profile)
        {
            //default profile check is done later
            if (profile == DefaultProfile)
                return false;

            if (profile == DefaultLocalPlayerProfile)
                return false;

            if (actorIdentifier.Type == IdentifierType.Owned)
            {
                if(profile.IsTemporary)
                    return profile.Characters.Any(x => x.Matches(actorIdentifier));
                else if(!actorIdentifier.IsOwnedByLocalPlayer())
                    return false;
            }

            return profile.Characters.Any(x => x.MatchesIgnoringOwnership(actorIdentifier));
        }

        if (_templateEditorManager.IsEditorActive && _templateEditorManager.EditorProfile.Enabled && IsProfileAppliesToCurrentActor(_templateEditorManager.EditorProfile))
            yield return _templateEditorManager.EditorProfile;

        foreach (var profile in Profiles.OrderByDescending(x => x.Priority))
        {
            if(profile.Enabled && IsProfileAppliesToCurrentActor(profile))
                yield return profile;
        }

        if (DefaultLocalPlayerProfile != null && DefaultLocalPlayerProfile.Enabled)
        {
            var currentPlayer = _actorManager.GetCurrentPlayer();
            if(_objectManager.IsInLobby || (currentPlayer.IsValid && currentPlayer.Matches(actorIdentifier)))
                yield return DefaultLocalPlayerProfile;
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

        foreach (var profile in Profiles.OrderByDescending(x => x.Priority))
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

            _logger.Debug($"ProfileManager.OnArmatureChange: Removed unused temporary profile for {profile.Characters[0].Incognito(null)}");

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