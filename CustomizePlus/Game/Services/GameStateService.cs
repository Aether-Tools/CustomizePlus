using CustomizePlusPlus.Game.Services.GPose;
using CustomizePlusPlus.Game.Services.GPose.ExternalTools;

namespace CustomizePlusPlus.Game.Services;

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
