using System;
using System.Numerics;
using CustomizePlus.Anamnesis.Data;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;

namespace CustomizePlus.Core.Extensions;

internal static class VectorExtensions
{
    public static bool IsApproximately(this hkVector4f vector, Vector3 other, float errorMargin = 0.001f)
    {
        return IsApproximately(vector.X, other.X, errorMargin)
               && IsApproximately(vector.Y, other.Y, errorMargin)
               && IsApproximately(vector.Z, other.Z, errorMargin);
    }

    public static bool IsApproximately(this Vector3 vector, Vector3 other, float errorMargin = 0.001f)
    {
        return IsApproximately(vector.X, other.X, errorMargin)
               && IsApproximately(vector.Y, other.Y, errorMargin)
               && IsApproximately(vector.Z, other.Z, errorMargin);
    }

    private static bool IsApproximately(float a, float b, float errorMargin)
    {
        var d = MathF.Abs(a - b);
        return d < errorMargin;
    }

    public static Vector3 ToVector3(this hkVector4f vec) => new Vector3(vec.X, vec.Y, vec.Z);

    public static Quaternion ToQuaternion(this Vector3 rotation)
    {
        return Quaternion.CreateFromYawPitchRoll(
            rotation.X * MathF.PI / 180,
            rotation.Y * MathF.PI / 180,
            rotation.Z * MathF.PI / 180);
    }

    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        var nq = Vector4.Normalize(q.GetAsNumericsVector());

        var rollX = MathF.Atan2(
            2 * (nq.W * nq.X + nq.Y * nq.Z),
            1 - 2 * (nq.X * nq.X + nq.Y * nq.Y));

        var pitchY = 2 * MathF.Atan2(
            MathF.Sqrt(1 + 2 * (nq.W * nq.Y - nq.X * nq.Z)),
            MathF.Sqrt(1 - 2 * (nq.W * nq.Y - nq.X * nq.Z)));

        var yawZ = MathF.Atan2(
            2 * (nq.W * nq.Z + nq.X * nq.Y),
            1 - 2 * (nq.Y * nq.Y + nq.Z * nq.Z));

        return new Vector3(rollX, pitchY, yawZ);
    }

    public static Quaternion ToQuaternion(this Vector4 rotation)
    {
        return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
    }

    public static Quaternion ToQuaternion(this hkQuaternionf rotation)
    {
        return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
    }

    public static Quaternion ToQuaternion(this hkVector4f rotation)
    {
        return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
    }


    public static hkQuaternionf ToHavokRotation(this Quaternion rotation)
    {
        return new hkQuaternionf
        {
            X = rotation.X,
            Y = rotation.Y,
            Z = rotation.Z,
            W = rotation.W
        };
    }

    public static hkVector4f ToHavokTranslation(this Vector3 translation)
    {
        return new hkVector4f
        {
            X = translation.X,
            Y = translation.Y,
            Z = translation.Z,
            W = 0.0f
        };
    }

    public static hkVector4f ToHavokScaling(this Vector3 scaling)
    {
        return new hkVector4f
        {
            X = scaling.X,
            Y = scaling.Y,
            Z = scaling.Z,
            W = 1.0f
        };
    }

    public static hkVector4f ToHavokVector(this Vector4 vec)
    {
        return new hkVector4f
        {
            X = vec.X,
            Y = vec.Y,
            Z = vec.Z,
            W = vec.W
        };
    }

    public static Vector3 GetAsNumericsVector(this PoseFile.Vector vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static Vector4 GetAsNumericsVector(this hkVector4f vec)
    {
        return new Vector4(vec.X, vec.Y, vec.Z, vec.W);
    }

    public static Vector4 GetAsNumericsVector(this Quaternion q)
    {
        return new Vector4(q.X, q.Y, q.Z, q.W);
    }

    public static Vector3 RemoveWTerm(this Vector4 vec)
    {
        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    public static bool Equals(this hkVector4f first, hkVector4f second)
    {
        return first.X == second.X
               && first.Y == second.Y
               && first.Z == second.Z
               && first.W == second.W;
    }
}