using DoomAssetLoader.Wad;
using System.Text;

namespace DoomAssetLoader
{
    public sealed class WadLoader
    {
        public bool LoadMaps { get; init; }
        public bool LoadTextures { get; init; }
        public bool LoadOther { get; init; }
        public string? MapToLoad { get; init; }

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

            Span<string> pNames = default;


            // Try getting the PNAMES lump first
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

                if (lumpName == LumpType.PNames)
                {
                    byte[] lumpbytes = new byte[lumpSize];

                    if (lumpSize != 0)
                    {
                        fs.Seek(lumpOffset, SeekOrigin.Begin);
                        fs.ReadExactly(lumpbytes, 0, (int)lumpSize);
                    }

                    var lump = new WadLump(lumpName, lumpbytes)
                    {
                        IsFlat = false,
                        IsPatch = false,
                        IsSprite = false,
                        MapName = mapName,
                        IsMap = false
                    };

                    pNames = WadLumpParser.ReadPNames(lump);

                    break;
                }
            }

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
                    case LumpType.PPStart:
                        isPatches = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.PEnd:
                    case LumpType.PPEnd:
                        isPatches = false;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.FStart:
                    case LumpType.FFStart:
                    case LumpType.TXStart:
                        isFlats = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.FEnd:
                    case LumpType.FFEnd:
                    case LumpType.TXEnd:
                        isFlats = false;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.SStart:
                    case LumpType.SSStart:
                        isSprites = true;
                        isMap = false;
                        mapName = null;
                        break;
                    case LumpType.SEnd:
                    case LumpType.SSEnd:
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

                bool inPnames = pNames.Contains(lumpName);

                bool isTexture = isFlats || isSprites || isPatches || inPnames || TextureLumps.Contains(lumpName);

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

                var lump = new WadLump(lumpName, lumpbytes)
                {
                    IsFlat = isFlats,
                    IsPatch = isPatches || inPnames,
                    IsSprite = isSprites,
                    MapName = mapName,
                    IsMap = isMap
                };

                wadFile.Lumps.Add(lump);
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
