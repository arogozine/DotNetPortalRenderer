namespace BuildAssetLoader.Con
{
    public sealed record ZshootvarCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.zshootvar, Zvel, TileNumber);
}

