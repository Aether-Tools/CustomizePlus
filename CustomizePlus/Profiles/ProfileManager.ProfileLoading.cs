using CustomizePlus.Profiles.Data;
using CustomizePlus.Profiles.Events;
using CustomizePlus.Templates.Data;
using CustomizePlus.Templates;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Actors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Penumbra.String;
using Penumbra.GameData.Structs;
using Dalamud.Game.ClientState.Objects.Enums;
using Penumbra.GameData.Gui;
using System.Xml;

namespace CustomizePlus.Profiles;

public partial class ProfileManager : IDisposable
{
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
                var profile = LoadIndividualProfile(data);
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
            if (_configuration.DefaultProfile == profile.UniqueId)
                DefaultProfile = profile;

            if (_configuration.DefaultLocalPlayerProfile == profile.UniqueId)
                DefaultLocalPlayerProfile = profile;
        }

        //insert temp profiles back into profile list
        if (temporaryProfiles.Count > 0)
            Profiles.AddRange(temporaryProfiles);

        var failed = MoveInvalidNames(invalidNames);
        if (invalidNames.Count > 0)
            _logger.Information(
                $"Moved {invalidNames.Count - failed} profiles to correct names.{(failed > 0 ? $" Failed to move {failed} profiles to correct names." : string.Empty)}");

        _logger.Information("Profiles load complete");
        _event.Invoke(ProfileChanged.Type.ReloadedAll, null, null);
    }

    private Profile LoadIndividualProfile(JObject obj)
    {
        var version = obj["Version"]?.ToObject<int>() ?? 0;
        return version switch
        {
            //Ignore everything below v4
             4 => LoadV4(obj),
             5 => LoadV5(obj),
            _ => throw new Exception("The profile to be loaded has no valid Version."),
        };
    }

    private Profile LoadV4(JObject obj)
    {
        var profile = LoadProfileV4V5(obj);

        var characterName = obj["CharacterName"]?.ToObject<string>()?.Trim() ?? throw new ArgumentNullException("CharacterName");

        if (string.IsNullOrWhiteSpace(characterName))
            return profile;

        var nameWordsCnt = characterName.Split(' ').Length;

        //companions come first because they seem to have duplicate entries in NPC dicts
        if (_reverseNameDicts.TryGetID(ObjectKind.Companion, characterName, out var id))
        {
            var currentPlayer = _actorManager.GetCurrentPlayer();
            profile.Characters.Add(_actorManager.CreateOwned(currentPlayer.PlayerName, currentPlayer.HomeWorld, ObjectKind.Companion, new NpcId(id)));
        }
        else if (_reverseNameDicts.TryGetID(ObjectKind.MountType, characterName, out id))
        {
            var currentPlayer = _actorManager.GetCurrentPlayer();
            profile.Characters.Add(_actorManager.CreateOwned(currentPlayer.PlayerName, currentPlayer.HomeWorld, ObjectKind.MountType, new NpcId(id)));
        }
        else if (_reverseNameDicts.TryGetID(ObjectKind.EventNpc, characterName, out id))
            profile.Characters.Add(_actorManager.CreateNpc(ObjectKind.EventNpc, new NpcId(id)));
        else if (_reverseNameDicts.TryGetID(ObjectKind.BattleNpc, characterName, out id))
            profile.Characters.Add(_actorManager.CreateNpc(ObjectKind.BattleNpc, new NpcId(id)));
        else if (nameWordsCnt == 2) //players come last
            profile.Characters.Add(_actorManager.CreatePlayer(ByteString.FromStringUnsafe(characterName, false), WorldId.AnyWorld));
        else
        {
            _logger.Warning($"Unable to automatically migrate \"{profile.Name}\" to V5, unknown character name: {characterName}");
            _messageService.NotificationMessage($"Unable to detect character type for profile \"{profile.Name}\", please set character for this profile manually.", Dalamud.Interface.ImGuiNotification.NotificationType.Error);
        }

        if (profile.Characters.Count > 0)
        {
            _logger.Debug($"Upgraded profile \"{profile.Name}\" to V5: {characterName} -> {profile.Characters[0]}. Save queued.");
            _saveService.QueueSave(profile);
        }

        return profile;
    }

    private Profile LoadV5(JObject obj)
    {
        var profile = LoadProfileV4V5(obj);

        profile.Priority = obj["Priority"]?.ToObject<int>() ?? throw new ArgumentNullException("Priority");

        if (obj["Characters"] is not JArray characterArray)
            return profile;

        foreach(var characterObj in characterArray)
        {
            if (characterObj is not JObject characterObjCast)
            {
                //todo: warning
                continue;
            }

            var character = _actorManager.FromJson(characterObjCast);

            if(!character.IsValid)
            {
                //todo: warning
                continue;
            }

            profile.Characters.Add(character);
        }

        return profile;
    }

    //V4 and V5 are mostly the same, so common loading logic is here
    private Profile LoadProfileV4V5(JObject obj)
    {
        var creationDate = obj["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate");

        var profile = new Profile()
        {
            CreationDate = creationDate,
            UniqueId = obj["UniqueId"]?.ToObject<Guid>() ?? throw new ArgumentNullException("UniqueId"),
            Name = new LowerString(obj["Name"]?.ToObject<string>()?.Trim() ?? throw new ArgumentNullException("Name")),
            Enabled = obj["Enabled"]?.ToObject<bool>() ?? throw new ArgumentNullException("Enabled"),
            ModifiedDate = obj["ModifiedDate"]?.ToObject<DateTimeOffset>() ?? creationDate,
            IsWriteProtected = obj["IsWriteProtected"]?.ToObject<bool>() ?? false,
            Templates = new List<Template>()
        };
        if (profile.ModifiedDate < creationDate)
            profile.ModifiedDate = creationDate;

        if (obj["Templates"] is not JArray templateArray)
            return profile;

        foreach (var templateObj in templateArray)
        {
            if (templateObj is not JObject templateObjCast)
            {
                //todo: warning
                continue;
            }

            var templateId = templateObjCast["TemplateId"]?.ToObject<Guid>();
            if (templateId == null)
                continue; //todo: error

            var template = _templateManager.GetTemplate((Guid)templateId);
            if (template != null)
                profile.Templates.Add(template);
        }

        return profile;
    }
}
