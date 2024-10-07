using CustomizePlus.Configuration.Data.Version3;
using CustomizePlus.Core.Data;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Data;
using System;
using System.Collections.Generic;

namespace CustomizePlus.Configuration.Helpers;

internal static class V3ProfileToV4Converter
{
    public static (Profile, Template) Convert(Version3Profile v3Profile)
    {
        var profile = new Profile
        {
            Name = $"{v3Profile.ProfileName} - {v3Profile.CharacterName}",
            CharacterName = v3Profile.CharacterName,
            CreationDate = v3Profile.CreationDate,
            ModifiedDate = DateTimeOffset.UtcNow,
            Enabled = v3Profile.Enabled,
            UniqueId = Guid.NewGuid(),
            Templates = new List<Template>(1)
        };

        var template = new Template
        {
            Name = $"{profile.Name}'s template",
            CreationDate = profile.CreationDate,
            ModifiedDate = profile.ModifiedDate,
            UniqueId = Guid.NewGuid(),
            Bones = new Dictionary<string, BoneTransform>(v3Profile.Bones.Count)
        };

        foreach (var kvPair in v3Profile.Bones)
            template.Bones.Add(kvPair.Key,
                new BoneTransform { Translation = kvPair.Value.Translation, Rotation = kvPair.Value.Rotation, Scaling = kvPair.Value.Scaling });

        profile.Templates.Add(template);

        return (profile, template);
    }
}
