using CustomizePlus.Profiles.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using ECommons.EzIpcManager;

using IPCProfileDataTuple = (System.Guid UniqueId, string Name, string CharacterName, bool IsEnabled);

namespace CustomizePlus.Api;

public partial class CustomizePlusIpc
{
    /// <summary>
    /// Retrieve list of all user profiles
    /// </summary>
    /// <returns></returns>
    [EzIPC("Profile.GetList")]
    private IList<IPCProfileDataTuple> GetProfileList()
    {
        return _profileManager.Profiles
            .Where(x => x.ProfileType == ProfileType.Normal)
            .Select(x => (x.UniqueId, x.Name.Text, x.CharacterName.Text, x.Enabled))
            .ToList();
    }

    /// <summary>
    /// Enable profile using its Unique ID
    /// </summary>
    /// <param name="uniqueId"></param>
    [EzIPC("Profile.EnableByUniqueId")]
    private void EnableProfileByUniqueId(Guid uniqueId)
    {
        _profileManager.SetEnabled(uniqueId, true);
    }

    /// <summary>
    /// Disable profile using its Unique ID
    /// </summary>
    [EzIPC("Profile.DisableByUniqueId")]
    private void DisableProfileByUniqueId(Guid uniqueId)
    {
        _profileManager.SetEnabled(uniqueId, false);
    }
}
