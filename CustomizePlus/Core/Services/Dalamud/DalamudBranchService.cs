using OtterGui.Log;
using OtterGui.Services;

namespace CustomizePlus.Core.Services.Dalamud;

public class DalamudBranchService : IService
{
    /// <summary>
    /// Message used in various places to tell user why the plugin is disabled
    /// </summary>
    public const string PluginDisabledMessage = "You are running development or testing version of Dalamud.\n" +
        "Regular users are not supposed to run Customize+ on non-release versions of Dalamud therefore Customize+ has disabled itself until you go back to stable version.\n\nYou can go back to stable version by typing /xlbranch, clicking \"release\" and then \"Pick & Restart\".";

    /// <summary>
    /// Current Dalamud branch
    /// </summary>
    public DalamudBranch CurrentBranch { get; private set; }

    /// <summary>
    /// Current Dalamud branch name
    /// </summary>
    public string CurrentBranchName { get; private set; }

    /// <summary>
    /// Whether to allow or not Customize+ to actually function
    /// </summary>
    public bool AllowPluginToRun { get; private set; } = true;

    public DalamudBranchService(DalamudConfigService dalamudConfigService, Logger logger)
    {/*
        dalamudConfigService.GetDalamudConfig<string>(DalamudConfigService.BetaKindOption, out var betaOption);

        CurrentBranchName = betaOption?.ToLowerInvariant() ?? "release";
        switch (CurrentBranchName)
        {
            case "release":
                CurrentBranch = DalamudBranch.Release;
                break;
            case "stg":
                CurrentBranch = DalamudBranch.Staging;
                break;
            default:
                CurrentBranch = DalamudBranch.Other;
                break;
        }

    #if CHECK_DALAMUD_BRANCH
        AllowPluginToRun = CurrentBranch == DalamudBranch.Release;
    #endif

        logger.Information($"Current Dalamud branch is: {CurrentBranchName} ({CurrentBranch}). Plugin allowed to run: {AllowPluginToRun}");*/
        CurrentBranchName = "release";
        CurrentBranch = DalamudBranch.Release;
        AllowPluginToRun = true;
    }

    public enum DalamudBranch
    {
        //For our purposes we want to default to Release
        Release,
        Staging,
        Other
    }
}
