using DoomAssetLoader;
using DoomAssetLoader.Map;
using DoomAssetLoader.Texture;
using DoomAssetLoader.Udmf;
using DoomAssetLoader.Wad;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using SkiaSharp;
using System.ComponentModel;
using System.Reflection;
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
                return ExtractDoomMap(textMap, mapName);
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
            Dictionary<string, TextureInfo> floorTextures = ExtractFloorTextures(wad);
            Dictionary<string, TextureInfo> textures = ExtractTextures(wad);
            Dictionary<string, TextureInfo> sprites = ExtractSprites(wad);

            foreach ((string name, var info) in floorTextures)
            {
                TextureCache.Add(name, new DoomTexture(name, info.Width, info.Height, info.Data));
            }

            foreach ((string name, var info) in textures)
            {
                TextureCache.Add(name, new DoomTexture(name, info.Width, info.Height, info.Data));
            }

            foreach ((string name, var info) in sprites)
            {
                TextureCache.Add(name, new DoomTexture(name, info.Width, info.Height, info.Data));
            }

            ExtractAllPallettes(wad);
        }

        public static void ExtractAllPallettes(WadFile wad)
        {
            Dictionary<int, byte[]> colorMaps = WadLumpParser.ReadColorMap(wad[LumpType.ColorMap]);
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);

            const int normalPalette = 0;
            ReadOnlySpan<BGRA> lookup = ToBGRA(playPal[normalPalette]);

            for (int i = 0; i < colorMaps.Count; i++)
            {
                ReadOnlySpan<byte> colorMap = colorMaps[i];
                BGRA[] palette = new BGRA[colorMap.Length];

                for (int j = 0; j < colorMap.Length; j++)
                {
                    palette[j] = lookup[colorMap[j]];
                }

                TextureCache.AddPallette(i, palette);
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

        private static bool TryDecodeImage(byte[] lumpBytes, [NotNullWhen(true)] out BGRA[]? bgra, out int width, out int height)
        {
            SKImage? image = SKImage.FromEncodedData(lumpBytes);

            if (image is null)
            {
                bgra = null;
                width = height = default;
                return false;
            }

            width = image.Width;
            height = image.Height;
            SKImageInfo info = new(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

            unsafe
            {
                bgra = new BGRA[info.BytesSize / sizeof(BGRA)];

                fixed (BGRA* ptr = bgra)
                {
                    return image.ReadPixels(info, (IntPtr)ptr, info.RowBytes, 0, 0);
                }
            }
        }

        public static Dictionary<string, TextureInfo> ExtractSprites(WadFile wad)
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

                // PNG
                if (WadLumpParser.IsPng(wadLump) && TryDecodeImage(wadLump.Bytes, out BGRA[]? png, out int width, out int height))
                {
                    textures[wadLump.Name] = new TextureInfo(width, height, png)
                    {
                        LeftOffset = 0,
                        TopOffset = 0
                    };

                    continue;
                }

                PatchHeader header = WadLumpParser.ReadPatchOrSprite(wadLump);

                BGRA[] texture = new BGRA[header.Width * header.Height];
                ref BGRA textureRef = ref MemoryMarshal.GetArrayDataReference(texture);

                for (int col = 0; col < header.Width; col++)
                {
                    ReadOnlySpan<Post> column = CollectionsMarshal.AsSpan(header.Columns[col]);

                    foreach (Post post in column)
                    {
                        int y = post.TopDelta;

                        for (int i = 0; i < post.Length; i++)
                        {
                            byte paletteIndex = post.Data[i];
                            int destY = y + i;

                            BGRA color = palette[paletteIndex];

                            int index = col + (destY) * header.Width;

                            Unsafe.Add(ref textureRef, index) = color;
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

        public static Dictionary<string, TextureInfo> ExtractTextures(WadFile wad)
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

        private static void ExtractTextures(WadFile wad, WadLump textureLump, Dictionary<string, TextureInfo> textures)
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

                textures[textureDefinition.Name] = new TextureInfo(textureDefinition.Width, textureDefinition.Height, texture);
            }
        }

        public static Dictionary<string, TextureInfo> ExtractFloorTextures(WadFile wad)
        {
            Dictionary<int, byte[]> colorMaps = WadLumpParser.ReadColorMap(wad[LumpType.ColorMap]);
            Dictionary<int, RGB[]> playPal = WadLumpParser.ReadPlaypal(wad[LumpType.PlayPal]);

            const int brightestColormap = 0;
            const int normalPalette = 0;
            ReadOnlySpan<BGRA> palette = ToBGRA(playPal[normalPalette]);
            ReadOnlySpan<byte> colorMap = colorMaps[brightestColormap];

            Dictionary<string, TextureInfo> flats = [];

            // pallette 0 is used in most situations
            // byte 0 will have the number of the palette color

            for (int i = 0; i < wad.Lumps.Count; i++)
            {
                WadLump wadLump = wad.Lumps[i];

                if (!wadLump.IsFlat || wadLump.Bytes.Length == 0)
                {
                    continue;
                }

                // PNG
                if (WadLumpParser.IsPng(wadLump) && TryDecodeImage(wadLump.Bytes, out BGRA[]? png, out int width, out int height))
                {
                    flats[wadLump.Name] = new TextureInfo(width, height, png);
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

                flats[wadLump.Name] = new TextureInfo(64, 64, texture);
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
            List<Sprite> sprites = ExtractSprites(things);

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

            var sectorToLinedefs = GetSectorToLineDefs(lineDefs, sideDefs, sectorDefs.Length);

            var sectors = new List<MapSector>(sectorDefs.Length);

            for (int i = 0; i < sectorDefs.Length; i++)
            {
                ref Sector sector = ref sectorDefs[i];

                if (!sectorToLinedefs.TryGetValue(i, out List<LineInfo>? lines))
                {
                    continue;
                }

                short ceiling = sector.CeilingHeight;
                short floor = sector.FloorHeight;

                MapSector mapSector = new()
                {
                    Id = i,
                    Settings = default,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = new Models.TextureInfo { Texture = TextureCache.GetTexture(sector.FloorTexture) },
                    CeilingTexture = new Models.TextureInfo { Texture = TextureCache.GetTexture(sector.CeilingTexture) },
                    FloorShade = sector.LightLevel,
                    CeilingShade = sector.LightLevel
                };

                foreach (LineInfo lineInfo in lines)
                {
                    ref Linedef linedef = ref lineDefs[lineInfo.LineDefId];
                    Vertex vertex1 = verticies[linedef.Vertex1];
                    Vertex vertex2 = verticies[linedef.Vertex2];

                    var line = new Line
                    {
                        Id = lineInfo.LineDefId,
                        TwoSided = true,
                        PointA = lineInfo.Left ? ToVector(vertex1, linedef.Vertex1) : ToVector(vertex2, linedef.Vertex2),
                        PointB = lineInfo.Left ? ToVector(vertex2, linedef.Vertex2) : ToVector(vertex1, linedef.Vertex1),
                        SectorTo = lineInfo.ParentSectorId,
                        UpperTexture = ToTextureInfo(lineInfo.UpperTexture, lineInfo.XOffsetTop, lineInfo.YOffsetTop, !linedef.Flags.HasFlag(LinedefFlags.DontPegTop)),
                        MiddleTexture = ToTextureInfo(lineInfo.MiddleTexture, lineInfo.XOffsetMid, lineInfo.YOffsetMid, linedef.Flags.HasFlag(LinedefFlags.DontPegBottom)),
                        LowerTexture = ToTextureInfo(lineInfo.LowerTexture, lineInfo.XOffsetBottom, lineInfo.YOffsetBottom, linedef.Flags.HasFlag(LinedefFlags.DontPegBottom)),
                        Shade = sector.LightLevel
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            DetermineSkybox(sectors, mapName);

            RecalculateOffsets(sectors);

            float radians = MathF.PI * (player1Start.Value.Angle / 180f);

            return new Map
            {
                Player = new Player
                {
                    Angle = radians,
                    Where = (player1Start.Value.X, player1Start.Value.Y, 0f)
                },
                Sprites = sprites.ToArray(),
                Sectors = sectors
            };
        }

        #region Re-Calculate Offsets

        // these can be positive, negative, and crazy big too
        // we want to normalize them to [0, texture size - 1]

        private static void RecalculateOffsets(List<MapSector> sectorList)
        {
            Span<MapSector> sectors = CollectionsMarshal.AsSpan(sectorList);

            foreach (MapSector sector in sectors)
            {
                foreach (Line line in sector.Walls)
                {
                    Models.TextureInfo? middleTextureInfo = line.MiddleTexture;

                    if (line.SectorTo is int sectorTo)
                    {
                        int sectorHeight = sector.Ceiling - sector.Floor;

                        MapSector neighborSector = sectors[sectorTo];
                        int floorOffset = neighborSector.Floor - sector.Floor;
                        int ceilOffset = neighborSector.Ceiling - sector.Ceiling;

                        if (floorOffset < 0)
                        {
                            floorOffset = 0;
                        }

                        if (ceilOffset > 0)
                        {
                            ceilOffset = 0;
                        }

                        // don't draw beyond the bounds
                        if (ceilOffset < -sectorHeight)
                        {
                            ceilOffset = -sectorHeight;
                        }

                        if (floorOffset > sectorHeight)
                        {
                            floorOffset = sectorHeight;
                        }

                        Models.TextureInfo lowerTextureInfo = line.LowerTexture!;

                        Models.TextureInfo upperTextureInfo = line.UpperTexture!;

                        Texture lowerTexture = TextureCache.GetTexture(lowerTextureInfo);
                        Texture upperTexture = TextureCache.GetTexture(upperTextureInfo);

                        lowerTextureInfo.XOffset = DetermineXOffset(lowerTextureInfo, lowerTexture);
                        upperTextureInfo.XOffset = DetermineXOffset(upperTextureInfo, upperTexture);

                        lowerTextureInfo.YOffset = DetermineLowerTextureYOffset(floorOffset, lowerTextureInfo, lowerTexture);
                        upperTextureInfo.YOffset = DetermineUpperTextureYOffset(ceilOffset, upperTextureInfo, upperTexture);

                        if (middleTextureInfo is not null)
                        {
                            Texture middleTexture = TextureCache.GetTexture(middleTextureInfo);
                            middleTextureInfo.XOffset = DetermineXOffset(middleTextureInfo, middleTexture);
                            // middleTextureInfo.YOffset = DetermineTextureYOffset(sector, middleTextureInfo, in middleTexture);
                        }
                    }
                    else if (middleTextureInfo is not null)
                    {
                        Texture middleTexture = TextureCache.GetTexture(middleTextureInfo);
                        middleTextureInfo.XOffset = DetermineXOffset(middleTextureInfo, middleTexture);
                        middleTextureInfo.YOffset = DetermineTextureYOffset(sector, middleTextureInfo, middleTexture);
                    }
                }
            }
        }

        private static int DetermineLowerTextureYOffset(
            int floorOffset,
            Models.TextureInfo textureInfo,
            Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = floorOffset;

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom - offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }

        private static int DetermineUpperTextureYOffset(
            int ceilingOffset,
            Models.TextureInfo textureInfo,
            Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = -ceilingOffset;

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom + offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }


        private static int DetermineTextureYOffset(
            MapSector sector,
            Models.TextureInfo textureInfo,
            Texture wallTexture)
        {
            int offset = textureInfo.YOffset;
            TextureRenderingOptions renderingOptions = textureInfo.RenderingOptions;
            int textureHeight = wallTexture.Height;
            int sectorHeight = sector.Ceiling - sector.Floor;

            offset = EnsureOffsetIsPositive(textureHeight, offset);

            if (renderingOptions.HasFlag(TextureRenderingOptions.FromBottom) || renderingOptions.HasFlag(TextureRenderingOptions.FromSectorBottom))
            {
                int offsetFromBottom = DetermineTextureOffsetFromBottom(textureHeight, sectorHeight);
                offset = offsetFromBottom + offset;
            }

            return EnsureOffsetIsPositive(textureHeight, offset);
        }

        private static int DetermineTextureOffsetFromBottom(int textureHeight, int sectorHeight)
        {
            if (sectorHeight >= textureHeight)
            {
                return sectorHeight % textureHeight;
            }
            else
            {
                return textureHeight - sectorHeight;
            }
        }

        private static int DetermineXOffset(Models.TextureInfo textureInfo, Texture wallTexture)
        {
            int offset = textureInfo.XOffset;
            int textureWidth = wallTexture.Width;

            if (offset < 0)
            {
                offset = textureWidth + offset;
            }

            return EnsureOffsetIsPositive(wallTexture.Width, offset);
        }

        private static int EnsureOffsetIsPositive(int textureHeight, int offset)
        {
            offset %= textureHeight;

            if (offset < 0)
            {
                offset = textureHeight + offset;
            }

            return offset;
        }

        #endregion

        private static void PrecalculateWallSprites(scoped ReadOnlySpan<Sprite> sprites)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                Models.TextureInfo textureInfo = sprite.Texture;
                Texture texture = TextureCache.GetTexture(textureInfo);

                float textureWidth = texture.Width * (textureInfo.XScale ?? 1f);

                (float x, float y) = sprite.Location;

                // calculate the x, y for the wall on the screen for both points
                float rx1 = x - textureWidth / 2f;
                float rx2 = x + textureWidth / 2f;
                float ry1 = y;
                float ry2 = y;

                if (sprite is WallSprite)
                {
                    (float sin, float cos) = MathF.SinCos(sprite.Angle);

                    (rx1, ry1) = SharedHelpers.RotateVertex(rx1, ry1, sin, cos, x, y);
                    (rx2, ry2) = SharedHelpers.RotateVertex(rx2, ry2, sin, cos, x, y);

                    rx1 += x;
                    ry1 += y;
                    rx2 += x;
                    ry2 += y;
                }

                sprite.Texture.Alpha = 1f;
                sprite.Length = textureWidth;
                sprite.PointA = new Point(rx1, ry1);
                sprite.PointB = new Point(rx2, ry2);
            }
        }

        private static void DetermineSkybox(List<MapSector> sectors, string mapName)
        {
            var defaultTexture = new Models.TextureInfo
            {
                Texture = TextureCache.GetTexture((string?)null)
            };

            // This seems to be hard coded,
            // https://doomwiki.org/wiki/Sky
            _ = int.TryParse(mapName.ToUpperInvariant().Replace("MAP", string.Empty), out int mapNumber);
            string skyTexture = mapNumber <= 11 ? "SKY1" : mapNumber <= 20 ? "SKY2" : "SKY3";

            const string placeholderSkyTextureName = "F_SKY";

            foreach (MapSector sector in sectors)
            {
                bool hasSkyBox = sector.CeilingTexture.Name.StartsWith(placeholderSkyTextureName, StringComparison.OrdinalIgnoreCase);
                bool hasFloorBox = sector.FloorTexture.Name.StartsWith(placeholderSkyTextureName, StringComparison.OrdinalIgnoreCase);

                if (hasSkyBox)
                {
                    sector.CeilingTexture.Texture = TextureCache.GetTexture(skyTexture);
                    sector.CeilingTexture.RenderingOptions |= TextureRenderingOptions.Skybox;
                }

                if (hasFloorBox)
                {
                    sector.FloorTexture.Texture = TextureCache.GetTexture(skyTexture);
                    sector.FloorTexture.RenderingOptions |= TextureRenderingOptions.Skybox;
                }
            }

            foreach (MapSector sector in sectors)
            {
                bool hasSkyBox = sector.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox);

                if (!hasSkyBox)
                {
                    continue;
                }

                sector.CeilingShade = byte.MaxValue;

                foreach (Line wall in sector.Walls)
                {
                    if (wall.SectorTo is int sectorId)
                    {
                        MapSector neightbor = sectors[sectorId];

                        if (neightbor.CeilingTexture.RenderingOptions.HasFlag(TextureRenderingOptions.Skybox))
                        {
                            wall.UpperTexture = new Models.TextureInfo
                            {
                                Texture = TextureCache.GetTexture(skyTexture),
                                RenderingOptions = TextureRenderingOptions.Skybox
                            };
                            wall.Shade = byte.MaxValue;
                        }
                        else
                        {
                            wall.UpperTexture ??= new Models.TextureInfo
                            {
                                Texture = TextureCache.GetTexture(skyTexture),
                                RenderingOptions = TextureRenderingOptions.Skybox
                            };
                            wall.Shade = byte.MaxValue;
                        }
                    }
                    else
                    {
                        wall.MiddleTexture ??= new Models.TextureInfo
                        {
                            Texture = TextureCache.GetTexture(skyTexture),
                            RenderingOptions = TextureRenderingOptions.Skybox
                        };
                        wall.Shade = byte.MaxValue;
                    }
                }
            }


            foreach (MapSector sector in sectors)
            {
                foreach (Line wall in sector.Walls)
                {
                    wall.UpperTexture ??= defaultTexture;
                    if (wall.SectorTo is null)
                        wall.MiddleTexture ??= defaultTexture;
                    wall.LowerTexture ??= defaultTexture;
                }
            }
        }

        private static List<Sprite> ExtractSprites(ReadOnlySpan<UdmfThing> things)
        {
            var spriteLookup = GetMultiAngleSprites();

            List<Sprite> sprites = new(things.Length);

            for (int i = 0; i < things.Length; i++)
            {
                UdmfThing thing = things[i];

                switch ((ThingType)thing.Type)
                {
                    case ThingType.DeathmatchStart:
                    case ThingType.TeleportLanding:
                    case ThingType.SpawnSpot:
                    case ThingType.MonsterSpawner:
                        continue;
                }

                _ = spriteLookup.TryGetValue((ThingType)thing.Type, out SpriteAnimationAngle? spriteAnimationAngle);

                var firstTexture = spriteAnimationAngle?.AnimationToAngleToTexture?.First()?.First().Texture ??
                    TextureCache.GetTexture((string?)null);

                sprites.Add(new Sprite
                {
                    Id = i,
                    Angle = DetermineAngleInRadians(thing.Angle),
                    Location = new Point(thing.X, thing.Y),
                    Height = thing.Height ?? 0f,
                    Texture = new Models.TextureInfo { Texture = firstTexture },
                    AnimationAngle = spriteAnimationAngle
                });
            }

            PrecalculateWallSprites(CollectionsMarshal.AsSpan(sprites));

            return sprites;
        }

        private sealed record DoomTextureInfo(
                string Name, char AnimationFrame, float Angle, bool Flipped);

        private static Dictionary<ThingType, SpriteAnimationAngle> GetMultiAngleSprites()
        {
            // https://doomwiki.org/wiki/Sprite

            string[] textures = TextureCache.TextureNames.Where(Valid).ToArray();
            (ThingType Val, string Code)[] thingCodes = GetThingCodes();
            var multiAngleTextures = new Dictionary<ThingType, SpriteAnimationAngle>(119);

            for (int i = 0; i < thingCodes.Length; i++)
            {
                (ThingType Thing, string Code) = thingCodes[i];

                var foundTextures = textures.Where(x => x.StartsWith(Code))
                    .ToList();

                // "NONE"
                if (foundTextures.Count == 0)
                {
                    continue;
                }

                var animationFrames = foundTextures
                    .SelectMany(GetAnimFrameAndAngle)
                    .GroupBy(x => x.AnimationFrame)
                    .OrderBy(g => g.Key)
                    .ToArray();

                TextureAngle[][] animationFramesA = new TextureAngle[animationFrames.Length][];

                for (int a = 0; a < animationFrames.Length; a++)
                {
                    DoomTextureInfo[] frames = animationFrames[a]
                        .OrderBy(x => x.Angle)
                        .ToArray();

                    animationFramesA[a] = new TextureAngle[frames.Length];

                    for (int t = 0; t < frames.Length; t++)
                    {
                        DoomTextureInfo frame = frames[t];
                        animationFramesA[a][t] = new TextureAngle(frame.Angle, TextureCache.GetTexture(frame.Name), frame.Flipped);
                    }
                }

                multiAngleTextures[Thing] = new SpriteAnimationAngle
                {
                    AnimationToAngleToTexture = animationFramesA
                };
            }

            return multiAngleTextures;

            static bool Valid(string texture)
            {
                switch (texture.Length)
                {
                    case 4:
                        return true;
                    case 6:
                        return !char.IsDigit(texture[4]) && char.IsDigit(texture[5]);
                    case 8:
                        return !char.IsDigit(texture[4]) && char.IsDigit(texture[5])
                            && !char.IsDigit(texture[6]) && char.IsDigit(texture[7]);
                }

                return false;
            }

            static DoomTextureInfo[] GetAnimFrameAndAngle(string texture)
            {
                if (texture.Length == 4)
                {
                    return [new (texture, 'A', 0f, false)];
                }

                if (texture.Length == 6)
                {
                    return [
                        new (texture, texture[4], DetermineAngle(texture[5]), false)
                    ];
                }

                if (texture.Length == 8)
                {
                    return [
                        new (texture, texture[4], DetermineAngle(texture[5]), false),
                        new (texture, texture[6], DetermineAngle(texture[7]), true),
                    ];
                }

                throw new NotImplementedException();
            }

            static float DetermineAngle (char angleChar)
            {
                Debug.Assert(char.IsDigit(angleChar));

                if (angleChar == '0')
                {
                    return 0f;
                }

                int angle = (angleChar - '1') * 45;
                Debug.Assert(angle >= 0 && angle <= (360 - 45));
                return DetermineAngleInRadians(angle);
            }

            static (ThingType Val, string Code)[] GetThingCodes()
            {
                ThingType[] thingTypes = Enum.GetValues<ThingType>();
                var valToCode = new (ThingType Val, string Code)[thingTypes.Length];

                for (int i = 0; i < thingTypes.Length; i++)
                {
                    ThingType thing = thingTypes[i];

                    FieldInfo field = typeof(ThingType).GetField(thingTypes[i].ToString())!;
                    DescriptionAttribute? descriptionAttribute = field
                        .GetCustomAttribute<DescriptionAttribute>();
                    Debug.Assert(descriptionAttribute != null);

                    valToCode[i] = (thing, descriptionAttribute.Description);
                }

                return valToCode;
            }
        }

        private static List<Sprite> ExtractSprites(Span<Thing> things)
        {
            var spriteLookup = GetMultiAngleSprites();

            List<Sprite> sprites = new(things.Length);

            for (int i = 0; i < things.Length; i++)
            {
                ref Thing thing = ref things[i];

                switch (thing.Type)
                {
                    case ThingType.DeathmatchStart:
                    case ThingType.TeleportLanding:
                    case ThingType.SpawnSpot:
                    case ThingType.MonsterSpawner:
                        continue;
                }

                var firstTexture = spriteLookup[thing.Type].AnimationToAngleToTexture
                    .First()
                    .First()
                    .Texture;

                sprites.Add(new Sprite
                {
                    Id = i,
                    Angle = DetermineAngleInRadians(thing.Angle),
                    Location = new Point(thing.X, thing.Y),
                    Height = 0f,
                    Texture = new Models.TextureInfo { Texture = firstTexture },
                    AnimationAngle = spriteLookup[thing.Type]
                });
            }

            PrecalculateWallSprites(CollectionsMarshal.AsSpan(sprites));

            return sprites;
        }

        private static Map ExtractDoomMap(WadLump textLump, string mapName)
        {
            var map = WadLumpParser.ReadTextMap(textLump);

            ReadOnlySpan<UdmfSector> sectorDefs = CollectionsMarshal.AsSpan(map.Sectors);
            ReadOnlySpan<UdmfLinedef> lineDefs = CollectionsMarshal.AsSpan(map.Linedefs);
            ReadOnlySpan<UdmfVertex> verticies = CollectionsMarshal.AsSpan(map.Vertices);
            ReadOnlySpan<UdmfThing> things = CollectionsMarshal.AsSpan(map.Things);
            List<Sprite> sprites = ExtractSprites(things);

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

                int ceiling = sector.HeightCeiling;
                int floor = sector.HeightFloor;

                MapSector mapSector = new()
                {
                    Id = i,
                    Settings = default,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = GetFloorTextureInfo(sector),
                    CeilingTexture = GetCeilingTextureInfo(sector),
                    FloorShade = sector.LightLevel,
                    CeilingShade = sector.LightLevel,
                    RotationCeiling = ToRadians(sector.RotationCeiling),
                    RotationFloor = ToRadians(sector.RotationFloor)
                };

                foreach (LineInfo lineInfo in lines)
                {
                    UdmfLinedef linedef = lineDefs[lineInfo.LineDefId];
                    UdmfVertex vertex1 = verticies[linedef.V1];
                    UdmfVertex vertex2 = verticies[linedef.V2];

                    Models.TextureInfo? middleTexture = ToTextureInfo(lineInfo.MiddleTexture, lineInfo.XOffsetMid, lineInfo.YOffsetMid, lineInfo.LowerUnpegged);
                    if (middleTexture is not null)
                    {
                        middleTexture.Alpha = linedef.Alpha ?? 1f;
                    }

                    bool twoSided = linedef.TwoSided ?? false;

                    var line = new Line
                    {
                        Id = lineInfo.LineDefId,
                        PointA = lineInfo.Left ? ToVector(vertex2, linedef.V2) : ToVector(vertex1, linedef.V1),
                        PointB = lineInfo.Left ? ToVector(vertex1, linedef.V1) : ToVector(vertex2, linedef.V2),
                        SectorTo = lineInfo.ParentSectorId,
                        UpperTexture = ToTextureInfo(lineInfo.UpperTexture, lineInfo.XOffsetTop, lineInfo.YOffsetTop, !lineInfo.LowerUnpegged),
                        MiddleTexture = middleTexture,
                        LowerTexture = ToTextureInfo(lineInfo.LowerTexture, lineInfo.XOffsetBottom, lineInfo.YOffsetBottom, lineInfo.UpperUnpegged),
                        Shade = sector.LightLevel,
                        TwoSided = twoSided
                    };

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            DetermineSkybox(sectors, mapName);
            RecalculateOffsets(sectors);

            return new Map
            {
                Player = new Player
                {
                    Angle = player1Start.Angle,
                    Where = (player1Start.X, player1Start.Y, 0f)
                },
                Sprites = sprites.ToArray(),
                Sectors = sectors
            };

            static Models.TextureInfo GetCeilingTextureInfo(UdmfSector sector)
            {
                return new Models.TextureInfo
                {
                    Texture = TextureCache.GetTexture(sector.TextureCeiling),
                    XOffset = ToInt32(sector.XPanningCeiling),
                    YOffset = ToInt32(sector.YPanningCeiling)
                };
            }

            static Models.TextureInfo GetFloorTextureInfo(UdmfSector sector)
            {
                return new Models.TextureInfo
                {
                    Texture = TextureCache.GetTexture(sector.TextureFloor),
                    XOffset = ToInt32(sector.XPanningFloor),
                    YOffset = ToInt32(sector.YPanningFloor)
                };
            }

            [return: NotNullIfNotNull(nameof(angle))]
            static float? ToRadians(float? angle)
            {
                if (angle is null || angle == 0f)
                {
                    return null;
                }

                return MathF.PI * (angle / 180f);
            }

            static int ToInt32(float? value) => (int)(value ?? 0f);
        }

        internal sealed class LineInfo
        {
            public required bool Left { get; init; }
            public required int? ParentSectorId { get; init; }
            public required int LineDefId { get; init; }
            public required string? UpperTexture { get; init; }
            public required string? MiddleTexture { get; init; }
            public required string? LowerTexture { get; init; }
            public required int XOffsetTop { get; init; }
            public required int YOffsetTop { get; init; }
            public required int XOffsetMid { get; init; }
            public required int YOffsetMid { get; init; }
            public required int XOffsetBottom { get; init; }
            public required int YOffsetBottom { get; init; }
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
                    AddSectorLineDef(leftSector, i, rightDef?.Sector, leftDef, linedef, true);
                }

                if (rightDef?.Sector is int rightSector)
                {
                    AddSectorLineDef(rightSector, i, leftDef?.Sector, rightDef, linedef, false);
                }
            }

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int? parentSectorId, UdmfSidedef sidedef, UdmfLinedef linedef, bool left)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo
                {
                    Left = !left,
                    ParentSectorId = parentSectorId,
                    LineDefId = linedefId,
                    UpperTexture = sidedef.TextureTop,
                    MiddleTexture = sidedef.TextureMiddle,
                    LowerTexture = sidedef.TextureBottom,
                    XOffsetTop = (int?)sidedef.XOffsetTop ?? sidedef.XOffset ?? 0,
                    YOffsetTop = (int?)sidedef.YOffsetTop ?? sidedef.YOffset ?? 0,
                    XOffsetMid = (int?)sidedef.XOffsetMid ?? sidedef.XOffset ?? 0,
                    YOffsetMid = (int?)sidedef.YOffsetMid ?? sidedef.YOffset ?? 0,
                    XOffsetBottom = (int?)sidedef.XOffsetBottom ?? sidedef.XOffset ?? 0,
                    YOffsetBottom = (int?)sidedef.YOffsetBottom ?? sidedef.YOffset ?? 0,
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
                    AddSectorLineDef(left.Sector, i, rightDef?.Sector, ref left, ref linedef, true);
                }

                if (rightDef is Sidedef right)
                {
                    AddSectorLineDef(right.Sector, i, leftDef?.Sector, ref right, ref linedef, false);
                }
            }

            // Debug.WriteLine($"{sectors} vs {sectorToLineDefs.Count}");

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int? parentSectorId, ref Sidedef sidedef, ref Linedef linedef, bool left)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<LineInfo>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add(new LineInfo
                {
                    Left = !left,
                    ParentSectorId = parentSectorId,
                    LineDefId = linedefId,
                    UpperTexture = sidedef.UpperTextureNullable,
                    MiddleTexture = sidedef.MiddleTextureNullable,
                    LowerTexture = sidedef.LowerTextureNullable,
                    XOffsetTop = sidedef.XOffset,
                    YOffsetTop = sidedef.YOffset,
                    XOffsetMid = sidedef.XOffset,
                    YOffsetMid = sidedef.YOffset,
                    XOffsetBottom = sidedef.XOffset,
                    YOffsetBottom = sidedef.YOffset,
                    LowerUnpegged = (linedef.Flags & LinedefFlags.DontPegBottom) == LinedefFlags.DontPegBottom,
                    UpperUnpegged = (linedef.Flags & LinedefFlags.DontPegTop) == LinedefFlags.DontPegTop
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
        private static ReadOnlySpan<BGRA> ToBGRA(ReadOnlySpan<RGB> rgb)
        {
            ref RGB color = ref MemoryMarshal.GetReference(rgb);

            Span<BGRA> bgra = new BGRA[rgb.Length];

            for (int i = 0; i < rgb.Length; i++)
            {
                // Calling "new BGRA" is extremely slow
                const uint Alpha = (uint)byte.MaxValue << 24;
                uint b = color.B;
                uint g = (uint)color.G << 8;
                uint r = (uint)color.R << 16;

                bgra[i] = b | g | r | Alpha;

                color = ref Unsafe.Add(ref color, 1);
            }

            return bgra;
        }

        private static LineVector ToVector(UdmfVertex vertex, int id)
        {
            return new LineVector(id, new Point(vertex.X, vertex.Y));
        }

        private static LineVector ToVector(Vertex vertex, int id)
        {
            return new LineVector(id, new Point(vertex.X, vertex.Y));
        }

        [return: NotNullIfNotNull(nameof(name))]
        private static Models.TextureInfo? ToTextureInfo(string? name, int xOffset, int yOffset, bool renderFromBottom)
        {
            if (name is null)
            {
                return null;
            }

            return new Models.TextureInfo
            {
                Texture = TextureCache.GetTexture(name),
                XOffset = xOffset,
                YOffset = yOffset,
                RenderingOptions = renderFromBottom ? TextureRenderingOptions.FromBottom : TextureRenderingOptions.FromTop,
                Alpha = 1f
            };
        }

        private static float DetermineAngleInRadians(int angle)
        {
            return MathF.PI * (angle / 180f);
        }
    }
}
