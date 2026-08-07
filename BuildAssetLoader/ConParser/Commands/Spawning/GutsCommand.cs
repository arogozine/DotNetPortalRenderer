namespace BuildAssetLoader.Con
{
    // guts <tilenum> <amount> — tilenum is one of the hardcoded gore tiles (JIBS1..JIBS6, HEADJIB, ...).
    public sealed record GutsCommand(string TileNum, string Amount) : Command(CommandList.guts);
}

