namespace BuildAssetLoader.Map
{
    public sealed class MapFile
    {
        public required uint Version { get; init; }
        public required StartingPosition StartingPosition { get; init; }
        public required SectorType[] Sectors { get; init; }
        public required WallType[] Walls { get; init; }
        public required SpriteType[] Sprites { get; init; }
    }
}
