namespace BuildAssetLoader.Con
{
    // prevspritestat <PrevSpriteID> <CurrentSpriteID>
    public sealed record PrevspritestatCommand(string PrevSpriteId, string CurrentSpriteId) : Command(CommandList.prevspritestat);
}

