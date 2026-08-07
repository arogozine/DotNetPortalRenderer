namespace BuildAssetLoader.Con
{
    public sealed record ShootCommand(string TileNumber) : BaseShootCommand(CommandList.shoot, TileNumber);
}

