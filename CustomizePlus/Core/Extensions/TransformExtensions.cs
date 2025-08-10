using System;
using System.Numerics;
using CustomizePlusPlus.Core.Data;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

//using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace CustomizePlusPlus.Core.Extensions;

internal static class TransformExtensions
{
    public static bool Equals(this hkQsTransformf first, hkQsTransformf second)
    {
        return first.Translation.Equals(second.Translation)
               && first.Rotation.Equals(second.Rotation)
               && first.Scale.Equals(second.Scale);
    }

    public static bool IsNull(this hkQsTransformf t)
    {
        return t.Equals(Constants.NullTransform);
    }

    public static hkQsTransformf ToHavokTransform(this BoneTransform bt)
    {
        return new hkQsTransformf
        {
            Translation = bt.Translation.ToHavokTranslation(),
            Rotation = bt.Rotation.ToQuaternion().ToHavokRotation(),
            Scale = bt.Scaling.ToHavokScaling()
        };
    }

    public static BoneTransform ToBoneTransform(this hkQsTransformf t)
    {
        var rotVec = Quaternion.Divide(t.Translation.ToQuaternion(), t.Rotation.ToQuaternion());

        return new BoneTransform
        {
            Translation = new Vector3(rotVec.X / rotVec.W, rotVec.Y / rotVec.W, rotVec.Z / rotVec.W),
            Rotation = t.Rotation.ToQuaternion().ToEulerAngles(),
            Scaling = new Vector3(t.Scale.X, t.Scale.Y, t.Scale.Z)
        };
    }

    public static hkVector4f GetAttribute(this hkQsTransformf t, BoneAttribute att)
    {
        return att switch
        {
            BoneAttribute.Position => t.Translation,
            BoneAttribute.Rotation => t.Rotation.ToQuaternion().GetAsNumericsVector().ToHavokVector(),
            BoneAttribute.Scale => t.Scale,
            _ => throw new NotImplementedException()
        };
    }
}