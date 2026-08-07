namespace BuildAssetLoader.Con
{
    // nextspritestat <NextSpriteID> <CurrentSpriteID>
    public sealed record NextspritestatCommand(string NextSpriteId, string CurrentSpriteId) : Command(CommandList.nextspritestat);
}

