using DoomAssetLoader.Wad;
using System.Text;

namespace DoomAssetLoader
{
    public sealed class WadLoader
    {
        public required bool LoadMaps { get; init; }
        public required bool LoadTextures { get; init; }
        public required bool LoadOther { get; init; }
        public required string? MapToLoad { get; init; }

        private readonly string filePath;

        public WadLoader(string filePath)
        {
            this.filePath = filePath;
        }

        private static readonly string[] MapLumps = [
            LumpType.Things,
            LumpType.LineDefs,
            LumpType.SideDefs,
            LumpType.SSectors,
            LumpType.Vertexes,
            LumpType.Segs,
            LumpType.Sectors,
            LumpType.Nodes,
            LumpType.Reject,
            LumpType.TextMap,
            LumpType.BlockMap
        ];

        private static readonly string[] TextureLumps = [
            LumpType.PlayPal,
            LumpType.PNames,
            LumpType.Texture1,
            LumpType.Texture2,
            LumpType.ColorMap
        ];

        public WadFile LoadWad()
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            var wadFile = new WadFile();

            byte[] buffer4 = new byte[4];
            byte[] buffer8 = new byte[8];

            int i;

            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            fs.Seek(0, SeekOrigin.Begin);

            // Bytes 0-3 (ASCII string): IWAD or PWAD
            fs.ReadExactly(buffer4, 0, buffer4.Length);

            string wadType = GetStringFromBytes(buffer4);

            wadFile.Type = Enum.Parse<WadType>(wadType);

            // Bytes 4-7 (int): lump count
            fs.ReadExactly(buffer4, 0, 4);
            int lumpCount = BitConverter.ToInt32(buffer4);
            if (lumpCount <= 0)
            {
                throw new ArgumentException("Invalid Format", nameof(filePath));
            }

            // Bytes 8-11 (int): directory offset
            fs.ReadExactly(buffer4, 0, 4);
            uint directoryOffset = BitConverter.ToUInt32(buffer4);
            if (directoryOffset < 12)
            {
                throw new ArgumentException("Invalid Format", nameof(filePath));
            }

            bool isFlats = false;
            bool isPatches = false;
            bool isSprites = false;
            bool isMap = false;
            string? mapName = null;

            for (i = 0; i < lumpCount; i++)
            {
                fs.Seek(directoryOffset + 16 * i, SeekOrigin.Begin);

                // a long integer, the file offset to the start of the lump
                fs.ReadExactly(buffer4, 0, 4);
                uint lumpOffset = BitConverter.ToUInt32(buffer4);

                // a long integer, the size of the lump in bytes
                fs.ReadExactly(buffer4, 0, 4);
                uint lumpSize = BitConverter.ToUInt32(buffer4);

                // 8-byte ASCII string, the name of the lump, padded with zeros
                fs.ReadExactly(buffer8, 0, 8);
                string lumpName = GetStringFromBytes(buffer8);

                if (lumpName.StartsWith("MAP"))
                {
                    mapName = lumpName;
                    isMap = true;
                }
                else if (isMap && !MapLumps.Contains(lumpName))
                {
                    isMap = false;
                    mapName = null;
                }

                switch (lumpName)
                {
                    case LumpType.PStart:
                        isPatches = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.PEnd:
                        isPatches = false;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.FStart:
                        isFlats = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.FEnd:
                        isFlats = false;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.SStart:
                        isSprites = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.SEnd:
                        isSprites = false;
                        isMap = false;
                        mapName = null;
                        break;
                }

                if (isMap && !LoadMaps)
                {
                    continue;
                }

                if (isMap && MapToLoad is string mapToLoad && mapToLoad != mapName)
                {
                    continue;
                }

                bool isTexture = isFlats || isSprites || isPatches || TextureLumps.Contains(lumpName);

                if (isTexture && !LoadTextures)
                {
                    continue;
                }

                if (!isTexture && !isMap && !LoadOther)
                {
                    continue;
                }

                byte[] lumpbytes = new byte[lumpSize];

                if (lumpSize != 0)
                {
                    fs.Seek(lumpOffset, SeekOrigin.Begin);
                    fs.ReadExactly(lumpbytes, 0, (int)lumpSize);
                }

                wadFile.Lumps.Add(new WadLump(lumpName, lumpbytes)
                {
                    IsFlat = isFlats,
                    IsPatch = isPatches,
                    IsSprite = isSprites,
                    MapName = mapName,
                    IsMap = isMap
                });
            }

            return wadFile;
        }

        private static string GetStringFromBytes(ReadOnlySpan<byte> asciiBytes)
        {
            int index = asciiBytes.IndexOf((byte)0);

            if (index > 0)
            {
                asciiBytes = asciiBytes[..index];
            }

            return Encoding.ASCII.GetString(asciiBytes);
        }
    }
}
