using System.ComponentModel;

namespace BuildAssetLoader.Texture
{
    public sealed class ArtFile
    {
        public required int ArtNum { get; init; }

        /// <summary>
        /// Art Version
        /// </summary>
        [DefaultValue(1U)]
        public required uint ArtVersion { get; init; }

        /// <summary>
        /// Start Tile
        /// </summary>
        public required uint LocalTileStart { get; init; }

        /// <summary>
        /// End Tile
        /// </summary>
        public required uint LocalTileEnd { get; init; }

        public required TileType[] Tiles { get; init; }
    }
}
