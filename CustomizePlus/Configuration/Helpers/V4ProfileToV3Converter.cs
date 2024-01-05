using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Profiles.Data;
using System;
using System.Collections.Generic;

namespace CustomizePlus.Configuration.Helpers;

internal static class V4ProfileToV3Converter
{
    public static Version3Profile Convert(Profile v4Profile)
    {
        var profile = new Version3Profile
        {
            ProfileName = v4Profile.Name,
            CharacterName = v4Profile.CharacterName,
            CreationDate = v4Profile.CreationDate.DateTime,
            ModifiedDate = DateTime.UtcNow,
            Enabled = v4Profile.Enabled,
            OwnedOnly = v4Profile.LimitLookupToOwnedObjects,
            ConfigVersion = 3,
            Bones = new Dictionary<string, V3BoneTransform>()
        };

        foreach (var template in v4Profile.Templates)
        {
            foreach (var kvPair in template.Bones) //not super optimal but whatever
            {
                profile.Bones[kvPair.Key] = new V3BoneTransform
                {
                    Translation = kvPair.Value.Translation,
                    Rotation = kvPair.Value.Rotation,
                    Scaling = kvPair.Value.Scaling
                };
            }
        }

        return profile;
    }
}
