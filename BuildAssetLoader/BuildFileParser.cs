using BuildAssetLoader.Map;
using BuildAssetLoader.Texture;

namespace BuildAssetLoader
{

    public static class BuildFileParser
    {
        public static unsafe List<MapFile> ExtractMapFiles(GrpFile grpFile)
        {
            List<MapFile> mapFiles = [];

            foreach (var fileAndBinary in grpFile.Files.Where(IsMapFile))
            {
                string fileName = fileAndBinary.Key;
                Span<byte> binary = fileAndBinary.Value;

                uint mapVersion = BitConverter.ToUInt32(binary);
                if (mapVersion != 7)
                {
                    throw new NotSupportedException("We only support v7 maps");
                }
                binary = binary[sizeof(uint)..];

                StartingPosition mapHeader = MemoryMarshal.Read<StartingPosition>(binary);
                binary = binary[sizeof(StartingPosition)..];

                // load all sectors
                ushort numSectors = BitConverter.ToUInt16(binary);
                binary = binary[sizeof(ushort)..];
                int sectorSizeInBytes = numSectors * sizeof(SectorType);
                SectorType[] sectors = MemoryMarshal.Cast<byte, SectorType>(binary[..sectorSizeInBytes])
                    .ToArray();
                binary = binary[sectorSizeInBytes..];

                // load all walls
                ushort numWalls = BitConverter.ToUInt16(binary);
                binary = binary[sizeof(ushort)..];
                int wallSizeInBytes = numSectors * sizeof(WallType);
                WallType[] walls = MemoryMarshal.Cast<byte, WallType>(binary[..wallSizeInBytes])
                    .ToArray();
                binary = binary[wallSizeInBytes..];

                // load all sprites
                ushort numSprites = BitConverter.ToUInt16(binary);
                binary = binary[sizeof(ushort)..];
                int spriteSizeInBytes = numSectors * sizeof(SpriteType);
                SpriteType[] sprites = MemoryMarshal.Cast<byte, SpriteType>(binary[..spriteSizeInBytes])
                    .ToArray();
                binary = binary[spriteSizeInBytes..];

                mapFiles.Add(new MapFile
                {
                    Version = mapVersion,
                    StartingPosition = mapHeader,
                    Sectors = sectors,
                    Sprites = sprites,
                    Walls = walls
                });
            }

            return mapFiles;
        }

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

        private static bool IsMapFile(KeyValuePair<string, byte[]> fileName)
        {
            return fileName.Key.EndsWith(".MAP", StringComparison.OrdinalIgnoreCase);
        }
    }
}
