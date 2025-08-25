using CustomizePlus.Game.Services.GPose;
using CustomizePlus.Game.Services.GPose.ExternalTools;

namespace CustomizePlus.Game.Services;

public class GameStateService
{
    private readonly GPoseService _gposeService;
    private readonly PosingModeDetectService _posingModeDetectService;

    public GameStateService(GPoseService gposeService, PosingModeDetectService posingModeDetectService)
    {
        _gposeService = gposeService;
        _posingModeDetectService = posingModeDetectService;
    }

    public bool GameInPosingMode()
    {
        return _gposeService.GPoseState == GPoseState.Inside || _posingModeDetectService.IsInPosingMode;
    }
}
