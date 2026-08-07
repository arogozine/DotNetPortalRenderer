namespace BuildAssetLoader.Con
{
    // canseespr <spriteID1> <spriteID2> <returnvar>
    public sealed record CanseesprCommand(string SpriteId1, string SpriteId2, string ReturnVar) : Command(CommandList.canseespr);
}

