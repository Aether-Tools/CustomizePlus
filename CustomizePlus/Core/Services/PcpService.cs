using CustomizePlus.Api.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Interop.Ipc;
using CustomizePlus.Profiles;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json.Linq;
using OtterGui.Log;
using OtterGui.Services;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;
using System;

namespace CustomizePlus.Core.Services;

public class PcpService : IRequiredService
{
    private readonly Logger _log;
    private readonly ProfileManager _profileManager;
    private readonly TemplateManager _templateManager;
    private readonly ActorObjectManager _objects;
    private readonly PluginConfiguration _config;

    public PcpService(
        PenumbraIpcHandler ipc,
        Logger log,
        ProfileManager profileManager,
        TemplateManager templateManager,
        ActorObjectManager objects,
        PluginConfiguration config)
    {
        _log = log;
        _profileManager = profileManager;
        _templateManager = templateManager;
        _objects = objects;
        _config = config;

        if (ipc.Available)
        {
            ipc.PcpCreated += OnPcpCreated;
            ipc.PcpParsed += OnPcpParsed;
            _log.Information("[CPlusPCPService] Subscribed to PCP events.");
        }
        else
        {
            _log.Warning("[CPlusPCPService] Penumbra IPC unavailable: PCP events not subscribed.");
        }
    }

    private void OnPcpCreated(JObject jObj, ushort index, string path)
    {
        if (!_config.ExternalSettings.HandlePCPFiles)
            return;

        _log.Debug($"[CPlusPCPService] PcpCreated: Index={index}, Path='{path}'");

        var actorIdentifier = _objects.Actors.FromJson(jObj["Actor"] as JObject);
        if (!actorIdentifier.IsValid)
        {
            _log.Debug("[CPlusPCPService] Invalid actor identifier.");
            return;
        }

        var actor = _objects.Objects[(int)index];
        if (!actor.Valid)
        {
            _log.Debug($"[CPlusPCPService] Actor index: '{index}' is invalid.");
            return;
        }

        var profile = _profileManager.GetActiveProfileByActor(actor);
        if (profile == null)
        {
            _log.Debug("[CPlusPCPService] No active profile found for actor.");
            return;
        }

        var ipcProfile = IPCCharacterProfile.FromFullProfile(profile);
        var template = new Template(ipcProfile);

        jObj["CustomizePlus"] = new JObject
        {
            ["Template"] = template.JsonSerialize()
        };

        _log.Debug("[CPlusPCPService] Successfully added template data to character.json.");
    }

    private void OnPcpParsed(JObject jObj, string modDirectory, Guid collection)
    {
        if (!_config.ExternalSettings.HandlePCPFiles)
            return;

        _log.Debug($"[CPlusPCPService] PcpParsed: ModDirectory='{modDirectory}', Collection={collection}");

        if (jObj["CustomizePlus"] is not JObject cpp)
        {
            _log.Debug("[CPlusPCPService] No CustomizePlus data found in .pcp");
            return;
        }

        if (cpp["Template"] is not JObject templateObj)
        {
            _log.Debug("[CPlusPCPService] No Template data found in .pcp");
            return;
        }

        Template parsedTemplate;
        try
        {
            parsedTemplate = Template.Load(templateObj);
        }
        catch (Exception ex)
        {
            _log.Debug($"[CPlusPCPService] Failed to deserialize template: {ex.Message}");
            return;
        }

        var name = jObj["Mod"] is JValue { Value: string modName } && !string.IsNullOrWhiteSpace(modName)
            ? modName.Trim()
            : "PCPtemplate";

        var newTemplate = _templateManager.Clone(parsedTemplate, $"PCP/{name}", handlePath: true);
        var profile = _profileManager.Create($"PCP/{name}", handlePath: true);

        if (jObj["Actor"] is JObject actorObj)
        {
            var identifier = _objects.Actors.FromJson(actorObj);
            if (identifier.IsValid)
                _profileManager.AddCharacter(profile, identifier);
        }

        _profileManager.AddTemplate(profile, newTemplate);
        _profileManager.SetEnabled(profile, true);

        _log.Debug($"[CPlusPCPService] Loaded CustomizePlus template '{newTemplate.Name}' with {newTemplate.Bones.Count} bones.");
    }


}
