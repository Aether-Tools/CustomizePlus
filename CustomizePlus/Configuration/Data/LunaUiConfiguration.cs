using CustomizePlus.Core.Services;
using Luna.Generators;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Actors;
using System.Text.Json;

namespace CustomizePlus.Configuration.Data;

public sealed partial class LunaUiConfiguration : ConfigurationFile<FilenameService>
{
    private readonly ActorManager _actors;

    public LunaUiConfiguration(SaveService saveService, MessageService messageService, ActorManager actors)
        : base(saveService, messageService, TimeSpan.FromMinutes(5))
    {
        _actors = actors;
        Load();
    }

    [ConfigProperty]
    private TwoPanelWidth _templatesTabScale = new(250, ScalingMode.Absolute);

    [ConfigProperty]
    private TwoPanelWidth _profilesTabScale = new(0.3f, ScalingMode.Percentage);

    public override int CurrentVersion
        => 1;

    protected override void AddData(Utf8JsonWriter j)
    {
        TemplatesTabScale.WriteJson(j, "TemplatesTab"u8);
        ProfilesTabScale.WriteJson(j, "ProfilesTab"u8);
    }

    protected override void LoadData(JObject j)
    {
        _templatesTabScale = TwoPanelWidth.ReadJson(j, "TemplatesTab", new TwoPanelWidth(250, ScalingMode.Absolute));
        _profilesTabScale = TwoPanelWidth.ReadJson(j, "ProfilesTab", new TwoPanelWidth(0.3f, ScalingMode.Percentage));
    }

    public override string ToFilePath(FilenameService fileNames)
        => fileNames.UiConfigurationFile;
}
