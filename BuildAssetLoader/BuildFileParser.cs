using BuildAssetLoader.Map;
using BuildAssetLoader.Texture;
using System.Diagnostics;

namespace BuildAssetLoader
{

    public static class BuildFileParser
    {
        public static unsafe List<MapFile> ExtractMapFiles(GrpFile grpFile)
        {
            List<MapFile> mapFiles = [];

            foreach (var fileAndBinary in grpFile.Files.Where(IsMapFile))
            {
                (int start, int length) sectors, walls, sprites;

                string fileName = fileAndBinary.Key;
                Span<byte> binary = fileAndBinary.Value;
                int offset = 0;

                uint mapVersion = BitConverter.ToUInt32(binary[offset..]);
                if (mapVersion != 7)
                {
                    throw new NotSupportedException("We only support v7 maps");
                }
                offset += sizeof(uint);

                StartingPosition mapHeader = MemoryMarshal.Read<StartingPosition>(binary[offset..]);
                offset += sizeof(StartingPosition);

                // sectors
                ushort numSectors = BitConverter.ToUInt16(binary[offset..]);
                offset += sizeof(ushort);
                int sectorSizeInBytes = numSectors * sizeof(SectorType);
                sectors = (offset, sectorSizeInBytes);
                offset += sectorSizeInBytes;

                // walls
                ushort numWalls = BitConverter.ToUInt16(binary[offset..]);
                offset += sizeof(ushort);
                int wallSizeInBytes = numWalls * sizeof(WallType);
                walls = (offset, wallSizeInBytes);
                offset += wallSizeInBytes;

                // sprites
                ushort numSprites = BitConverter.ToUInt16(binary[offset..]);
                offset += sizeof(ushort);
                int spriteSizeInBytes = numSprites * sizeof(SpriteType);
                sprites = (offset, spriteSizeInBytes);

                mapFiles.Add(new MapFile(fileAndBinary.Value, sectors, walls, sprites)
                {
                    MapName = Path.GetFileNameWithoutExtension(fileName),
                    Version = mapVersion,
                    StartingPosition = mapHeader,
                });
            }

            return mapFiles;
        }

        public static List<ArtFile> ExtractArtFiles(GrpFile grpFile)
        {
            List<ArtFile> artFiles = [];

            foreach (KeyValuePair<string, byte[]> fileAndBinary in grpFile.Files.Where(IsArtFile))
            {
                string fileName = fileAndBinary.Key;
                Span<byte> binary = fileAndBinary.Value;
                int offset = 0;

                int artNum = 0;

                string baseName = Path.GetFileNameWithoutExtension(fileName);
                if (baseName.Length >= 3)
                {
                    string lastThree = baseName[^3..];
                    _ = int.TryParse(lastThree, out artNum);
                }

                uint artVersion = BitConverter.ToUInt32(binary[offset..]);
                uint localTileStart = BitConverter.ToUInt32(binary[(offset + 8)..]);
                uint localTileEnd = BitConverter.ToUInt32(binary[(offset + 12)..]);

                Debug.Assert(artVersion == 1);

                offset += 16;
                int numberOfTiles = unchecked((int)(localTileEnd - localTileStart + 1));
                int numberOfTilesInt16 = numberOfTiles * sizeof(short);
                int numberOfTilesInt32 = numberOfTiles * sizeof(int);

                Span<short> tilesizx = MemoryMarshal.Cast<byte, short>(binary.Slice(offset, numberOfTilesInt16));
                offset += numberOfTilesInt16;
                Span<short> tilesizy = MemoryMarshal.Cast<byte, short>(binary.Slice(offset, numberOfTilesInt16));
                offset += numberOfTilesInt16;
                Span<PropType> picanm = MemoryMarshal.Cast<byte, PropType>(binary.Slice(offset, numberOfTilesInt32));
                offset += numberOfTilesInt32;

                var tiles = new TileType[numberOfTiles];

                // Read per-tile metadata
                for (int i = 0; i < numberOfTiles; i++)
                {
                    short xSize = tilesizx[i];
                    short ySize = tilesizy[i];
                    PropType prop = picanm[i];

                    int length = xSize * ySize;
                    
                    tiles[i] = new TileType(fileAndBinary.Value, (offset, length), xSize, ySize, prop);

                    offset += length;
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
