using CustomizePlus.Api.Data;
using CustomizePlus.Core.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OtterGui.Classes;
using System;
using System.Collections.Generic;
using System.IO;

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
    /// Creates a new pseudo template from IPCCharacterProfile bone data
    /// </summary>
    public Template(IPCCharacterProfile profile)
    {
        CreationDate = DateTimeOffset.UtcNow;
        ModifiedDate = DateTimeOffset.UtcNow;

        foreach (var (boneName, ipcBone) in profile.Bones)
        {
            Bones[boneName] = new BoneTransform
            {
                Translation = ipcBone.Translation,
                Rotation = ipcBone.Rotation,
                Scaling = ipcBone.Scaling,
                PropagateTranslation = ipcBone.PropagateTranslation,
                PropagateRotation = ipcBone.PropagateRotation,
                PropagateScale = ipcBone.PropagateScale,
            };
        }
    }

    /// <summary>
    /// Creates a new template based on bone data from another one
    /// </summary>
    public Template(Template original) : this()
    {
        foreach (var kvp in original.Bones)
        {
            if (!kvp.Value.IsEdited()) //do not copy unedited bones
                continue;

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
            4 or 5 => LoadV5(obj),
            _ => throw new Exception("The design to be loaded has no valid Version."),
        };
    }

    private static Template LoadV5(JObject obj)
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
