using CustomizePlus.Armatures.Data;
using CustomizePlus.Core.Extensions;
using CustomizePlus.Core.Services;
using CustomizePlus.Profiles.Enums;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;

namespace CustomizePlus.Profiles.Data;

/// <summary>
///     Encapsulates the user-controlled aspects of a character profile, ie all of
///     the information that gets saved to disk by the plugin.
/// </summary>
public sealed class Profile : ISavable, IFileSystemValue<Profile>
{
    public const int Version = 5;

    private static int _nextGlobalId;

    private readonly int _localId;

    public List<Armature> Armatures = new();

    public List<ActorIdentifier> Characters { get; set; } = new();

    public string Name { get; set; }

    public bool Enabled { get; set; }
    public DateTimeOffset CreationDate { get; set; } = DateTime.UtcNow;
    public DateTimeOffset ModifiedDate { get; set; } = DateTime.UtcNow;

    public Guid UniqueId { get; set; } = Guid.NewGuid();

    public List<Template> Templates { get; init; } = new();
    public HashSet<Guid> DisabledTemplates { get; init; } = new();

    public bool IsWriteProtected { get; internal set; }

    public ProfileType ProfileType { get; set; }

    /// <summary>
    /// Profile priority when there are several profiles affecting same character
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Tells us if this profile is not persistent (ex. was made via IPC calls) and should have specific treatement like not being shown in UI, etc.
    /// WARNING, TEMPLATES FOR TEMPORARY PROFILES *ARE NOT* STORED IN TemplateManager
    /// </summary>
    public bool IsTemporary => ProfileType == ProfileType.Temporary;

    public string Incognito
        => UniqueId.ToString()[..8];

    public int Index { get; internal set; }

    public DataPath Path { get; } = new();

    public IFileSystemData<Profile>? Node { get; set; }

    string IFileSystemValue.DisplayName
        => Name;

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
        Characters = original.Characters.ToList();

        foreach (var template in original.Templates)
        {
            Templates.Add(template);
        }

        foreach (var disabledTemplate in original.DisabledTemplates)
        {
            DisabledTemplates.Add(disabledTemplate);
        }
    }

    public override string ToString()
    {
        return $"Profile '{Name.Incognify()}' on {string.Join(',', Characters.Select(x => x.Incognito(null)))} [{UniqueId}]";
    }

    #region Serialization

    public JObject JsonSerialize()
    {
        var ret = new JObject()
        {
            ["Version"] = Version,
            ["UniqueId"] = UniqueId,
            ["CreationDate"] = CreationDate,
            ["ModifiedDate"] = ModifiedDate,
            ["Characters"] = SerializeCharacters(),
            ["Name"] = Name,
            ["Enabled"] = Enabled,
            ["IsWriteProtected"] = IsWriteProtected,
            ["Priority"] = Priority,
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
                ["TemplateId"] = template.UniqueId,
                ["Enabled"] = !DisabledTemplates.Contains(template.UniqueId)
            });
        }
        return ret;
    }

    private JArray SerializeCharacters()
    {
        var ret = new JArray();
        foreach (var character in Characters)
        {
            ret.Add(character.ToJson());
        }
        return ret;
    }

    #endregion

    //Loading is in ProfileManager

    #region ISavable

    public string ToFilePath(FilenameService fileNames)
        => fileNames.ProfileFile(this);

    public void Save(Stream stream)
    {
        //saving of temporary profiles is not allowed
        if (IsTemporary)
            return;

        using var writer = new StreamWriter(stream);
        using var j = new JsonTextWriter(writer)
        {
            Formatting = Formatting.Indented,
        };
        var obj = JsonSerialize();
        WriteFileSystemPath(obj);
        obj.WriteTo(j);
    }

    public string LogName(string fileName)
        => System.IO.Path.GetFileNameWithoutExtension(fileName);

    #endregion

    public string Identifier
        => UniqueId.ToString();

    internal static void ReadFileSystemPath(JObject obj, DataPath path)
    {
        if (obj["FileSystemPath"] is not JObject pathObj)
            return;

        path.Folder = pathObj["Folder"]?.ToObject<string>() ?? string.Empty;
        path.SortName = pathObj["SortName"]?.ToObject<string>();
    }

    private void WriteFileSystemPath(JObject obj)
    {
        if (Path.IsDefault)
            return;

        obj["FileSystemPath"] = new JObject
        {
            ["Folder"] = Path.Folder,
            ["SortName"] = Path.SortName,
        };
    }
}