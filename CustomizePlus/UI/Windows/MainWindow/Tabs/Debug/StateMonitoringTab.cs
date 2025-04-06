using ImGuiNET;
using System.Linq;
using System;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Profiles;
using CustomizePlus.Armatures.Services;
using CustomizePlus.Templates;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Data;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.GameData.Services;
using CustomizePlus.Core.Extensions;
using System.Numerics;
using CustomizePlus.Game.Services;
using CustomizePlus.Core.Data;
using Penumbra.GameData.Interop;

namespace CustomizePlus.UI.Windows.MainWindow.Tabs.Debug;

public class StateMonitoringTab
{
    private readonly ProfileManager _profileManager;
    private readonly TemplateManager _templateManager;
    private readonly ArmatureManager _armatureManager;
    private readonly ActorObjectManager _objectManager;
    private readonly GameObjectService _gameObjectService;

    public StateMonitoringTab(
        ProfileManager profileManager,
        TemplateManager templateManager,
        ArmatureManager armatureManager,
        ActorObjectManager objectManager,
        GameObjectService gameObjectService)
    {
        _profileManager = profileManager;
        _templateManager = templateManager;
        _armatureManager = armatureManager;
        _objectManager = objectManager;
        _gameObjectService = gameObjectService;
    }

    public void Draw()
    {
        var showProfiles = ImGui.CollapsingHeader($"Profiles ({_profileManager.Profiles.Count})###profiles_header");

        if (showProfiles)
            DrawProfiles();

        var showTemplates = ImGui.CollapsingHeader($"Templates ({_templateManager.Templates.Count})###templates_header");

        if (showTemplates)
            DrawTemplates();

        var showArmatures = ImGui.CollapsingHeader($"Armatures ({_armatureManager.Armatures.Count})###armatures_header");

        if (showArmatures)
            DrawArmatures();

        var showObjectManager = ImGui.CollapsingHeader($"Object manager ({_objectManager.Count})###objectmanager_header");

        if (showObjectManager)
            DrawObjectManager();
    }

    private void DrawProfiles()
    {
        foreach (var profile in _profileManager.Profiles.OrderByDescending(x => x.Enabled).ThenByDescending(x => x.Priority))
        {
            DrawSingleProfile("root", profile);
            ImGui.Spacing();
            ImGui.Spacing();
        }
    }

    private void DrawTemplates()
    {
        foreach (var template in _templateManager.Templates)
        {
            DrawSingleTemplate($"root", template);
            ImGui.Spacing();
            ImGui.Spacing();
        }
    }

    private void DrawArmatures()
    {
        foreach (var armature in _armatureManager.Armatures)
        {
            DrawSingleArmature($"root", armature.Value);
            ImGui.Spacing();
            ImGui.Spacing();
        }
    }

    private void DrawObjectManager()
    {
        foreach (var kvPair in _objectManager)
        {
            var show = ImGui.CollapsingHeader($"{kvPair.Key} ({kvPair.Value.Objects.Count} objects)###object-{kvPair.Key}");

            if (!show)
                continue;

            ImGui.Text($"ActorIdentifier");
            ImGui.Text($"PlayerName: {kvPair.Key.PlayerName.ToString()}");
            ImGui.Text($"HomeWorld: {kvPair.Key.HomeWorld}");
            ImGui.Text($"Retainer: {kvPair.Key.Retainer}");
            ImGui.Text($"Kind: {kvPair.Key.Kind}");
            ImGui.Text($"Data id: {kvPair.Key.DataId}");
            ImGui.Text($"Index: {kvPair.Key.Index.Index}");
            ImGui.Text($"Type: {kvPair.Key.Type}");
            ImGui.Text($"Special: {kvPair.Key.Special.ToString()}");
            ImGui.Text($"ToName: {kvPair.Key.ToName()}");
            ImGui.Text($"ToNameWithoutOwnerName: {kvPair.Key.ToNameWithoutOwnerName()}");
            (var actorIdentifier, var specialResult) = _gameObjectService.GetTrueActorForSpecialTypeActor(kvPair.Key);
            ImGui.Text($"True actor: {actorIdentifier.ToName()} ({specialResult})");

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text($"Objects");
            ImGui.Text($"Valid: {kvPair.Value.Valid}");
            ImGui.Text($"Label: {kvPair.Value.Label}");
            ImGui.Text($"Count: {kvPair.Value.Objects.Count}");
            foreach (var item in kvPair.Value.Objects)
            {
                ImGui.Text($"[{item.Index}] - {item}, valid: {item.Valid}");
            }

            ImGui.Spacing();
            ImGui.Spacing();
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

        var show = ImGui.CollapsingHeader($"[{(profile.Enabled ? "E" : "D")}] [P:{profile.Priority}] {name} on {characterName} [{profile.ProfileType}] [{profile.UniqueId}]###{prefix}-profile-{profile.UniqueId}");

        if (!show)
            return;

        ImGui.Text($"ID: {profile.UniqueId}");
        ImGui.Text($"Enabled: {(profile.Enabled ? "Enabled" : "Disabled")}");
        ImGui.Text($"State : {(profile.IsTemporary ? "Temporary" : "Permanent")}");
        var showTemplates = ImGui.CollapsingHeader($"Templates###{prefix}-profile-{profile.UniqueId}-templates");

        if (showTemplates)
        {
            foreach (var template in profile.Templates)
            {
                DrawSingleTemplate($"profile-{profile.UniqueId}", template);
            }
        }

        if (profile.Armatures.Count > 0)
            foreach (var armature in profile.Armatures)
                DrawSingleArmature($"profile-{profile.UniqueId}", armature);
        else
            ImGui.Text("No armatures");
    }

    private void DrawSingleTemplate(string prefix, Template template)
    {
        string name = template.Name;

#if INCOGNIFY_STRINGS
        name = name.Incognify();
#endif

        var show = ImGui.CollapsingHeader($"{name} [{template.UniqueId}]###{prefix}-template-{template.UniqueId}");

        if (!show)
            return;

        ImGui.Text($"ID: {template.UniqueId}");

        ImGui.Text($"Bones:");
        foreach (var kvPair in template.Bones)
        {
#if !INCOGNIFY_STRINGS
            ImGui.Text($"{kvPair.Key}: p: {kvPair.Value.Translation} | r: {kvPair.Value.Rotation} | s: {kvPair.Value.Scaling}");
#else
            ImGui.Text($"{BoneData.GetBoneDisplayName(kvPair.Key)} ({kvPair.Key}): p: {(kvPair.Value.Translation.IsApproximately(Vector3.Zero) ? "Approx. not changed" : "Changed")} | r: {(kvPair.Value.Rotation.IsApproximately(Vector3.Zero) ? "Approx. not changed" : "Changed")} | s: {(kvPair.Value.Scaling.IsApproximately(Vector3.One) ? "Not changed" : "Changed")}");
#endif
        }
    }

    private void DrawSingleArmature(string prefix, Armature armature)
    {
        var show = ImGui.CollapsingHeader($"{armature} [{(armature.IsBuilt ? "Built" : "Not built")}, {(armature.IsVisible ? "Visible" : "Not visible")}]###{prefix}-armature-{armature.GetHashCode()}");

        if (!show)
            return;

        if (armature.IsBuilt)
        {
            ImGui.Text($"Total bones: {armature.TotalBoneCount}");
            ImGui.Text($"Partial skeletons: {armature.PartialSkeletonCount}");
            ImGui.Text($"Root bone: {armature.MainRootBone}");
        }

        ImGui.Text($"Profile: {armature.Profile.Name.Text.Incognify()} ({armature.Profile.UniqueId})");
        ImGui.Text($"Actor: {armature.ActorIdentifier.IncognitoDebug()}");
        ImGui.Text($"Last seen: {armature.LastSeen} (UTC)");
        //ImGui.Text("Profile:");
        //DrawSingleProfile($"armature-{armature.GetHashCode()}", armature.Profile);

        var bindingsShow = ImGui.CollapsingHeader($"Bone template bindings ({armature.BoneTemplateBinding.Count})###{prefix}-armature-{armature.GetHashCode()}-bindings");

        if (bindingsShow)
        {
            foreach (var kvPair in armature.BoneTemplateBinding)
            {
                ImGui.Text($"{BoneData.GetBoneDisplayName(kvPair.Key)} ({kvPair.Key}) -> {kvPair.Value.Name.Text.Incognify()} ({kvPair.Value.UniqueId})");
            }
        }

        var bonesShow = ImGui.CollapsingHeader($"Armature bones###{prefix}-armature-{armature.GetHashCode()}-bones");

        if (!bonesShow)
            return;

        var bones = armature.GetAllBones().ToList();
        ImGui.Text($"{bones.Count} bones");

        foreach (var bone in bones)
        {
            ImGui.Text($"{(bone.IsActive ? "[A] " : "")}{BoneData.GetBoneDisplayName(bone.BoneName)} [{bone.BoneName}] ({bone.PartialSkeletonIndex}-{bone.BoneIndex})");
        }
    }
}
