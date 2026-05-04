using CustomizePlus.Game.Services;
using CustomizePlus.GameData.Extensions;
using CustomizePlus.Profiles;
using CustomizePlus.Profiles.Data;
using CustomizePlus.Templates.Data;

namespace CustomizePlus.Core.Services;

public sealed class ColorsService : IService
{
    private readonly GameObjectService _gameObjectService;
    private readonly ProfileManager _profileManager;

    public ColorsService(GameObjectService gameObjectService, ProfileManager profileManager)
    {
        _gameObjectService = gameObjectService;
        _profileManager = profileManager;
    }

    public Rgba32 GetProfileColor(Profile profile)
    {
        if (profile.IsTemporary)
            return Colors.Value(ColorId.DisabledProfile);

        var identifier = _gameObjectService.GetCurrentPlayerActorIdentifier();
        if (profile.Enabled)
            return Colors.Value(profile.Characters.Any(x => x.MatchesIgnoringOwnership(identifier)) ? ColorId.LocalCharacterEnabledProfile : ColorId.EnabledProfile);
        else
            return Colors.Value(profile.Characters.Any(x => x.MatchesIgnoringOwnership(identifier)) ? ColorId.LocalCharacterDisabledProfile : ColorId.DisabledProfile);
    }

    public Rgba32 GetTemplateColor(Template template)
    {
        return Colors.Value(_profileManager.GetProfilesUsingTemplate(template).Any() ? ColorId.UsedTemplate : ColorId.UnusedTemplate);
    }
}
