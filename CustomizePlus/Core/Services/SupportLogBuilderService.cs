using System;
using System.Linq;
using System.Text;
using CustomizePlus.Armatures.Services;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Core.Helpers;
using CustomizePlus.Core.Services.Dalamud;
using CustomizePlus.Profiles;
using CustomizePlus.Templates;
using Dalamud.Plugin;

namespace CustomizePlus.Core.Services;

//Based on Penumbra's support log
public class SupportLogBuilderService
{
    private readonly PluginConfiguration _configuration;
    private readonly TemplateManager _templateManager;
    private readonly ProfileManager _profileManager;
    private readonly ArmatureManager _armatureManager;
    private readonly IDalamudPluginInterface _dalamudPluginInterface;
    private readonly PcpService _pcpService;

    public SupportLogBuilderService(
        PluginConfiguration configuration,
        TemplateManager templateManager,
        ProfileManager profileManager,
        ArmatureManager armatureManager,
        IDalamudPluginInterface dalamudPluginInterface,
        PcpService pcpService)
    {
        _configuration = configuration;
        _templateManager = templateManager;
        _profileManager = profileManager;
        _armatureManager = armatureManager;
        _dalamudPluginInterface = dalamudPluginInterface;
        _pcpService = pcpService;
    }

    public string BuildSupportLog()
    {
        var sb = new StringBuilder(102400); //it's fair to assume this will very often be quite large
        sb.AppendLine("**Settings**");
        sb.Append($"> **`Plugin Version:                 `** {VersionHelper.Version}\n");
        sb.Append($"> **`Commit Hash:                    `** {ThisAssembly.Git.Commit}+{ThisAssembly.Git.Sha}\n");
        sb.Append($"> **`Plugin enabled:                 `** {_configuration.PluginEnabled}\n");
        sb.AppendLine("**Settings -> Editor Settings**");
        sb.Append($"> **`Preview character (editor):     `** {_configuration.EditorConfiguration.PreviewCharacter.Incognito(null)}\n");
        sb.Append($"> **`Set preview character on login: `** {_configuration.EditorConfiguration.SetPreviewToCurrentCharacterOnLogin}\n");
        sb.Append($"> **`Root editing:                   `** {_configuration.EditorConfiguration.RootPositionEditingEnabled}\n");
        sb.AppendLine("**Settings -> Profile application**");
        sb.Append($"> **`Character window:               `** {_configuration.ProfileApplicationSettings.ApplyInCharacterWindow}\n");
        sb.Append($"> **`Try On:                         `** {_configuration.ProfileApplicationSettings.ApplyInTryOn}\n");
        sb.Append($"> **`Cards:                          `** {_configuration.ProfileApplicationSettings.ApplyInCards}\n");
        sb.Append($"> **`Inspect:                        `** {_configuration.ProfileApplicationSettings.ApplyInInspect}\n");
        sb.Append($"> **`Lobby:                          `** {_configuration.ProfileApplicationSettings.ApplyInLobby}\n");
        sb.AppendLine("**Dalamud**");
        sb.Append($"> **`Dalamud Version:                `** {_dalamudPluginInterface.GetDalamudVersion().Version}\n");
        sb.Append($"> **`Branch:                         `** {_dalamudPluginInterface.GetDalamudVersion().BetaTrack ?? "Release"}\n");
        sb.AppendLine("**Relevant plugins**");
        GatherRelevantPlugins(sb);
        sb.AppendLine("**Integrations**");
        sb.Append($"> **`Penumbra (PCP):                 `** {_configuration.IntegrationSettings.PenumbraPCPIntegrationEnabled} (Penumbra is{(!_pcpService.IsPenumbraAvailable ? " NOT " : " ")}available)\n");
        sb.AppendLine("**Templates**");
        sb.Append($"> **`Count:                          `** {_templateManager.Templates.Count}\n");
        foreach (var template in _templateManager.Templates)
        {
            sb.Append($">   > **`{template.ToString(),-32}`**\n");
        }
        sb.AppendLine("**Profiles**");
        sb.Append($"> **`Default profile:                `** {_profileManager.DefaultProfile?.ToString() ?? "None"}\n");
        sb.Append($"> **`Default local player profile:   `** {_profileManager.DefaultLocalPlayerProfile?.ToString() ?? "None"}\n");
        sb.Append($"> **`Count:                          `** {_profileManager.Profiles.Count}\n");
        foreach (var profile in _profileManager.Profiles)
        {
            sb.Append($">   > =====\n");
            sb.Append($">   > **`{profile.ToString(),-32}`*\n");
            sb.Append($">   > **`Name:                       `** {profile.Name.Text.Incognify()}\n");
            sb.Append($">   > **`Type:                       `** {profile.ProfileType} \n");
            sb.Append($">   > **`Characters:             `** {string.Join(',', profile.Characters.Select(x => x.Incognito(null)))}\n");
            sb.Append($">   > **`Templates:`**\n");
            sb.Append($">   >   > **`Count:                  `** {profile.Templates.Count}\n");
            foreach (var template in profile.Templates)
            {
                sb.Append($">   >   > **`{template.ToString(),-32}`**\n");

                if (profile.DisabledTemplates.Contains(template.UniqueId))
                    sb.Append($">   >   >   >  **`Disabled`**\n");
            }
            sb.Append($">   > **`Armatures:`**\n");
            sb.Append($">   >   > **`Count:                  `** {profile.Armatures.Count}\n");
            foreach (var armature in profile.Armatures)
            {
                sb.Append($">   >   > **`{armature.ToString(),-32}`**\n");
            }
            sb.Append($">   > =====\n");
        }
        sb.AppendLine("**Armatures**");
        sb.Append($"> **`Count:                          `** {_armatureManager.Armatures.Count}\n");
        foreach (var kvPair in _armatureManager.Armatures)
        {
            var identifier = kvPair.Key;
            var armature = kvPair.Value;
            sb.Append($">   > =====\n");
            sb.Append($">   > **`{armature.ToString(),-32}`**\n");
            sb.Append($">   > **`Actor:                      `** {armature.ActorIdentifier.Incognito(null) ?? "None"}\n");
            sb.Append($">   > **`Built:                      `** {armature.IsBuilt}\n");
            sb.Append($">   > **`Visible:                    `** {armature.IsVisible}\n");
            sb.Append($">   > **`Pending rebind:             `** {armature.IsPendingProfileRebind}\n");
            sb.Append($">   > **`Last seen:                  `** {armature.LastSeen}\n");
            sb.Append($">   > **`Profile:                    `** {armature.Profile?.ToString() ?? "None"}\n");
            sb.Append($">   > **`Bone template bindings:`**\n");
            foreach (var bindingKvPair in armature.BoneTemplateBinding)
            {
                sb.Append($">   >   > **`{BoneData.GetBoneDisplayName(bindingKvPair.Key)} ({bindingKvPair.Key}) -> {bindingKvPair.Value.ToString()}`**\n");
            }
            sb.Append($">   > =====\n");
        }
        return sb.ToString();
    }


    private void GatherRelevantPlugins(StringBuilder sb)
    {
        ReadOnlySpan<string> relevantPlugins =
        [
            "MareSynchronos", "Ktisis", "Brio", "DynamicBridge", "SimpleHeels",
            "IllusioVitae", "LoporritSync", "AQuestReborn", "RoleplayingVoiceDalamud", "AetherRemote",
            "CustomizePlusPlus", "CharacterSelectPlugin"
        ];
        var plugins = _dalamudPluginInterface.InstalledPlugins
            .GroupBy(p => p.InternalName)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.OrderByDescending(p => p.IsLoaded).ThenByDescending(p => p.Version).First();
                return (item.IsLoaded, item.Version, item.Name);
            });
        foreach (var plugin in relevantPlugins)
        {
            if (plugins.TryGetValue(plugin, out var data))
                sb.Append($"> **`{data.Name + ':',-32}`** {data.Version}{(data.IsLoaded ? string.Empty : " (Disabled)")}\n");
        }
    }
}
