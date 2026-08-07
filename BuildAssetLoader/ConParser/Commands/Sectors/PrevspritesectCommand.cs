namespace BuildAssetLoader.Con
{
    // prevspritesect <PrevSpriteID> <CurrentSpriteID>
    public sealed record PrevspritesectCommand(string PrevSpriteId, string CurrentSpriteId) : Command(CommandList.prevspritesect);
}

