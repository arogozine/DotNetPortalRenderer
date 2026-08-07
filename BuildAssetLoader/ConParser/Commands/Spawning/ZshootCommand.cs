namespace BuildAssetLoader.Con
{
    public sealed record ZshootCommand(string Zvel, string TileNumber) : BaseZshootCommand(CommandList.zshoot, Zvel, TileNumber);
}

