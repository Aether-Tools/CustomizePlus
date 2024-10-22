global using IPCCharacterDataTuple = (string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType);

//Virtual path is full path to the profile in the virtual folders created by user in the profile list UI

//Character.WorldId is value of Penumbra.GameData.Structs.WorldId. ushort.MaxValue if AnyWorld or if CharacterType != Player/Owned.
//Does not bear any meaning for CharacterType = Owned right now.

//CharacterType represents Penumbra.GameData.Enums.IdentifierType and can be one of the following:
//0 = Invalid (should never be returned in normal circumstances)
//1 = Player
//2 = Owned (companion, minion)
//3 = Unused
//4 = NPC
//5 = Retainer
//6 = Unused

//CharacterSubType represents Penumbra.GameData.Actors.ActorIdentifier.RetainerType and only used by CharacterType = Retainer and can be:
//0 = Both
//1 = Bell
//2 = Mannequin

global using IPCProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled);