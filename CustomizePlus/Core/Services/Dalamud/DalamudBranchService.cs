using OtterGui.Log;
using OtterGui.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Core.Services.Dalamud;

public class DalamudBranchService : IService
{
    /// <summary>
    /// Current Dalamud branch
    /// </summary>
    public DalamudBranch CurrentBranch { get; private set; }

    /// <summary>
    /// Current Dalamud branch name
    /// </summary>
    public string CurrentBranchName { get; private set; }

    public DalamudBranchService(DalamudConfigService dalamudConfigService, Logger logger)
    {
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

        logger.Information($"Current Dalamud branch is: {CurrentBranchName} ({CurrentBranch})");
    }

    public enum DalamudBranch
    {
        //For our purposes we want to default to Release
        Release,
        Staging,
        Other
    }
}
