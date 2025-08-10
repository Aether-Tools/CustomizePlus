using CustomizePlusPlus.Configuration.Data.Version2;
using CustomizePlusPlus.Configuration.Data.Version3;
using CustomizePlusPlus.Core.Data;
using System.Numerics;

namespace CustomizePlusPlus.Configuration.Helpers;

internal static class V2ProfileToV3Converter
{
    public static Version3Profile Convert(Version2Profile v2Profile)
    {
        Version3Profile newProfile = new()
        {
            CharacterName = v2Profile.CharacterName,
            ProfileName = v2Profile.ScaleName,
            Enabled = v2Profile.BodyScaleEnabled
        };

        foreach (var kvp in v2Profile.Bones)
        {
            var novelValues = kvp.Value.Position != Vector3.Zero
                              || kvp.Value.Rotation != Vector3.Zero
                              || kvp.Value.Scale != Vector3.One;

            if (novelValues || kvp.Key == Constants.RootBoneName)
            {
                newProfile.Bones[kvp.Key] = new V3BoneTransform
                {
                    Translation = kvp.Value.Position,
                    Rotation = kvp.Value.Rotation,
                    Scaling = kvp.Value.Scale
                };
            }
        }

        return newProfile;
    }
}
