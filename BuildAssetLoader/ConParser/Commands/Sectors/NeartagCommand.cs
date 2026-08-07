namespace BuildAssetLoader.Con
{
    // neartag <x> <y> <z> <sect> <ang> <nearTagSector> <nearTagWall> <nearTagSprite> <nearTagHitDist> <nearTagRange> <tagSearch>
    public sealed record NeartagCommand(
        string X, string Y, string Z, string Sect, string Ang,
        string NearTagSector, string NearTagWall, string NearTagSprite, string NearTagHitDist, string NearTagRange, string TagSearch)
        : Command(CommandList.neartag);
}

