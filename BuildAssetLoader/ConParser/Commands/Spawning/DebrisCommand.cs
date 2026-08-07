namespace BuildAssetLoader.Con
{
    // ===== Materials =====

    // debris <tilenum> <amount> — tilenum is SCRAP1..SCRAP6.
    public sealed record DebrisCommand(string TileNum, string Amount) : Command(CommandList.debris);
}

