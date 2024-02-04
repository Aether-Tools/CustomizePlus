using System;
using System.Collections.Generic;

namespace CustomizePlus.Configuration.Data.Version2;

[Serializable]
public class Version2Profile
{
    public static Dictionary<string, bool> BoneVisibility = new();
    public string CharacterName { get; set; } = string.Empty;
    public string ScaleName { get; set; } = string.Empty;
    public bool BodyScaleEnabled { get; set; } = true;
    public Dictionary<string, V2BoneEditsContainer> Bones { get; set; } = new();
}
