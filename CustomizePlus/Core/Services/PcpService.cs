using CustomizePlus.Api.Data;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Interop.Ipc;
using CustomizePlus.Profiles;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using Penumbra.GameData.Interop;

namespace CustomizePlus.Core.Services;

public class PcpService : IRequiredService
{
    private readonly Logger _logger;
    private readonly ProfileManager _profileManager;
    private readonly TemplateManager _templateManager;
    private readonly ActorObjectManager _objects;
    private readonly PluginConfiguration _configuration;
    private readonly PenumbraIpcHandler _penumbraIpcHandler;

    private bool _isEnabled;

    public bool IsPenumbraAvailable => _penumbraIpcHandler.Available;
    public bool IsEnabled => _isEnabled;

    public PcpService(
        PenumbraIpcHandler ipc,
        Logger logger,
        ProfileManager profileManager,
        TemplateManager templateManager,
        ActorObjectManager objects,
        PluginConfiguration configuration)
    {
        _penumbraIpcHandler = ipc;
        _logger = logger;
        _profileManager = profileManager;
        _templateManager = templateManager;
        _objects = objects;
        _configuration = configuration;

        SetEnabled(_configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled);
    }

    public void SetEnabled(bool value)
    {
        if (value == _isEnabled)
            return;

        if (value)
        {
            _penumbraIpcHandler.PcpCreated += OnPcpCreated;
            _penumbraIpcHandler.PcpParsed += OnPcpParsed;
            _logger.Debug("[CPlusPCPService] Attached to PCP handling.");
        }
        else
        {
            _penumbraIpcHandler.PcpCreated -= OnPcpCreated;
            _penumbraIpcHandler.PcpParsed -= OnPcpParsed;
            _logger.Debug("[CPlusPCPService] Detached from PCP handling.");
        }

        _isEnabled = value;
    }

    /// <summary>
    /// Deletes all PCP data imported into the plugin except for any that was modified by the user (DataSource = User)
    /// </summary>
    public void DeletePCPData()
    {
        _logger.Debug("[CPlusPCPService] Deleting all PCP data imported into the plugin.");

        var profiles = _profileManager.Profiles.Where(p => p.Source == DataSource.PCPImport).ToList();
        _logger.Information($"[CPlusPCPService] {profiles.Count} PCP profiles is about to be deleted");

        foreach (var profile in profiles)
        {
            _logger.Information($"[CPlusPCPService] Deleting PCP profile: {profile}");
            _profileManager.Delete(profile);
        }

        var templates = _templateManager.Templates.Where(t => t.Source == DataSource.PCPImport).ToList();
        _logger.Information($"[CPlusPCPService] {templates.Count} PCP templates is about to be deleted");

        foreach (var template in templates)
        {
            _logger.Information($"[CPlusPCPService] Deleting PCP template: {template}");
            _templateManager.Delete(template);
        }
    }

    private void OnPcpCreated(JObject jObj, ushort index, string path)
    {
        if (!_configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled)
            return;

        _logger.Debug($"[CPlusPCPService] PcpCreated: Index={index}, Path='{path}'");

        var actorIdentifier = _objects.Actors.FromJson(jObj["Actor"] as JObject);
        if (!actorIdentifier.IsValid)
        {
            _logger.Debug("[CPlusPCPService] Invalid actor identifier.");
            return;
        }

        var actor = _objects.Objects[(int)index];
        if (!actor.Valid)
        {
            _logger.Debug($"[CPlusPCPService] Actor index: '{index}' is invalid.");
            return;
        }

        var profile = _profileManager.GetActiveProfileByActor(actor);
        if (profile == null)
        {
            _logger.Debug("[CPlusPCPService] No active profile found for actor.");
            return;
        }

        var ipcProfile = IPCCharacterProfile.FromFullProfile(profile);
        var template = new Template(ipcProfile);

        jObj["CustomizePlus"] = new JObject
        {
            ["Template"] = template.JsonSerialize()
        };

        _logger.Debug("[CPlusPCPService] Successfully added template data to character.json.");
    }

    private void OnPcpParsed(JObject jObj, string modDirectory, Guid collection)
    {
        if (!_configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled)
            return;

        _logger.Debug($"[CPlusPCPService] PcpParsed: ModDirectory='{modDirectory}', Collection={collection}");

        if (jObj["CustomizePlus"] is not JObject cpp)
        {
            _logger.Debug("[CPlusPCPService] No CustomizePlus data found in .pcp");
            return;
        }

        if (cpp["Template"] is not JObject templateObj)
        {
            _logger.Debug("[CPlusPCPService] No Template data found in .pcp");
            return;
        }

        Template parsedTemplate;
        try
        {
            parsedTemplate = Template.Load(templateObj);
        }
        catch (Exception ex)
        {
            _logger.Debug($"[CPlusPCPService] Failed to deserialize template: {ex.Message}");
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

        _templateManager.SetWriteProtection(newTemplate, true);
        _profileManager.SetWriteProtection(profile, true);

        //Should be done last or otherwise source will be overwritten with default value
        _templateManager.SetSource(newTemplate, DataSource.PCPImport);
        _profileManager.SetSource(profile, DataSource.PCPImport);

        _logger.Debug($"[CPlusPCPService] Loaded CustomizePlus template '{newTemplate.Name}' with {newTemplate.Bones.Count} bones.");
    }
}
