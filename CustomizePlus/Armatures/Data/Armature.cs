using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Penumbra.GameData.Actors;
using CustomizePlus.Core.Data;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Data;
using CustomizePlus.GameData.Extensions;

namespace CustomizePlus.Armatures.Data;

/// <summary>
/// Represents a "copy" of the ingame skeleton upon which the linked character profile is meant to operate.
/// Acts as an interface by which the in-game skeleton can be manipulated on a bone-by-bone basis.
/// </summary>
public unsafe class Armature
{
    /// <summary>
    /// Gets the Customize+ profile for which this mockup applies transformations.
    /// </summary>
    public Profile Profile { get; set; }

    /// <summary>
    /// Static identifier of the actor associated with this armature
    /// </summary>
    public ActorIdentifier ActorIdentifier { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether or not this armature has any renderable objects on which it should act.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Represents date and time when actor associated with this armature was last seen.
    /// Implemented mostly as a armature cleanup protection hack for mare and penumbra.
    /// </summary>
    public DateTime LastSeen { get; private set; }

    /// <summary>
    /// Gets a value indicating whether or not this armature has successfully built itself with bone information.
    /// </summary>
    public bool IsBuilt => _partialSkeletons.Any();

    /// <summary>
    /// Internal flag telling ArmatureManager that it should attempt to rebind profile to (another) profile whenever possible.
    /// </summary>
    public bool IsPendingProfileRebind { get; set; }

    /// <summary>
    /// For debugging purposes, each armature is assigned a globally-unique ID number upon creation.
    /// </summary>
    private static uint _nextGlobalId;
    private readonly uint _localId;

    /// <summary>
    /// Binding telling which bones are bound to each template for this armature. Built from template list in profile.
    /// </summary>
    public Dictionary<string, Template> BoneTemplateBinding { get; init; }

    /// <summary>
    /// Each skeleton is made up of several smaller "partial" skeletons.
    /// Each partial skeleton has its own list of bones, with a root bone at index zero.
    /// The root bone of a partial skeleton may also be a regular bone in a different partial skeleton.
    /// </summary>
    private ModelBone[][] _partialSkeletons;

    #region Bone Accessors -------------------------------------------------------------------------------

    /// <summary>
    /// Gets the number of partial skeletons contained in this armature.
    /// </summary>
    public int PartialSkeletonCount => _partialSkeletons.Length;

    /// <summary>
    /// Get the list of bones belonging to the partial skeleton at the given index.
    /// </summary>
    public ModelBone[] this[int i]
    {
        get => _partialSkeletons[i];
    }

    /// <summary>
    /// Returns the number of bones contained within the partial skeleton with the given index.
    /// </summary>
    public int GetBoneCountOfPartial(int partialIndex) => _partialSkeletons[partialIndex].Length;

    /// <summary>
    /// Get the bone at index 'j' within the partial skeleton at index 'i'.
    /// </summary>
    public ModelBone this[int i, int j]
    {
        get => _partialSkeletons[i][j];
    }

    /// <summary>
    /// Return the bone at the given indices, if it exists
    /// </summary>
    public ModelBone? GetBoneAt(int partialIndex, int boneIndex)
    {
        if (_partialSkeletons.Length > partialIndex
            && _partialSkeletons[partialIndex].Length > boneIndex)
        {
            return this[partialIndex, boneIndex];
        }

        return null;
    }

    /// <summary>
    /// Returns the root bone of the partial skeleton with the given index.
    /// </summary>
    public ModelBone GetRootBoneOfPartial(int partialIndex) => this[partialIndex, 0];

    public ModelBone MainRootBone => GetRootBoneOfPartial(0);

    /// <summary>
    /// Get the total number of bones in each partial skeleton combined.
    /// </summary>
    // In exactly one partial skeleton will the root bone be an independent bone. In all others, it's a reference to a separate, real bone.
    // For that reason we must subtract the number of duplicate bones
    public int TotalBoneCount => _partialSkeletons.Sum(x => x.Length);

    public IEnumerable<ModelBone> GetAllBones()
    {
        for (var i = 0; i < _partialSkeletons.Length; ++i)
        {
            for (var j = 0; j < _partialSkeletons[i].Length; ++j)
            {
                yield return this[i, j];
            }
        }
    }

    //----------------------------------------------------------------------------------------------------
    #endregion

    public Armature(ActorIdentifier actorIdentifier, Profile profile)
    {
        _localId = _nextGlobalId++;

        _partialSkeletons = Array.Empty<ModelBone[]>();

        BoneTemplateBinding = new Dictionary<string, Template>();

        ActorIdentifier = actorIdentifier;
        Profile = profile;
        IsVisible = false;

        UpdateLastSeen();

        Profile.Armatures.Add(this);

        Plugin.Logger.Debug($"Instantiated {this}, attached to {Profile}");
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return IsBuilt
            ? $"Armature (#{_localId}) on {ActorIdentifier.IncognitoDebug()} ({Profile}) with {TotalBoneCount} bone/s"
            : $"Armature (#{_localId}) on {ActorIdentifier.IncognitoDebug()} ({Profile}) with no skeleton reference";
    }

    public bool NewBonesAvailable(CharacterBase* cBase)
    {
        if (cBase == null)
        {
            return false;
        }
        else if (cBase->Skeleton->PartialSkeletonCount > _partialSkeletons.Length)
        {
            return true;
        }
        else
        {
            for (var i = 0; i < cBase->Skeleton->PartialSkeletonCount; ++i)
            {
                var newPose = cBase->Skeleton->PartialSkeletons[i].GetHavokPose(Constants.TruePoseIndex);
                if (newPose != null
                    && newPose->Skeleton->Bones.Length > _partialSkeletons[i].Length)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Rebuild the armature using the provided character base as a reference.
    /// </summary>
    public void RebuildSkeleton(CharacterBase* cBase)
    {
        if (cBase == null)
            return;

        var newPartials = ParseBonesFromObject(this, cBase);

        _partialSkeletons = newPartials.Select(x => x.ToArray()).ToArray();

        RebuildBoneTemplateBinding(); //todo: intentionally not calling ArmatureChanged.Type.Updated because this is pending rewrite

        Plugin.Logger.Debug($"Rebuilt {this}");
    }

    public void AugmentSkeleton(CharacterBase* cBase)
    {
        if (cBase == null)
            return;

        var oldPartials = _partialSkeletons.Select(x => x.ToList()).ToList();
        var newPartials = ParseBonesFromObject(this, cBase);

        //for each of the new partial skeletons discovered...
        for (var i = 0; i < newPartials.Count; ++i)
        {
            //if the old skeleton doesn't contain the new partial at all, add the whole thing
            if (i > oldPartials.Count)
            {
                oldPartials.Add(newPartials[i]);
            }
            //otherwise, add every model bone the new partial has that the old one doesn't
            else
            {
                //Case: get carbuncle, enable profile for it, turn carbuncle into human via glamourer
                if (oldPartials.Count <= i)
                    oldPartials.Add(new List<ModelBone>());

                for (var j = oldPartials[i].Count; j < newPartials[i].Count; ++j)
                {
                    oldPartials[i].Add(newPartials[i][j]);
                }
            }
        }

        _partialSkeletons = oldPartials.Select(x => x.ToArray()).ToArray();

        RebuildBoneTemplateBinding(); //todo: intentionally not calling ArmatureChanged.Type.Updated because this is pending rewrite

        Plugin.Logger.Debug($"Augmented {this} with new bones");
    }

    public BoneTransform? GetAppliedBoneTransform(string boneName)
    {
        if (BoneTemplateBinding.TryGetValue(boneName, out var template)
            && template != null)
        {
            if (template.Bones.TryGetValue(boneName, out var boneTransform))
                return boneTransform;
            else
                Plugin.Logger.Error($"Bone {boneName} is null in template {template.UniqueId}");
        }

        return null;
    }

    /// <summary>
    /// Update last time actor for this armature was last seen in the game
    /// </summary>
    public void UpdateLastSeen(DateTime? dateTime = null)
    {
        if(dateTime == null)
            dateTime = DateTime.UtcNow;

        LastSeen = (DateTime)dateTime;
    }

    private static unsafe List<List<ModelBone>> ParseBonesFromObject(Armature arm, CharacterBase* cBase)
    {
        List<List<ModelBone>> newPartials = new();

        try
        {
            //build the skeleton
            for (var pSkeleIndex = 0; pSkeleIndex < cBase->Skeleton->PartialSkeletonCount; ++pSkeleIndex)
            {
                var currentPartial = cBase->Skeleton->PartialSkeletons[pSkeleIndex];
                var currentPose = currentPartial.GetHavokPose(Constants.TruePoseIndex);

                newPartials.Add(new());

                if (currentPose == null)
                    continue;

                for (var boneIndex = 0; boneIndex < currentPose->Skeleton->Bones.Length; ++boneIndex)
                {
                    if (currentPose->Skeleton->Bones[boneIndex].Name.String is string boneName &&
                        boneName != null)
                    {
                        //time to build a new bone
                        ModelBone newBone = new(arm, boneName, pSkeleIndex, boneIndex);
                        Plugin.Logger.Verbose($"Created new bone: {boneName} on {pSkeleIndex}->{boneIndex} arm: {arm._localId}");

                        if (currentPose->Skeleton->ParentIndices[boneIndex] is short parentIndex
                            && parentIndex >= 0)
                        {
                            newBone.AddParent(pSkeleIndex, parentIndex);
                            newPartials[pSkeleIndex][parentIndex].AddChild(pSkeleIndex, boneIndex);
                        }

                        foreach (var mb in newPartials.SelectMany(x => x))
                        {
                            if (AreTwinnedNames(boneName, mb.BoneName))
                            {
                                newBone.AddTwin(mb.PartialSkeletonIndex, mb.BoneIndex);
                                mb.AddTwin(pSkeleIndex, boneIndex);
                                break;
                            }
                        }

                        //linking is performed later

                        newPartials.Last().Add(newBone);
                    }
                    else
                    {
                        Plugin.Logger.Error($"Failed to process bone @ <{pSkeleIndex}, {boneIndex}> while parsing bones from {cBase->ToString()}");
                    }
                }
            }

            BoneData.LogNewBones(newPartials.SelectMany(x => x.Select(y => y.BoneName)).ToArray());
        }
        catch (Exception ex)
        {
            Plugin.Logger.Error($"Error parsing armature skeleton from {cBase->ToString()}:\n\t{ex}");
        }

        return newPartials;
    }

    public void RebuildBoneTemplateBinding()
    {
        BoneTemplateBinding.Clear();

        foreach (var template in Profile.Templates)
        {
            foreach (var kvPair in template.Bones)
            {
                BoneTemplateBinding[kvPair.Key] = template;
            }
        }

        foreach (var bone in GetAllBones())
            bone.LinkToTemplate(BoneTemplateBinding.ContainsKey(bone.BoneName) ? BoneTemplateBinding[bone.BoneName] : null);

        Plugin.Logger.Debug($"Rebuilt template binding for armature {_localId}");
    }

    private static bool AreTwinnedNames(string name1, string name2)
    {
        return name1[^1] == 'r' ^ name2[^1] == 'r'
            && name1[^1] == 'l' ^ name2[^1] == 'l'
            && name1[0..^1] == name2[0..^1];
    }
}