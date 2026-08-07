namespace BuildAssetLoader.Con
{
    // nextspritesect <NextSpriteID> <CurrentSpriteID>
    public sealed record NextspritesectCommand(string NextSpriteId, string CurrentSpriteId) : Command(CommandList.nextspritesect);
}

