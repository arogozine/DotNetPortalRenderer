using DoomAssetLoader;
using DoomAssetLoader.Map;
using DoomAssetLoader.Texture;
using DoomAssetLoader.Udmf;
using DoomAssetLoader.Wad;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using RenderingEngine.Models.Json;
using SkiaSharp;
using System.Text;
using Sector = DoomAssetLoader.Map.Sector;

namespace RenderingEngine.DoomMapLoader
{
    [SkipLocalsInit]
    internal static class WadReader
    {
        public static Map ExtractDoomMap()
        {
            WadFile wad = LoadWad(
                "C:\\Users\\Alexa\\Downloads\\New folder\\doom2.wad"
                // "C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\EISBERG.wad"
                // "C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\testmap.wad"
                // "C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\doommap.wad"
            );

            Dictionary<string, BGRA[]> floorTextures = ExtractFloorTextures(wad);
            Dictionary<string, TextureInfo> textures = ExtractTextures(wad);

            foreach ((string name, BGRA[] image) in floorTextures)
            {
                TextureCache.Add(name, 64, 64, image);
            }

            foreach ((string name, var info) in textures)
            {
                TextureCache.Add(name, info.Width, info.Height, info.Data);
            }

            WadLump? textMap = wad[LumpType.TextMap];

            if (textMap is not null)
            {
                return ExtractDoomMap(textMap);
            }
            else
            {
                return ExtractDoomMap(wad);
            }
        }

        internal sealed class TextureInfo
        {
            public readonly int Width;
            public readonly int Height;
            public readonly BGRA[] Data;

            public TextureInfo(int width, int height, BGRA[] data)
            {
                Width = width;
                Height = height;
                Data = data;
            }
        }

        public static unsafe Dictionary<string, TextureInfo> ExtractTextures(WadFile wad)
        {
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);
            Span<string> patchNames = WadLumpParser.ReadPNames(wad[LumpType.PNames]);
            Span<TextureDefinition> textureList = WadLumpParser.ReadTexture(wad[LumpType.Texture1]);

            Dictionary<string, TextureInfo> textures = [];

            const int normalPalette = 0;
            ReadOnlySpan<BGRA> palette = ToBGRA(playPal[normalPalette]);

            for (int t = 0; t < textureList.Length; t++)
            {
                ref TextureDefinition textureDefinition = ref textureList[t];
                int height = textureDefinition.Height;
                int width = textureDefinition.Width;

                BGRA[] texture = new BGRA[textureDefinition.Width * textureDefinition.Height];
                ref BGRA textureRef = ref MemoryMarshal.GetArrayDataReference(texture);

                for (int patchIndex = 0; patchIndex < textureDefinition.Patches.Length; patchIndex++)
                {
                    ref PatchDescriptor patch = ref textureDefinition.Patches[patchIndex];
                    string patchName = patchNames[patch.Number];

                    PatchHeader header = WadLumpParser.ReadPatch(wad[patchName]);

                    int originX = Math.Max((short)0, patch.XOffset);
                    int originY = Math.Max((short)0, patch.YOffset);

                    for (int col = 0; col < header.Width; col++)
                    {
                        int x = originX + col;

                        // clip to texture bounds
                        if (x < 0 || x >= width)
                        {
                            continue;
                        }

                        ReadOnlySpan<Post> column = CollectionsMarshal.AsSpan(header.Columns[col]);

                        foreach (Post post in column)
                        {
                            for (int s = 0; s < post.Length; s++)
                            {
                                int y = originY + post.TopDelta;

                                for (int i = 0; i < post.Length; i++)
                                {
                                    if (y + i >= 0 && y + i < height)
                                    {
                                        byte paletteIndex = post.Data[i];

                                        // skip transparent pixels
                                        if (paletteIndex == 0)
                                        {
                                            continue;
                                        }
                                        else if (paletteIndex == 255)
                                        {
                                            break;
                                        }

                                        int destY = y + i;

                                        BGRA color = palette[paletteIndex];

                                        int index = x + (destY) * width;

                                        Unsafe.Add(ref textureRef, index) = color;
                                    }
                                }

                            }

                        }

                    }
                }

                textures[textureDefinition.Name] = new TextureInfo(textureDefinition.Width, textureDefinition.Height, texture);
            }

            return textures;
        }

        public static unsafe Dictionary<string, BGRA[]> ExtractFloorTextures(WadFile wad)
        {
            Dictionary<int, byte[]> colorMaps = WadLumpParser.ReadColorMap(wad[LumpType.ColorMap]);
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);

            const int brightestColormap = 0;
            const int normalPalette = 0;
            ReadOnlySpan<BGRA> palette = ToBGRA(playPal[normalPalette]);
            ReadOnlySpan<byte> colorMap = colorMaps[brightestColormap];

            Dictionary<string, BGRA[]> flats = [];

            // pallette 0 is used in most situations
            // byte 0 will have the number of the palette color

            for (int i = 0; i < wad.Lumps.Count; i++) {
                WadLump wadLump = wad.Lumps[i];

                if (!wadLump.IsFlat || wadLump.Bytes.Length == 0)
                {
                    continue;
                }

                ReadOnlySpan<byte> bytes = wadLump.Bytes;
                BGRA[] texture = new BGRA[bytes.Length];
                ref BGRA textureRef = ref MemoryMarshal.GetArrayDataReference(texture);

                for (int c = 0; c < bytes.Length; c++)
                {
                    int colorMapIndex = bytes[c];
                    int paletteIndex = colorMap[colorMapIndex];
                    BGRA color = palette[paletteIndex];
                    Unsafe.Add(ref textureRef, c) = color;
                }

                flats[wadLump.Name] = texture;
            }

            return flats;
        }

        public static Models.Json.Map ExtractDoomMap(WadFile wad)
        {
            Span<Vertex> verticies = WadLumpParser.ReadVertexes(wad[LumpType.Vertexes]);
            Span<Sidedef> sideDefs = WadLumpParser.ReadSideDefs(wad[LumpType.SideDefs]);
            Span<Linedef> lineDefs = WadLumpParser.ReadLineDefs(wad[LumpType.LineDefs]);
            Span<Sector> sectorDefs = WadLumpParser.ReadSectors(wad[LumpType.Sectors]);
            Span<Thing> things = WadLumpParser.ReadThings(wad[LumpType.Things]);

            Thing? player1Start = null;
            for (int i = 0; i < things.Length; i++)
            {
                Thing thing = things[i];

                if (thing.Type == ThingType.Player1Start)
                {
                    player1Start = thing;
                    break;
                }
            }

            if (!player1Start.HasValue)
            {
                throw new ArgumentException("No Player 1 Start", nameof(wad));
            }

            var sectorToLinedefs = WadReader.GetSectorToLineDefs(lineDefs, sideDefs, sectorDefs.Length);

            var sectors = new List<MapSector>(sectorDefs.Length);

            for (int i = 0; i < sectorDefs.Length; i++)
            {
                ref Sector sector = ref sectorDefs[i];

                if (!sectorToLinedefs.TryGetValue(i, out List<LineInfo>? lines))
                {
                    continue;
                }

                float ceiling = sector.CeilingHeight;
                float floor = sector.FloorHeight;

                MapSector mapSector = new MapSector {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    CeilingTexture = sector.CeilingTexture,
                    FloorTexture = sector.FloorTexture
                };

                foreach (LineInfo lineInfo in lines)
                {
                    ref Linedef linedef = ref lineDefs[lineInfo.LineDefId];
                    Vertex vertex1 = verticies[linedef.Vertex1];
                    Vertex vertex2 = verticies[linedef.Vertex2];

                    var line = new Line
                    {
                        Id = lineInfo.LineDefId,
                        PointA = ToVector(vertex1),
                        PointB = ToVector(vertex2),
                        SectorTo = lineInfo.ParentSectorId,
                        UpperTexture = lineInfo.UpperTexture,
                        MiddleTexture = lineInfo.MiddleTexture,
                        LowerTexture = lineInfo.LowerTexture,
                        YOffset = lineInfo.YOffset,
                        XOffset = lineInfo.XOffset
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            return new Models.Json.Map
            {
                PlayerStart = new PlayerStart
                {
                    Angle = player1Start.Value.Angle,
                    XPosition = player1Start.Value.X,
                    YPosition = player1Start.Value.Y,
                    ZPosition = 0f
                },
                Sectors = sectors
            };
        }

        public static Models.Json.Map ExtractDoomMap(WadLump textLump)
        {
            var map = WadLumpParser.ReadTextMap(textLump);

            ReadOnlySpan<UdmfSector> sectorDefs = CollectionsMarshal.AsSpan(map.Sectors);
            ReadOnlySpan<UdmfLinedef> lineDefs = CollectionsMarshal.AsSpan(map.Linedefs);
            ReadOnlySpan<UdmfVertex> verticies = CollectionsMarshal.AsSpan(map.Vertices);
            ReadOnlySpan<UdmfThing> things = CollectionsMarshal.AsSpan(map.Things);

            UdmfThing? player1Start = null;
            for (int i = 0; i < things.Length; i++)
            {
                UdmfThing thing = things[i];

                if (thing.Type == (int)ThingType.Player1Start)
                {
                    player1Start = thing;
                    break;
                }
            }

            if (player1Start == null)
            {
                throw new ArgumentException("No Player 1 Start", nameof(textLump));
            }

            Dictionary<int, List<LineInfo>> sectorToLinedefs = GetSectorToLineDefs(map, sectorDefs.Length);

            var sectors = new List<MapSector>();

            for (int i = 0; i < sectorDefs.Length; i++)
            {
                UdmfSector sector = sectorDefs[i];

                if (!sectorToLinedefs.TryGetValue(i, out List<LineInfo>? lines))
                {
                    continue;
                }

                float ceiling = sector.HeightCeiling;
                float floor = sector.HeightFloor;

                MapSector mapSector = new MapSector {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = sector.TextureFloor,
                    CeilingTexture = sector.TextureCeiling
                };

                foreach (LineInfo lineInfo in lines)
                {
                    UdmfLinedef linedef = lineDefs[lineInfo.LineDefId];
                    UdmfVertex vertex1 = verticies[linedef.V1];
                    UdmfVertex vertex2 = verticies[linedef.V2];

                    var line = new Line
                    {
                        Id = lineInfo.LineDefId,
                        PointA = ToVector(vertex1),
                        PointB = ToVector(vertex2),
                        SectorTo = lineInfo.ParentSectorId,
                        UpperTexture = lineInfo.UpperTexture,
                        MiddleTexture = lineInfo.MiddleTexture,
                        LowerTexture = lineInfo.LowerTexture,
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            return new Models.Json.Map
            {
                PlayerStart = new PlayerStart
                {
                    Angle = player1Start.Angle,
                    XPosition = player1Start.X,
                    YPosition = player1Start.Y,
                    ZPosition = 0f
                },
                Sectors = sectors
            };
        }

        public static WadFile LoadWad(string filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);

            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            var wadFile = new WadFile();

            byte[] buffer4 = new byte[4];
            byte[] buffer8 = new byte[8];

            int i;

            using FileStream fs = new FileStream(filePath, FileMode.Open);

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

                switch (lumpName)
                {
                    case LumpType.PStart:
                        isPatches = true;
                        break;
                    case LumpType.PEnd:
                        isPatches = false;
                        break;
                    case LumpType.FStart:
                        isFlats = true;
                        break;
                    case LumpType.FEnd:
                        isFlats = false;
                        break;
                }

                // Debug.WriteLine($"{lumpName}, patch? {isPatches}, flat? {isFlats}");

                byte[] lumpbytes = new byte[lumpSize];

                if (lumpSize != 0)
                {
                    fs.Seek(lumpOffset, SeekOrigin.Begin);
                    fs.ReadExactly(lumpbytes, 0, (int)lumpSize);
                }

                wadFile.Lumps.Add(new WadLump(lumpName, lumpbytes, isFlats, isPatches));
            }

            return wadFile;
        }

        internal sealed class LineInfo
        {
            public readonly int ParentSectorId;
            public readonly int LineDefId;
            public readonly string? UpperTexture;
            public readonly string? MiddleTexture;
            public readonly string? LowerTexture;
            public readonly int XOffset;
            public readonly int YOffset;

            public LineInfo(int parentSectorId, int lineDefId, string? upperTexture, string? middleTexture, string? lowerTexture, int xOffset, int yOffset)
            {
                ParentSectorId = parentSectorId;
                LineDefId = lineDefId;
                UpperTexture = upperTexture;
                MiddleTexture = middleTexture;
                LowerTexture = lowerTexture;
                XOffset = xOffset;
                YOffset = yOffset;
            }
        }

        public static Dictionary<int, List<LineInfo>> GetSectorToLineDefs(UdmfMapData textMap, int sectors)
        {
            ReadOnlySpan<UdmfLinedef> lineDefs = CollectionsMarshal.AsSpan(textMap.Linedefs);
            ReadOnlySpan<UdmfSidedef> sideDefs = CollectionsMarshal.AsSpan(textMap.Sidedefs);

            var sectorToLineDefs = new Dictionary<int, List<LineInfo>>(sectors);

            for (int i = 0; i < lineDefs.Length; i++)
            {
                UdmfLinedef linedef = lineDefs[i];

                UdmfSidedef? leftDef = linedef.SidedefFront is int sidedefFront ? sideDefs[sidedefFront] : null;
                UdmfSidedef? rightDef = linedef.SidedefBack is int sidedefBack ? sideDefs[sidedefBack] : null;

                if (leftDef?.Sector is int leftSector)
                {
                    AddSectorLineDef(leftSector, i, rightDef?.Sector ?? -1, leftDef);
                }

                if (rightDef?.Sector is int rightSector)
                {
                    AddSectorLineDef(rightSector, i, leftDef?.Sector ?? -1, rightDef);
                }
            }

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId, UdmfSidedef sidedef)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo(parentSectorId, linedefId, sidedef.TextureTop, sidedef.TextureMiddle, sidedef.TextureBottom, sidedef.XOffset ?? 0, sidedef.YOffset ?? 0));
            }
        }

        public static Dictionary<int, List<LineInfo>> GetSectorToLineDefs(
            ReadOnlySpan<Linedef> lineDefs,
            ReadOnlySpan<Sidedef> sideDefs,
            int sectors)
        {
            var sectorToLineDefs = new Dictionary<int, List<LineInfo>>(sectors);

            for (int i = 0; i < lineDefs.Length; i++)
            {
                Linedef linedef = lineDefs[i];

                Sidedef? leftDef = linedef.HasSideDefLeft ? sideDefs[linedef.SidedefLeft] : null;
                Sidedef? rightDef = linedef.HasSideDefRight ? sideDefs[linedef.SidedefRight] : null;

                if (leftDef is Sidedef left)
                {
                    AddSectorLineDef(left.Sector, i, rightDef is null ? -1 : rightDef.Value.Sector, left);
                }

                if (rightDef is Sidedef right)
                {
                    AddSectorLineDef(right.Sector, i, leftDef is null ? - 1: leftDef.Value.Sector, right);
                }
            }

            Debug.WriteLine($"{sectors} vs {sectorToLineDefs.Count}");

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId, Sidedef sidedef)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo(parentSectorId, linedefId, sidedef.UpperTextureNullable, sidedef.MiddleTextureNullable, sidedef.LowerTextureNullable, sidedef.XOffset, sidedef.YOffset));
            }
        }

        public static Dictionary<int, List<int>> GetLineDefsToVectors(ReadOnlySpan<Linedef> lineDefs)
        {
            var lineDefsToVectors = new Dictionary<int, List<int>>();

            for (int i = 0; i < lineDefs.Length; i++)
            {
                Linedef linedef = lineDefs[i];
                AddSectorLineDef(i, linedef.Vertex1);
                AddSectorLineDef(i, linedef.Vertex2);
            }

            return lineDefsToVectors;

            void AddSectorLineDef(int lineDefId, int vertexId)
            {
                if (!lineDefsToVectors.TryGetValue(lineDefId, out List<int>? vertexes))
                {
                    vertexes = [];
                    lineDefsToVectors[lineDefId] = vertexes;
                }

                vertexes.Add(vertexId);
            }
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

        private unsafe static void DebugTexture(int width, int height, Span<BGRA> texture, string textureName)
        {
            var info = new SKImageInfo(width, height)
            {
                AlphaType = SKAlphaType.Premul,
                ColorType = SKColorType.Bgra8888,
            };

            fixed (BGRA* bgraPtr = &texture[0])
            {
                var image = SKImage.FromPixels(info, (nint)bgraPtr, info.RowBytes);

                using var data = image.Encode(SKEncodedImageFormat.Png, 100); // 100 = max quality
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"C:\\Users\\Alexa\\Downloads\\New folder\\tex\\{textureName}.PNG");
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }

                Console.WriteLine($"Image saved to {outputPath}");
            }
        }

        [SkipLocalsInit]
        private static ReadOnlySpan<BGRA> ToBGRA(ReadOnlySpan<RGB> rgb) {
            ref RGB color = ref MemoryMarshal.GetReference(rgb);

            Span<BGRA> bgra = new BGRA[rgb.Length];

            for (int i = 0; i < rgb.Length; i++)
            {
                // Calling "new BGRA" is extremely slow
                unchecked
                {
                    const uint Alpha = (uint)byte.MaxValue << 24;
                    uint b = color.B;
                    uint g = (uint)color.G << 8;
                    uint r = (uint)color.R << 16;

                    bgra[i] = b | g | r | Alpha;
                }

                color = ref Unsafe.Add(ref color, 1);
            }

            return bgra;
        }

        private static Point ToVector(UdmfVertex vertex)
        {
            return new Point(vertex.X, vertex.Y);
        }

        private static Point ToVector(Vertex vertex)
        {
            return new Point(vertex.X, vertex.Y);
        }

    }
}
