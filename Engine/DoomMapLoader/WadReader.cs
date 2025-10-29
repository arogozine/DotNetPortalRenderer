using DoomAssetLoader;
using DoomAssetLoader.Map;
using DoomAssetLoader.Texture;
using DoomAssetLoader.Udmf;
using DoomAssetLoader.Wad;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using SkiaSharp;
using Sector = DoomAssetLoader.Map.Sector;

namespace RenderingEngine.DoomMapLoader
{
    [SkipLocalsInit]
    internal static class WadReader
    {
        public static WadFile LoadWad(string path,
            bool loadMaps = false,
            bool loadTextures = false,
            bool loadOther = false,
            string? mapName = null)
        {
            var loader = new WadLoader(path)
            {
                LoadMaps = loadMaps,
                LoadTextures = loadTextures,
                LoadOther = loadOther,
                MapToLoad = mapName
            };

            return loader.LoadWad();
        }

        public static Map LoadDoomMap(WadFile wad, string mapName)
        {
            if (wad.GetMapLump(mapName, LumpType.TextMap) is WadLump textMap)
            {
                return ExtractDoomMap(textMap);
            }
            else
            {
                return ExtractDoomMap(wad, mapName);
            }
        }

        public static WadFile ExtractAllTextures(string path)
        {
            var loader = new WadLoader(path)
            {
                LoadTextures = true
            };

            WadFile wad = loader.LoadWad();
            ExtractAllTextures(wad);
            return wad;
        }

        public static void ExtractAllTextures(WadFile wad)
        {
            Dictionary<string, BGRA[]> floorTextures = ExtractFloorTextures(wad);
            Dictionary<string, TextureInfo> textures = ExtractTextures(wad);
            Dictionary<string, TextureInfo> sprites = ExtractSprites(wad);

            foreach ((string name, BGRA[] image) in floorTextures)
            {
                TextureCache.Add(name, 64, 64, image);
            }

            foreach ((string name, var info) in textures)
            {
                TextureCache.Add(name, info.Width, info.Height, info.Data);
            }

            foreach ((string name, var info) in sprites)
            {
                TextureCache.Add(name, info.Width, info.Height, info.Data);
            }
        }

        internal sealed class TextureInfo
        {
            public int Width { get; }
            public int Height { get; }
            public BGRA[] Data { get; }
            public short LeftOffset { get; init; }
            public short TopOffset { get; init; }

            public TextureInfo(int width, int height, BGRA[] data)
            {
                Width = width;
                Height = height;
                Data = data;
            }
        }

        public static unsafe Dictionary<string, TextureInfo> ExtractSprites(WadFile wad)
        {
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);

            Dictionary<string, TextureInfo> textures = [];

            const int normalPalette = 0;
            ReadOnlySpan<BGRA> palette = ToBGRA(playPal[normalPalette]);

            for (int l = 0; l < wad.Lumps.Count; l++)
            {
                WadLump wadLump = wad.Lumps[l];

                if (!wadLump.IsSprite || wadLump.Bytes.Length == 0)
                {
                    continue;
                }

                if (WadLumpParser.IsPng(wadLump))
                {
                    Debug.WriteLine(wadLump.Name);
                    continue;
                }

                PatchHeader header = WadLumpParser.ReadPatchOrSprite(wadLump);

                BGRA[] texture = new BGRA[header.Width * header.Height];
                ref BGRA textureRef = ref MemoryMarshal.GetArrayDataReference(texture);

                int originX = 0;
                int originY = 0;

                for (int col = 0; col < header.Width; col++)
                {
                    int x = originX + col;

                    ReadOnlySpan<Post> column = CollectionsMarshal.AsSpan(header.Columns[col]);

                    foreach (Post post in column)
                    {
                        for (int s = 0; s < post.Length; s++)
                        {
                            int y = originY + post.TopDelta;

                            for (int i = 0; i < post.Length; i++)
                            {                                
                                byte paletteIndex = post.Data[i];
                                int destY = y + i;

                                BGRA color = palette[paletteIndex];

                                int index = x + (destY) * header.Width;

                                Unsafe.Add(ref textureRef, index) = color;
                            }

                        }

                    }

                }

                textures[wadLump.Name] = new TextureInfo(header.Width, header.Height, texture)
                {
                    LeftOffset = header.LeftOffset,
                    TopOffset = header.TopOffset
                };
            }

            return textures;
        }

        public static unsafe Dictionary<string, TextureInfo> ExtractTextures(WadFile wad)
        {
            Dictionary<string, TextureInfo> textures = [];

            if (wad[LumpType.Texture1] is WadLump textureLump1)
            {
                ExtractTextures(wad, textureLump1, textures);
            }

            if (wad[LumpType.Texture2] is WadLump textureLump2)
            {
                ExtractTextures(wad, textureLump2, textures);
            }

            return textures;
        }

        private static unsafe void ExtractTextures(WadFile wad, WadLump textureLump, Dictionary<string, TextureInfo> textures)
        {
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);
            Span<string> patchNames = WadLumpParser.ReadPNames(wad[LumpType.PNames]);
            Span<TextureDefinition> textureList = WadLumpParser.ReadTexture(textureLump);

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

                    PatchHeader header = WadLumpParser.ReadPatchOrSprite(wad[patchName]);

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

        private static Map ExtractDoomMap(WadFile wad, string mapName)
        {
            Span<Vertex> verticies = WadLumpParser.ReadVertexes(wad.GetMapLump(mapName, LumpType.Vertexes));
            Span<Sidedef> sideDefs = WadLumpParser.ReadSideDefs(wad.GetMapLump(mapName, LumpType.SideDefs));
            Span<Linedef> lineDefs = WadLumpParser.ReadLineDefs(wad.GetMapLump(mapName, LumpType.LineDefs));
            Span<Sector> sectorDefs = WadLumpParser.ReadSectors(wad.GetMapLump(mapName, LumpType.Sectors));
            Span<Thing> things = WadLumpParser.ReadThings(wad.GetMapLump(mapName, LumpType.Things));

            Thing? player1Start = null;
            for (int i = 0; i < things.Length; i++)
            {
                ref Thing thing = ref things[i];

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

                bool hasSkyBox = sector.CeilingTexture.StartsWith("F_SKY", StringComparison.OrdinalIgnoreCase);

                MapSector mapSector = new MapSector
                {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = sector.FloorTexture,
                    CeilingTexture = hasSkyBox ? "SKY1" : sector.CeilingTexture,
                    HasSkybox = hasSkyBox
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
                        LowerUnpegged = (linedef.Flags & LinedefFlags.LowerUnpegged) == LinedefFlags.LowerUnpegged,
                        UpperUnpegged = (linedef.Flags & LinedefFlags.UpperUnpegged) == LinedefFlags.UpperUnpegged,
                        YOffset = lineInfo.YOffset,
                        XOffset = lineInfo.XOffset
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            float radians = MathF.PI * (player1Start.Value.Angle / 180f);

            return new Map
            {
                Player = new Player
                {
                    Angle = radians,
                    Where = (player1Start.Value.X, player1Start.Value.Y, 0f)
                },
                Sectors = sectors
            };
        }

        private static Map ExtractDoomMap(WadLump textLump)
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
                bool hasSkyBox = sector.TextureCeiling.StartsWith("F_SKY", StringComparison.OrdinalIgnoreCase);

                MapSector mapSector = new MapSector {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = sector.TextureFloor,
                    CeilingTexture = hasSkyBox ? "SKY1" : sector.TextureCeiling,
                    HasSkybox = hasSkyBox
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
                        XOffset = lineInfo.XOffset,
                        YOffset = lineInfo.YOffset,
                        LowerUnpegged = lineInfo.LowerUnpegged,
                        UpperUnpegged = lineInfo.UpperUnpegged
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            return new Map
            {
                Player = new Player
                {
                    Angle = player1Start.Angle,
                    Where = (player1Start.X, player1Start.Y, 0f)
                },
                Sectors = sectors
            };
        }

        internal sealed class LineInfo
        {
            public required int ParentSectorId { get; init; }
            public required int LineDefId { get; init; }
            public required string? UpperTexture { get; init; }
            public required string? MiddleTexture { get; init; }
            public required string? LowerTexture { get; init; }
            public required int XOffset { get; init; }
            public required int YOffset { get; init; }
            public required bool LowerUnpegged { get; init; }
            public required bool UpperUnpegged { get; init; }

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
                    AddSectorLineDef(leftSector, i, rightDef?.Sector ?? -1, leftDef, linedef);
                }

                if (rightDef?.Sector is int rightSector)
                {
                    AddSectorLineDef(rightSector, i, leftDef?.Sector ?? -1, rightDef, linedef);
                }
            }

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId, UdmfSidedef sidedef, UdmfLinedef linedef)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo {
                    ParentSectorId = parentSectorId,
                    LineDefId = linedefId,
                    UpperTexture = sidedef.TextureTop,
                    MiddleTexture = sidedef.TextureMiddle,
                    LowerTexture = sidedef.TextureBottom,
                    XOffset = sidedef.XOffset ?? 0,
                    YOffset = sidedef.YOffset ?? 0,
                    LowerUnpegged = linedef.DontPegBottom,
                    UpperUnpegged = linedef.DontPegTop
                });
            }
        }

        public static Dictionary<int, List<LineInfo>> GetSectorToLineDefs(
            Span<Linedef> lineDefs,
            ReadOnlySpan<Sidedef> sideDefs,
            int sectors)
        {
            var sectorToLineDefs = new Dictionary<int, List<LineInfo>>(sectors);

            for (int i = 0; i < lineDefs.Length; i++)
            {
                ref Linedef linedef = ref lineDefs[i];

                Sidedef? leftDef = linedef.HasSideDefLeft ? sideDefs[linedef.SidedefLeft] : null;
                Sidedef? rightDef = linedef.HasSideDefRight ? sideDefs[linedef.SidedefRight] : null;

                if (leftDef is Sidedef left)
                {
                    AddSectorLineDef(left.Sector, i, rightDef is null ? -1 : rightDef.Value.Sector, ref left, ref linedef);
                }

                if (rightDef is Sidedef right)
                {
                    AddSectorLineDef(right.Sector, i, leftDef is null ? - 1: leftDef.Value.Sector, ref right, ref linedef);
                }
            }

            // Debug.WriteLine($"{sectors} vs {sectorToLineDefs.Count}");

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId, ref Sidedef sidedef, ref Linedef linedef)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo
                {
                    ParentSectorId = parentSectorId,
                    LineDefId = linedefId,
                    UpperTexture = sidedef.UpperTextureNullable,
                    MiddleTexture = sidedef.MiddleTextureNullable,
                    LowerTexture = sidedef.LowerTextureNullable,
                    XOffset = sidedef.XOffset,
                    YOffset = sidedef.YOffset,
                    LowerUnpegged = (linedef.Flags & LinedefFlags.LowerUnpegged) == LinedefFlags.LowerUnpegged,
                    UpperUnpegged = (linedef.Flags & LinedefFlags.UpperUnpegged) == LinedefFlags.UpperUnpegged
                });
            }
        }

        private unsafe static void DebugTexture(int width, int height, Span<BGRA> texture, string textureName)
        {
            textureName = textureName.Replace("\\", "_");

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
