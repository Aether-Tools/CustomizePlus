using CustomizePlus.Armatures.Data;
using CustomizePlus.Armatures.Services;
using CustomizePlus.Configuration.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using Penumbra.GameData.Interop;

#if INCOGNIFY_STRINGS
using System.Numerics;
#endif

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

public class StateMonitoringTab : ITab<MainTabType>
{
    private readonly ProfileManager _profileManager;
    private readonly TemplateManager _templateManager;
    private readonly ArmatureManager _armatureManager;
    private readonly ActorObjectManager _objectManager;
    private readonly GameObjectService _gameObjectService;
    private readonly PluginConfiguration _configuration;

    public StateMonitoringTab(
        ProfileManager profileManager,
        TemplateManager templateManager,
        ArmatureManager armatureManager,
        ActorObjectManager objectManager,
        GameObjectService gameObjectService,
        PluginConfiguration configuration)
    {
        _profileManager = profileManager;
        _templateManager = templateManager;
        _armatureManager = armatureManager;
        _objectManager = objectManager;
        _gameObjectService = gameObjectService;
        _configuration = configuration;
    }

    public ReadOnlySpan<byte> Label
        => "State Monitoring"u8;

    public MainTabType Identifier
        => MainTabType.StateMonitoring;

    public bool IsVisible => _configuration.DebuggingModeEnabled;

    public void DrawContent()
    {
        var showProfiles = Im.Tree.Header($"Profiles ({_profileManager.Profiles.Count})###profiles_header");

        if (showProfiles)
            DrawProfiles();

        var showTemplates = Im.Tree.Header($"Templates ({_templateManager.Templates.Count})###templates_header");

        if (showTemplates)
            DrawTemplates();

        var showArmatures = Im.Tree.Header($"Armatures ({_armatureManager.Armatures.Count})###armatures_header");

        if (showArmatures)
            DrawArmatures();

        var showObjectManager = Im.Tree.Header($"Object manager ({_objectManager.Count})###objectmanager_header");

        if (showObjectManager)
            DrawObjectManager();
    }

    private void DrawProfiles()
    {
        foreach (var profile in _profileManager.Profiles.OrderByDescending(x => x.Enabled).ThenByDescending(x => x.Priority))
        {
            DrawSingleProfile("root", profile);
            Im.Line.Spacing();
            Im.Line.Spacing();
        }
    }

    private void DrawTemplates()
    {
        foreach (var template in _templateManager.Templates)
        {
            DrawSingleTemplate($"root", template);
            Im.Line.Spacing();
            Im.Line.Spacing();
        }
    }

    private void DrawArmatures()
    {
        foreach (var armature in _armatureManager.Armatures)
        {
            DrawSingleArmature($"root", armature.Value);
            Im.Line.Spacing();
            Im.Line.Spacing();
        }
    }

    private void DrawObjectManager()
    {
        foreach (var kvPair in _objectManager)
        {
            var show = Im.Tree.Header($"{kvPair.Key} ({kvPair.Value.Objects.Count} objects [{kvPair.Value.Objects.Count(x => x.IsRenderedByGame())} rendered])###object-{kvPair.Key}");

            if (!show)
                continue;

            Im.Text($"ActorIdentifier");
            Im.Text($"PlayerName: {kvPair.Key.PlayerName.ToString()}");
            Im.Text($"HomeWorld: {kvPair.Key.HomeWorld}");
            Im.Text($"Retainer: {kvPair.Key.Retainer}");
            Im.Text($"Kind: {kvPair.Key.Kind}");
            Im.Text($"Data id: {kvPair.Key.DataId}");
            Im.Text($"Index: {kvPair.Key.Index.Index}");
            Im.Text($"Type: {kvPair.Key.Type}");
            Im.Text($"Special: {kvPair.Key.Special.ToString()}");
            Im.Text($"ToName: {kvPair.Key.ToName()}");
            Im.Text($"ToNameWithoutOwnerName: {kvPair.Key.ToNameWithoutOwnerName()}");
            (var actorIdentifier, var specialResult) = _gameObjectService.GetTrueActorForSpecialTypeActor(kvPair.Key);
            Im.Text($"True actor: {actorIdentifier.ToName()} ({specialResult})");

            Im.Line.Spacing();
            Im.Line.Spacing();

            Im.Text($"Objects");
            Im.Text($"Valid: {kvPair.Value.Valid}");
            Im.Text($"Label: {kvPair.Value.Label}");
            Im.Text($"Count: {kvPair.Value.Objects.Count}");
            foreach (var item in kvPair.Value.Objects)
            {
                Im.Text($"[{item.Index}] - {item}, valid: {item.Valid}, rendered: {item.IsRenderedByGame()}");
            }

            Im.Line.Spacing();
            Im.Line.Spacing();
        }
    }

    private void DrawSingleProfile(string prefix, Profile profile)
    {
        string name = profile.Name;
        string characterName = string.Join(',', profile.Characters.Select(x => x.ToNameWithoutOwnerName().Incognify()));

#if INCOGNIFY_STRINGS
        name = name.Incognify();
        //characterName = characterName.Incognify();
#endif

        var show = Im.Tree.Header($"[{(profile.Enabled ? "E" : "D")}] [P:{profile.Priority}] {name} on {characterName} [{profile.ProfileType}] [{profile.UniqueId}]###{prefix}-profile-{profile.UniqueId}");

        if (!show)
            return;

        Im.Text($"ID: {profile.UniqueId}");
        Im.Text($"Enabled: {(profile.Enabled ? "Enabled" : "Disabled")}");
        Im.Text($"State : {(profile.IsTemporary ? "Temporary" : "Permanent")}");
        var showTemplates = Im.Tree.Header($"Templates###{prefix}-profile-{profile.UniqueId}-templates");

        if (showTemplates)
        {
            foreach (var template in profile.Templates)
            {
                DrawSingleTemplate($"profile-{profile.UniqueId}", template, profile.DisabledTemplates.Contains(template.UniqueId) ? " [Disabled]" : null);
            }
        }

        if (profile.Armatures.Count > 0)
            foreach (var armature in profile.Armatures)
                DrawSingleArmature($"profile-{profile.UniqueId}", armature);
        else
            Im.Text("No armatures"u8);
    }

    private void DrawSingleTemplate(string prefix, Template template, string? additionalText = null)
    {
        string name = template.Name;

#if INCOGNIFY_STRINGS
        name = name.Incognify();
#endif

        var show = Im.Tree.Header($"{name} [{template.UniqueId}]{(additionalText != null ? additionalText : string.Empty)}###{prefix}-template-{template.UniqueId}");

        if (!show)
            return;

        Im.Text($"ID: {template.UniqueId}");

        Im.Text($"Bones:");
        foreach (var kvPair in template.Bones)
        {
#if !INCOGNIFY_STRINGS
            Im.Text($"{kvPair.Key}: p: {kvPair.Value.Translation} | r: {kvPair.Value.Rotation} | s: {kvPair.Value.Scaling} | cs: {kvPair.Value.ChildScaling}{(!kvPair.Value.ChildScalingIndependent ? " (link)" : string.Empty)}");
#else
            Im.Text($"{BoneData.GetBoneDisplayName(kvPair.Key)} ({kvPair.Key}): p: {(kvPair.Value.Translation.IsApproximately(Vector3.Zero) ? "Approx. not changed" : "Changed")} | r: {(kvPair.Value.Rotation.IsApproximately(Vector3.Zero) ? "Approx. not changed" : "Changed")} | s: {(kvPair.Value.Scaling.IsApproximately(Vector3.One) ? "Not changed" : "Changed")} | cs: {(!kvPair.Value.ChildScalingIndependent ? "Link" : (kvPair.Value.ChildScaling.IsApproximately(Vector3.One) ? "Not changed" : "Changed"))}");
#endif
        }
    }

    private void DrawSingleArmature(string prefix, Armature armature)
    {
        var show = Im.Tree.Header($"{armature} [{(armature.IsBuilt ? "Built" : "Not built")}, {(armature.IsVisible ? "Visible" : "Not visible")}]###{prefix}-armature-{armature.GetHashCode()}");

        if (!show)
            return;

        if (armature.IsBuilt)
        {
            Im.Text($"Total bones: {armature.TotalBoneCount}");
            Im.Text($"Partial skeletons: {armature.PartialSkeletonCount}");
            Im.Text($"Root bone: {armature.MainRootBone}");
        }

        Im.Text($"Profile: {armature.Profile.Name.Incognify()} ({armature.Profile.UniqueId})");
        Im.Text($"Actor: {armature.ActorIdentifier.IncognitoDebug()}");
        Im.Text($"Last seen: {armature.LastSeen} (UTC)");
        //Im.Text("Profile:");
        //DrawSingleProfile($"armature-{armature.GetHashCode()}", armature.Profile);

        var bindingsShow = Im.Tree.Header($"Bone template bindings ({armature.BoneTemplateBinding.Count})###{prefix}-armature-{armature.GetHashCode()}-bindings");

        if (bindingsShow)
        {
            foreach (var kvPair in armature.BoneTemplateBinding)
            {
                Im.Text($"{BoneData.GetBoneDisplayName(kvPair.Key)} ({kvPair.Key}) -> {kvPair.Value.Name.Incognify()} ({kvPair.Value.UniqueId})");
            }
        }

        var bonesShow = Im.Tree.Header($"Armature bones###{prefix}-armature-{armature.GetHashCode()}-bones");

        if (!bonesShow)
            return;

        var bones = armature.GetAllBones().ToList();
        Im.Text($"{bones.Count} bones");

        foreach (var bone in bones)
        {
            Im.Text($"{(bone.IsActive ? "[A] " : string.Empty)}{BoneData.GetBoneDisplayName(bone.BoneName)} [{bone.BoneName}] ({bone.PartialSkeletonIndex}-{bone.BoneIndex})");
        }
    }
}
