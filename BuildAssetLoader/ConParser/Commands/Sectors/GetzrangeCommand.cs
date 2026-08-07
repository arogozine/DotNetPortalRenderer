namespace BuildAssetLoader.Con
{
    // getzrange <x> <y> <z> <sector> <ceilingz> <ceilinghit> <floorz> <floorhit> <walldist> <clipmask>
    public sealed record GetzrangeCommand(
        string X, string Y, string Z, string Sector,
        string CeilingZ, string CeilingHit, string FloorZ, string FloorHit, string WallDist, string ClipMask)
        : Command(CommandList.getzrange);
}

