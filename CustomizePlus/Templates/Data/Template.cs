using System;
using System.Collections.Generic;
using System.IO;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;

namespace CustomizePlus.Templates.Data;

/// <summary>
///     Encapsulates the user-controlled aspects of a template, ie all of
///     the information that gets saved to disk by the plugin.
/// </summary>
public sealed class Template : ISavable
{
    public const int Version = Constants.ConfigurationVersion;

    public LowerString Name { get; internal set; } = "Template";

    public DateTimeOffset CreationDate { get; internal set; } = DateTime.UtcNow;
    public DateTimeOffset ModifiedDate { get; internal set; } = DateTime.UtcNow;

    public Guid UniqueId { get; internal set; } = Guid.NewGuid();

    public bool IsWriteProtected { get; internal set; }

    public string Incognito
        => UniqueId.ToString()[..8];

    public Dictionary<string, BoneTransform> Bones { get; init; } = new();

    public Template()
    {
    }

    /// <summary>
    /// Creates a new template based on bone data from another one
    /// </summary>
    public Template(Template original) : this()
    {
        foreach (var kvp in original.Bones)
        {
            Bones[kvp.Key] = new BoneTransform();
            Bones[kvp.Key].UpdateToMatch(kvp.Value);
        }
    }

    public override string ToString()
    {
        return $"Template '{Name.Text.Incognify()}' with {Bones.Count} bone edits [{UniqueId}]";
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
            ["Name"] = Name.Text,
            ["Bones"] = JObject.FromObject(Bones),
            ["IsWriteProtected"] = IsWriteProtected
        };

        return ret;
    }

    #endregion

    #region Deserialization

    public static Template Load(JObject obj)
    {
        var version = obj["Version"]?.ToObject<int>() ?? 0;
        return version switch
        {
            //Did not exist before v4
            4 => LoadV4(obj),
            _ => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Template LoadV4(JObject obj)
    {
        var creationDate = obj["CreationDate"]?.ToObject<DateTimeOffset>() ?? throw new ArgumentNullException("CreationDate");

        var template = new Template()
        {
            CreationDate = creationDate,
            UniqueId = obj["UniqueId"]?.ToObject<Guid>() ?? throw new ArgumentNullException("UniqueId"),
            Name = new LowerString(obj["Name"]?.ToObject<string>()?.Trim() ?? throw new ArgumentNullException("Name")),
            ModifiedDate = obj["ModifiedDate"]?.ToObject<DateTimeOffset>() ?? creationDate,
            Bones = obj["Bones"]?.ToObject<Dictionary<string, BoneTransform>>() ?? throw new ArgumentNullException("Bones"),
            IsWriteProtected = obj["IsWriteProtected"]?.ToObject<bool>() ?? false
        };
        if (template.ModifiedDate < creationDate)
            template.ModifiedDate = creationDate;

        return template;
    }

    #endregion

    #region ISavable

    public string ToFilename(FilenameService fileNames)
        => fileNames.TemplateFile(this);

    public void Save(StreamWriter writer)
    {
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
