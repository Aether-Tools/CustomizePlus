using CustomizePlus.Core.Data;
using System;
using System.Collections.Generic;

namespace CustomizePlus.Configuration.Data.Version3;

/// <summary>
///     Encapsulates the user-controlled aspects of a character profile, ie all of
///     the information that gets saved to disk by the plugin.
/// </summary>
[Serializable]
public sealed class Version3Profile
{
    public string CharacterName { get; set; } = "Default";
    public string ProfileName { get; set; } = "Profile";
    public nint? Address { get; set; } = null;
    public bool OwnedOnly { get; set; } = false;
    public int ConfigVersion { get; set; } = Constants.ConfigurationVersion;
    public bool Enabled { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;

    public Dictionary<string, V3BoneTransform> Bones { get; init; } = new();
}
