using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Core.Data;
using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Api.Data;

/// <summary>
/// Bare essentials version of character profile
/// </summary>
public class IPCCharacterProfile
{
    public Dictionary<string, IPCBoneTransform> Bones { get; init; } = new();

    public static IPCCharacterProfile FromFullProfile(Profile profile)
    {
        var ipcProfile = new IPCCharacterProfile();

        foreach (var template in profile.Templates)
        {
            foreach (var kvPair in template.Bones) //not super optimal but whatever
            {
                ipcProfile.Bones[kvPair.Key] = new IPCBoneTransform
                {
                    Translation = kvPair.Value.Translation,
                    Rotation = kvPair.Value.Rotation,
                    Scaling = kvPair.Value.Scaling
                };
            }
        }

        return ipcProfile;
    }

    public static (Profile, Template) ToFullProfile(IPCCharacterProfile profile, bool isTemporary = true)
    {
        var fullProfile = new Profile
        {
            //Character should be set manually
            CreationDate = DateTimeOffset.UtcNow,
            ModifiedDate = DateTimeOffset.UtcNow,
            Enabled = true,
            UniqueId = Guid.NewGuid(),
            Templates = new List<Template>(1),
            ProfileType = isTemporary ? Profiles.Enums.ProfileType.Temporary : Profiles.Enums.ProfileType.Normal
        };

        fullProfile.Name = $"IPC Profile {fullProfile.UniqueId}";

        var template = new Template
        {
            Name = $"{fullProfile.Name} - template",
            CreationDate = fullProfile.CreationDate,
            ModifiedDate = fullProfile.ModifiedDate,
            UniqueId = Guid.NewGuid(),
            Bones = new Dictionary<string, BoneTransform>(profile.Bones.Count)
        };

        foreach (var kvPair in profile.Bones)
            template.Bones.Add(kvPair.Key,
                new BoneTransform { Translation = kvPair.Value.Translation, Rotation = kvPair.Value.Rotation, Scaling = kvPair.Value.Scaling });

        fullProfile.Templates.Add(template);

        return (fullProfile, template);
    }
}

public class IPCBoneTransform
{
    private Vector3 _translation;
    public Vector3 Translation
    {
        get => _translation;
        set => _translation = ClampVector(value);
    }

    private Vector3 _rotation;
    public Vector3 Rotation
    {
        get => _rotation;
        set => _rotation = ClampAngles(value);
    }

    private Vector3 _scaling;
    public Vector3 Scaling
    {
        get => _scaling;
        set => _scaling = ClampVector(value);
    }

    /// <summary>
    /// Reserved for future use
    /// </summary>
    public bool PropagateTranslation { get; set; }

    /// <summary>
    /// Reserved for future use
    /// </summary>
    public bool PropagateRotation { get; set; }

    /// <summary>
    /// Reserved for future use
    /// </summary>
    public bool PropagateScale { get; set; }

    /// <summary>
    /// Clamp all vector values to be within allowed limits.
    /// </summary>
    private Vector3 ClampVector(Vector3 vector)
    {
        return new Vector3
        {
            X = Math.Clamp(vector.X, -512, 512),
            Y = Math.Clamp(vector.Y, -512, 512),
            Z = Math.Clamp(vector.Z, -512, 512)
        };
    }

    private static Vector3 ClampAngles(Vector3 rotVec)
    {
        static float Clamp(float angle)
        {
            if (angle > 180)
                angle -= 360;
            else if (angle < -180)
                angle += 360;

            return angle;
        }

        rotVec.X = Clamp(rotVec.X);
        rotVec.Y = Clamp(rotVec.Y);
        rotVec.Z = Clamp(rotVec.Z);

        return rotVec;
    }
}