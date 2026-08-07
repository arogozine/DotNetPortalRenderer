namespace BuildAssetLoader.Con
{
    // operatesectors <sector> <actor> — triggers elevator/lift sectors from actor code.
    public sealed record OperatesectorsCommand(string Sector, string Actor) : Command(CommandList.operatesectors);
}

