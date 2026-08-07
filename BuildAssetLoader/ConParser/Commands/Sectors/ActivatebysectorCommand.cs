namespace BuildAssetLoader.Con
{
    // activatebysector <sectNum> <spriteNum> — triggers ACTIVATOR sprites in a sector, or its tag effect if none found.
    public sealed record ActivatebysectorCommand(string SectNum, string SpriteNum) : Command(CommandList.activatebysector);
}

