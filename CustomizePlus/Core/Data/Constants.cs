using FFXIVClientStructs.Havok;
using FFXIVClientStructs.Havok.Common.Base.Math.QsTransform;
using FFXIVClientStructs.Havok.Common.Base.Math.Quaternion;
using FFXIVClientStructs.Havok.Common.Base.Math.Vector;
using System.Numerics;

namespace CustomizePlus.Core.Data;

internal static class Constants
{
    /// <summary>
    /// Version of the configuration file, when increased a converter should be implemented if necessary.
    /// </summary>
    public const int ConfigurationVersion = 4;

    /// <summary>
    /// The name of the root bone.
    /// </summary>
    public const string RootBoneName = "n_root";

    /// <summary>
    /// Minimum allowed value for any of the vector values.
    /// </summary>
    public const int MinVectorValueLimit = -512;

    /// <summary>
    /// Maximum allowed value for any of the vector values.
    /// </summary>
    public const int MaxVectorValueLimit = 512;

    /// <summary>
    /// Predicate function for determining if the given object table index represents an
    /// NPC in a busy area (i.e. there are ~245 other objects already).
    /// </summary>
    public static bool IsInObjectTableBusyNPCRange(int index) => index > 245;

    /// <summary>
    /// A "null" havok vector. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
    /// is valid input in a lot of cases, we can use this instead.
    /// </summary>
    public static readonly hkVector4f NullVector = new()
    {
        X = float.NaN,
        Y = float.NaN,
        Z = float.NaN,
        W = float.NaN
    };

    /// <summary>
    /// A "null" havok quaternion. Since the type isn't inherently nullable, and the default value (0, 0, 0, 0)
    /// is valid input in a lot of cases, we can use this instead.
    /// </summary>
    public static readonly hkQuaternionf NullQuaternion = new()
    {
        X = float.NaN,
        Y = float.NaN,
        Z = float.NaN,
        W = float.NaN
    };

    /// <summary>
    /// A "null" havok transform. Since the type isn't inherently nullable, and the default values
    /// aren't immediately obviously wrong, we can use this instead.
    /// </summary>
    public static readonly hkQsTransformf NullTransform = new()
    {
        Translation = NullVector,
        Rotation = NullQuaternion,
        Scale = NullVector
    };

    /// <summary>
    /// The pose at index 0 is the only one we apparently need to care about.
    /// </summary>
    public const int TruePoseIndex = 0;

    /// <summary>
    /// Main render hook address
    /// </summary>
    public const string RenderHookAddress = "E8 ?? ?? ?? ?? 48 81 C3 ?? ?? ?? ?? BF ?? ?? ?? ?? 33 ED";

    /// <summary>
    /// Movement hook address, used for position offset and other changes which cannot be done in main hook
    /// </summary>
    public const string MovementHookAddress = "E8 ?? ?? ?? ?? 84 DB 74 45";

    internal static class Colors
    {
        public static Vector4 Normal = new Vector4(1, 1, 1, 1);
        public static Vector4 Info = new Vector4(0.3f, 0.5f, 1f, 1);
        public static Vector4 Warning = new Vector4(1, 0.5f, 0, 1);
        public static Vector4 Error = new Vector4(1, 0, 0, 1);
    }
}