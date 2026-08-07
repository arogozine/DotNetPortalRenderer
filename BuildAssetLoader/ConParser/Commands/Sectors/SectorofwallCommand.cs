namespace BuildAssetLoader.Con
{
    // sectorofwall <returnvar> <wall ID>
    public sealed record SectorofwallCommand(string ReturnVar, string WallId) : Command(CommandList.sectorofwall);
}

