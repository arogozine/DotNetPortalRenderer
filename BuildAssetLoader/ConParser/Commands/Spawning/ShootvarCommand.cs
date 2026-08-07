namespace BuildAssetLoader.Con
{
    public sealed record ShootvarCommand(string TileNumber) : BaseShootCommand(CommandList.shootvar, TileNumber);
}

