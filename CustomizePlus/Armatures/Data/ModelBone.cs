using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Templates.Data;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Animation.Rig;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using OtterGui.Text.EndObjects;
using Penumbra.GameData;
using static CustomizePlus.Anamnesis.Data.PoseFile;
using static FFXIVClientStructs.Havok.Animation.Rig.hkaPose;

namespace CustomizePlus.Armatures.Data;

/// <summary>
///     Represents a single bone of an ingame character's skeleton.
/// </summary>
public unsafe class ModelBone
{
    public enum PoseType
    {
        Local, Model, BindPose, World
    }

    public readonly Armature MasterArmature;

    public readonly int PartialSkeletonIndex;
    public readonly int BoneIndex;

    /// <summary>
    /// Gets the model bone corresponding to this model bone's parent, if it exists.
    /// (It should in all cases but the root of the skeleton)
    /// </summary>
    public ModelBone? ParentBone => _parentPartialIndex >= 0 && _parentBoneIndex >= 0
        ? MasterArmature[_parentPartialIndex, _parentBoneIndex]
        : null;
    private int _parentPartialIndex = -1;
    private int _parentBoneIndex = -1;

    /// <summary>
    /// Gets each model bone for which this model bone corresponds to a direct parent thereof.
    /// A model bone may have zero children.
    /// </summary>
    public IEnumerable<ModelBone> ChildBones => _childPartialIndices.Zip(_childBoneIndices, (x, y) => MasterArmature[x, y]);
    public IEnumerable<ModelBone> GetDescendants()
    {
        var list = ChildBones.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            list.AddRange(list[i].ChildBones.ToList());
        }
        return list;
    }
    private List<int> _childPartialIndices = new();
    private List<int> _childBoneIndices = new();

    /// <summary>
    /// Gets the model bone that forms a mirror image of this model bone, if one exists.
    /// </summary>
    public ModelBone? TwinBone => _twinPartialIndex >= 0 && _twinBoneIndex >= 0
        ? MasterArmature[_twinPartialIndex, _twinBoneIndex]
        : null;
    private int _twinPartialIndex = -1;
    private int _twinBoneIndex = -1;

    /// <summary>
    /// The name of the bone within the in-game skeleton. Referred to in some places as its "code name".
    /// </summary>
    public string BoneName;

    /// <summary>
    /// The transform that this model bone will impart upon its in-game sibling when the master armature
    /// is applied to the in-game skeleton. Reference to transform contained in top most template in profile applied to character.
    /// </summary>
    public BoneTransform? CustomizedTransform { get; private set; }

    /// <summary>
    /// True if bone is linked to any template
    /// </summary>
    public bool IsActive => CustomizedTransform != null;

    public ModelBone(Armature arm, string codeName, int partialIdx, int boneIdx)
    {
        MasterArmature = arm;
        PartialSkeletonIndex = partialIdx;
        BoneIndex = boneIdx;

        BoneName = codeName;
    }

    /// <summary>
    /// Link bone to specific template, unlinks if null is passed
    /// </summary>
    /// <param name="template"></param>
    /// <returns></returns>
    public bool LinkToTemplate(Template? template)
    {
        if (template == null)
        {
            if (CustomizedTransform == null)
                return false;

            CustomizedTransform = null;

            Plugin.Logger.Verbose($"Unlinked {BoneName} from all templates");

            return true;
        }

        if (!template.Bones.ContainsKey(BoneName))
            return false;

        Plugin.Logger.Verbose($"Linking {BoneName} to {template.Name}");
        CustomizedTransform = template.Bones[BoneName];

        return true;
    }

    /// <summary>
    /// Indicate a bone to act as this model bone's "parent".
    /// </summary>
    public void AddParent(int parentPartialIdx, int parentBoneIdx)
    {
        if (_parentPartialIndex != -1 || _parentBoneIndex != -1)
        {
            throw new Exception($"Tried to add redundant parent to model bone -- {this}");
        }

        _parentPartialIndex = parentPartialIdx;
        _parentBoneIndex = parentBoneIdx;
    }

    /// <summary>
    /// Indicate that a bone is one of this model bone's "children".
    /// </summary>
    public void AddChild(int childPartialIdx, int childBoneIdx)
    {
        _childPartialIndices.Add(childPartialIdx);
        _childBoneIndices.Add(childBoneIdx);
    }

    /// <summary>
    /// Indicate a bone that acts as this model bone's mirror image, or "twin".
    /// </summary>
    public void AddTwin(int twinPartialIdx, int twinBoneIdx)
    {
        _twinPartialIndex = twinPartialIdx;
        _twinBoneIndex = twinBoneIdx;
    }

    public override string ToString()
    {
        //string numCopies = _copyIndices.Count > 0 ? $" ({_copyIndices.Count} copies)" : string.Empty;
        return $"{BoneName} ({BoneData.GetBoneDisplayName(BoneName)}) @ <{PartialSkeletonIndex}, {BoneIndex}>";
    }

    /// <summary>
    /// Get the lineage of this model bone, going back to the skeleton's root bone.
    /// </summary>
    public IEnumerable<ModelBone> GetAncestors(bool includeSelf = true) => includeSelf
        ? GetAncestors(new List<ModelBone>() { this })
        : GetAncestors(new List<ModelBone>());

    private IEnumerable<ModelBone> GetAncestors(List<ModelBone> tail)
    {
        tail.Add(this);
        if (ParentBone is ModelBone mb && mb != null)
        {
            return mb.GetAncestors(tail);
        }
        else
        {
            return tail;
        }
    }

    /// <summary>
    /// Gets all model bones with a lineage that contains this one.
    /// </summary>
    public IEnumerable<ModelBone> GetDescendants(bool includeSelf = false) => includeSelf
        ? GetDescendants(this)
        : GetDescendants(null);

    private IEnumerable<ModelBone> GetDescendants(ModelBone? first)
    {
        var output = first != null
            ? new List<ModelBone>() { first }
            : new List<ModelBone>();

        output.AddRange(ChildBones);

        using (var iter = output.GetEnumerator())
        {
            while (iter.MoveNext())
            {
                output.AddRange(iter.Current.ChildBones);
                yield return iter.Current;
            }
        }
    }

    /// <summary>
    /// Given a character base to which this model bone's master armature (presumably) applies,
    /// return the game's transform value for this model's in-game sibling within the given reference frame.
    /// </summary>
    public hkQsTransformf GetGameTransform(CharacterBase* cBase, PoseType refFrame)
    {

        var skelly = cBase->Skeleton;
        var pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
        var targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
        //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

        if (targetPose == null) return Constants.NullTransform;

        if (BoneIndex >= targetPose->Skeleton->Bones.Length) return Constants.NullTransform;

        return refFrame switch
        {
            PoseType.Local => targetPose->LocalPose[BoneIndex],
            PoseType.Model => targetPose->ModelPose[BoneIndex],
            _ => Constants.NullTransform
            //TODO properly implement the other options
        };
    }

    public hkQsTransformf* GetGameTransformAccess(CharacterBase* cBase, PoseType refFrame)
    {

        var skelly = cBase->Skeleton;
        var pSkelly = skelly->PartialSkeletons[PartialSkeletonIndex];
        var targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
        //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

        if (targetPose == null)
            return null;

        // It's really gonna crash without it, skeleton changes aren't getting picked up fast enough
        if (BoneIndex >= targetPose->Skeleton->Bones.Length) return null;

        return refFrame switch
        {
            PoseType.Local => targetPose->AccessBoneLocalSpace(BoneIndex),
            PoseType.Model => targetPose->AccessBoneModelSpace(BoneIndex, PropagateOrNot.DontPropagate),
            _ => null
            //TODO properly implement the other options
        }; ;
    }

    private void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, PoseType refFrame)
    {
        SetGameTransform(cBase, transform, PartialSkeletonIndex, BoneIndex, refFrame);
    }

    private static void SetGameTransform(CharacterBase* cBase, hkQsTransformf transform, int partialIndex, int boneIndex, PoseType refFrame)
    {
        var skelly = cBase->Skeleton;
        var pSkelly = skelly->PartialSkeletons[partialIndex];
        var targetPose = pSkelly.GetHavokPose(Constants.TruePoseIndex);
        //hkaPose* targetPose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);

        if (targetPose == null || targetPose->ModelInSync == 0) return;

        switch (refFrame)
        {
            case PoseType.Local:
                targetPose->LocalPose.Data[boneIndex] = transform;
                return;

            case PoseType.Model:
                targetPose->ModelPose.Data[boneIndex] = transform;
                return;

            default:
                return;

                //TODO properly implement the other options
        }
    }

    /// <summary>
    /// Apply this model bone's associated transformation to its in-game sibling within
    /// the skeleton of the given character base.
    /// </summary>
    public void ApplyModelTransform(CharacterBase* cBase)
    {
        if (!IsActive)
            return;

        if (cBase == null || CustomizedTransform == null || !CustomizedTransform.IsEdited())
            return;

        var doPropagate = CustomizedTransform.PropagateTranslation ||
                          CustomizedTransform.PropagateRotation ||
                          CustomizedTransform.PropagateScale;

        if (!doPropagate)
        {
            var gameTransform = GetGameTransform(cBase, PoseType.Model);
            if (!gameTransform.Equals(Constants.NullTransform))
            {
                var modify_Transform = CustomizedTransform.ModifyExistingTransform(gameTransform);
                if (!modify_Transform.Equals(Constants.NullTransform))
                {
                    SetGameTransform(cBase, modify_Transform, PoseType.Model);
                }
            }

            return;
        }

        var gameTransformAccess = GetGameTransformAccess(cBase, PoseType.Model);
        if (gameTransformAccess == null)
            return;

        var initialPos = gameTransformAccess->Translation.ToVector3();
        var initialRot = gameTransformAccess->Rotation.ToQuaternion();
        var initialScale = gameTransformAccess->Scale.ToVector3();

        var modTransform = CustomizedTransform.ModifyExistingTransform(*gameTransformAccess);
        SetGameTransform(cBase, modTransform, PoseType.Model);

        var pose = cBase->Skeleton->PartialSkeletons[PartialSkeletonIndex].GetHavokPose(Constants.TruePoseIndex);
        if (pose == null || pose->ModelInSync == 0)
            return;

        var access2 = GetGameTransformAccess(cBase, PoseType.Model);
        if (access2 == null)
            return;

        var childScaleToUse = access2->Scale.ToVector3();

        if (CustomizedTransform.ChildScalingIndependent)
        {
            childScaleToUse = new Vector3(
                initialScale.X * CustomizedTransform.ChildScaling.X,
                initialScale.Y * CustomizedTransform.ChildScaling.Y,
                initialScale.Z * CustomizedTransform.ChildScaling.Z
            );
        }

        var shouldPropagateScale = CustomizedTransform.PropagateScale &&
            (!CustomizedTransform.Scaling.Equals(Vector3.One) ||
             (CustomizedTransform.ChildScalingIndependent && !CustomizedTransform.ChildScaling.Equals(Vector3.One)));

        PropagateChildren(cBase, access2, initialPos, initialRot, initialScale,
            CustomizedTransform.PropagateTranslation && !CustomizedTransform.Translation.Equals(Vector3.Zero),
            CustomizedTransform.PropagateRotation && !CustomizedTransform.Rotation.Equals(Vector3.Zero),
            shouldPropagateScale,
            childScaleToUse);
    }


    public unsafe void PropagateChildren(CharacterBase* cBase, hkQsTransformf* transform, Vector3 initialPos, Quaternion initialRot, Vector3 initialScale, bool propagateTranslation, bool propagateRotation, bool propagateScale, Vector3 childScale, bool includePartials = true)
    {
        // Bone parenting
        // Adapted from Anamnesis Studio code shared by Yuki - thank you!

        // Original Parent Bone position after it had its offsets applied
        var sourcePos = transform->Translation.ToVector3();

        var deltaRot = transform->Rotation.ToQuaternion() / initialRot;
        var deltaPos = sourcePos - initialPos;
        var deltaScale = childScale / initialScale;

        foreach (var child in GetDescendants())
        {
            // Plugin.Logger.Debug($"Propagating to {child.BoneName}...");
            var access = child.GetGameTransformAccess(cBase, PoseType.Model);
            if (access != null)
            {

                var offset = access->Translation.ToVector3() - sourcePos;

                var matrix = InteropAlloc.GetMatrix(access);
                if (propagateScale)
                {
                    var scaleMatrix = Matrix4x4.CreateScale(deltaScale, Vector3.Zero);
                    matrix *= scaleMatrix;
                    offset = Vector3.Transform(offset, scaleMatrix);
                }
                if (propagateRotation)
                {
                    matrix *= Matrix4x4.CreateFromQuaternion(deltaRot);
                    offset = Vector3.Transform(offset, deltaRot);
                }
                matrix.Translation = deltaPos + sourcePos + offset;
                InteropAlloc.SetMatrix(access, matrix);
            }
        }
    }

    public void ApplyModelScale(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingScale);
    public void ApplyModelRotation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingRotation);
    public void ApplyModelFullTranslation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslationWithRotation);
    public void ApplyStraightModelTranslation(CharacterBase* cBase) => ApplyTransFunc(cBase, CustomizedTransform.ModifyExistingTranslation);

    private void ApplyTransFunc(CharacterBase* cBase, Func<hkQsTransformf, hkQsTransformf> modTrans)
    {
        if (!IsActive)
            return;

        if (cBase != null
            && CustomizedTransform.IsEdited()
            && GetGameTransform(cBase, PoseType.Model) is hkQsTransformf gameTransform
            && !gameTransform.Equals(Constants.NullTransform))
        {
            var modTransform = modTrans(gameTransform);

            if (!modTransform.Equals(gameTransform) && !modTransform.Equals(Constants.NullTransform))
            {
                SetGameTransform(cBase, modTransform, PoseType.Model);
            }
        }
    }


    /// <summary>
    /// Checks for a non-zero and non-identity (root) scale.
    /// </summary>
    /// <param name="mb">The bone to check</param>
    /// <returns>If the scale should be applied.</returns>
    public bool IsModifiedScale()
    {
        if (!IsActive)
            return false;
        return CustomizedTransform.Scaling.X != 0 && CustomizedTransform.Scaling.X != 1 ||
               CustomizedTransform.Scaling.Y != 0 && CustomizedTransform.Scaling.Y != 1 ||
               CustomizedTransform.Scaling.Z != 0 && CustomizedTransform.Scaling.Z != 1;
    }
}