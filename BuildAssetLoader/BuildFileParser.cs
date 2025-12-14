using System.ComponentModel;

namespace BuildAssetLoader
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PropType
    {
        public byte AnimType;
        public byte OffsetX;
        public byte OffsetY;
        public byte AnimSpeed;
    }

    public readonly struct TileType
    {
        public readonly short XSize;
        public readonly short YSize;
        public readonly PropType Properties;
        public readonly byte[] Pixels;

        public TileType(short xSize, short ySize, PropType properties, byte[] pixels)
        {
            XSize = xSize;
            YSize = ySize;
            Properties = properties;
            Pixels = pixels;
        }
    }

    internal class ArtFile
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

    internal static class BuildFileParser
    {
        public static List<ArtFile> ExtractArtFiles(GrpFile grpFile)
        {
            List<ArtFile> artFiles = [];

            foreach (var fileAndBinary in grpFile.Files.Where(IsArtFile))
            {
                string fileName = fileAndBinary.Key;
                Span<byte> binary = fileAndBinary.Value;

                int artNum = 0;

                string baseName = Path.GetFileNameWithoutExtension(fileName);
                if (baseName.Length >= 3)
                {
                    string lastThree = baseName[^3..];
                    _ = int.TryParse(lastThree, out artNum);
                }

                uint artVersion = BitConverter.ToUInt32(binary[0..]);
                uint localTileStart = BitConverter.ToUInt32(binary[8..]);
                uint localTileEnd = BitConverter.ToUInt32(binary[12..]);

                binary = binary[16..];
                int numberOfTiles = unchecked((int)(localTileEnd - localTileStart + 1));
                int numberOfTilesInt16 = numberOfTiles * sizeof(short);
                int numberOfTilesInt32 = numberOfTiles * sizeof(int);

                Span<short> tilesizx = MemoryMarshal.Cast<byte, short>(binary[..numberOfTilesInt16]);
                binary = binary[numberOfTilesInt16..];
                Span<short> tilesizy = MemoryMarshal.Cast<byte, short>(binary[..numberOfTilesInt16]);
                binary = binary[numberOfTilesInt16..];
                Span<PropType> picanm = MemoryMarshal.Cast<byte, PropType>(binary[..numberOfTilesInt32]);
                binary = binary[numberOfTilesInt32..];

                var tiles = new TileType[numberOfTiles];

                // Read per-tile metadata
                for (int i = 0; i < numberOfTiles; i++)
                {
                    short xSize = tilesizx[i];
                    short ySize = tilesizy[i];
                    PropType prop = picanm[i];

                    int length = xSize * ySize;
                    byte[] pixels = binary[..length].ToArray();
                    binary = binary[length..];

                    tiles[i] = new TileType(xSize, ySize, prop, pixels);
                }

                artFiles.Add(new ArtFile
                {
                    ArtNum = artNum,
                    ArtVersion = artVersion,
                    LocalTileStart = localTileStart,
                    LocalTileEnd = localTileEnd,
                    Tiles = tiles
                });
            }

            return artFiles;
        }

        private static bool IsArtFile(KeyValuePair<string, byte[]> fileName)
        {
            return fileName.Key.EndsWith(".ART", StringComparison.OrdinalIgnoreCase);
        }
    }
}
