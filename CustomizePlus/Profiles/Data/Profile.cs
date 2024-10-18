using System;
using System.Collections.Generic;
using System.IO;
using CustomizePlus.Armatures.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Enums;
using CustomizePlus.Templates;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using Penumbra.GameData.Actors;

namespace CustomizePlus.Profiles.Data;

/// <summary>
///     Encapsulates the user-controlled aspects of a character profile, ie all of
///     the information that gets saved to disk by the plugin.
/// </summary>
public sealed class Profile : ISavable
{
    public const int Version = 5;

    private static int _nextGlobalId;

    private readonly int _localId;

    public List<Armature> Armatures = new();

    [Obsolete("To be removed in the future versions")]
    public LowerString CharacterName { get; set; } = LowerString.Empty;

    public ActorIdentifier Character { get; set; } = ActorIdentifier.Invalid;

    public LowerString Name { get; set; } = LowerString.Empty;

    /// <summary>
    /// Whether to search only through local player owned characters or all characters when searching for game object by name
    /// </summary>
    //public bool LimitLookupToOwnedObjects { get; set; } = false;

    public bool Enabled { get; set; }
    public DateTimeOffset CreationDate { get; set; } = DateTime.UtcNow;
    public DateTimeOffset ModifiedDate { get; set; } = DateTime.UtcNow;

    public Guid UniqueId { get; set; } = Guid.NewGuid();

    public List<Template> Templates { get; init; } = new();

    public bool IsWriteProtected { get; internal set; }

    public ProfileType ProfileType { get; set; }

    /// <summary>
    /// Tells us if this profile is not persistent (ex. was made via IPC calls) and should have specific treatement like not being shown in UI, etc.
    /// WARNING, TEMPLATES FOR TEMPORARY PROFILES *ARE NOT* STORED IN TemplateManager
    /// </summary>
    public bool IsTemporary => ProfileType == ProfileType.Temporary;

   /* /// <summary>
    /// Identificator specifying specific actor this profile applies to, only works for temporary profiles
    /// </summary>
    public ActorIdentifier TemporaryActor { get; set; } = ActorIdentifier.Invalid;*/

    public string Incognito
        => UniqueId.ToString()[..8];

    public Profile()
    {
        _localId = _nextGlobalId++;
    }

    /// <summary>
    /// Creates a new profile based on data from another one
    /// </summary>
    /// <param name="original"></param>
    public Profile(Profile original) : this()
    {
        Character = original.Character;

        foreach (var template in original.Templates)
        {
            Templates.Add(template);
        }
    }

    public override string ToString()
    {
        return $"Profile '{Name.Text.Incognify()}' on {Character.Incognito(null)} [{UniqueId}]";
    }

    #region Serialization

    public new JObject JsonSerialize()
    {
        var ret = new JObject()
        {
            ["Version"] = Version,
            ["UniqueId"] = UniqueId,
            ["CreationDate"] = CreationDate,
            ["ModifiedDate"] = ModifiedDate,
            ["CharacterName"] = CharacterName.Text,
            ["Character"] = Character.ToJson(),
            ["Name"] = Name.Text,
            ["Enabled"] = Enabled,
            ["IsWriteProtected"] = IsWriteProtected,
            ["Templates"] = SerializeTemplates()
        };

        return ret;
    }

    private JArray SerializeTemplates()
    {
        var ret = new JArray();
        foreach (var template in Templates)
        {
            ret.Add(new JObject()
            {
                ["TemplateId"] = template.UniqueId
            });
        }
        return ret;
    }

    #endregion

    //Loading is in ProfileManager

    #region ISavable

    public string ToFilename(FilenameService fileNames)
        => fileNames.ProfileFile(this);

    public void Save(StreamWriter writer)
    {
        //saving of temporary profiles is not allowed
        if (IsTemporary)
            return;

        using var j = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
        };
        var obj = JsonSerialize();
        obj.WriteTo(j);
    }

    public string LogName(string fileName)
        => Path.GetFileNameWithoutExtension(fileName);

    #endregion
}