using BuildAssetLoader;
using BuildAssetLoader.Map;
using BuildAssetLoader.Texture;
using RenderingEngine.Engine;
using RenderingEngine.Models;
using SkiaSharp;

namespace RenderingEngine.DoomMapLoader
{
    internal static class GrpReader
    {
        public static Map LoadBuildMap(GrpFile grp, string mapName)
        {
            var map = BuildFileParser.ExtractMapFiles(grp);

            return ExtractBuildMap(map[0], mapName);
        }

        public static void ExtractAllTextures(GrpFile grp, PaletteFile paletteFile)
        {
            List<ArtFile> artFiles = BuildFileParser.ExtractArtFiles(grp);
            Dictionary<string, TextureInfo> textures = ExtractTextures(artFiles, paletteFile);

            foreach ((string name, var info) in textures)
            {
                TextureCache.Add(name, info.Width, info.Height, info.Data);
            }
        }

        private static Map ExtractBuildMap(MapFile mapFile, string mapName)
        {
            StartingPosition startingPosition = mapFile.StartingPosition;
            Span<SectorType> grpSectors = mapFile.Sectors;
            Span<WallType> walls = mapFile.Walls;
            Span<SpriteType> sprites = mapFile.Sprites;

            int ij = 0;

            var sectors = new List<MapSector>(grpSectors.Length);

            for (int i = 0; i < grpSectors.Length; i++)
            {
                ref SectorType sector = ref grpSectors[i];

                float ceiling = sector.CeilingZ;
                float floor = sector.FloorZ;

                string floorTexture = $"TILE_{sector.FloorPicNum}";
                string ceilingTexture = $"TILE_{sector.CeilingPicNum}";

                MapSector mapSector = new MapSector
                {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = new Models.TextureInfo { Name = floorTexture, XOffset = sector.FloorXPanning, YOffset = sector.FloorYPanning },
                    CeilingTexture = new Models.TextureInfo { Name = ceilingTexture, XOffset = sector.CeilingXPanning, YOffset = sector.CeilingYPanning },
                    LightLevel = byte.MaxValue
                };

                int wallStart = sector.WallPtr;
                int wallEnd = wallStart + sector.WallNum;

                for (int j = wallStart; j < wallEnd; j++)
                {
                    ref WallType wall = ref walls[j];
                    ref WallType nextWall = ref walls[wall.Point2];

                    string texture = $"TILE_{wall.PicNum}";

                    var line = new Line {
                        Id = ij,
                        PointA = new LineVector(j, new Point(wall.X, wall.Y)),
                        PointB = new LineVector(wall.Point2, new Point(nextWall.X, nextWall.Y)),
                        LowerTexture = new Models.TextureInfo { Name = texture, XOffset = wall.XRepeat, YOffset = wall.YRepeat },
                        MiddleTexture = new Models.TextureInfo { Name = texture, XOffset = wall.XRepeat, YOffset = wall.YRepeat },
                        SectorTo = wall.NextSector,
                        UpperTexture = new Models.TextureInfo { Name = texture, XOffset = wall.XRepeat, YOffset = wall.YRepeat },
                    };

                    ij++;

                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);

            }

            float radians = MathF.PI * (startingPosition.Angle / 2048f);

            return new Map
            {
                Player = new Player
                {
                    Angle = radians,
                    Where = (startingPosition.PosX, startingPosition.PosY, startingPosition.PosZ)
                },
                Sprites = ExtractSprites(sprites),
                Sectors = sectors
            };
        }

        private static Sprite[] ExtractSprites(Span<SpriteType> spritesTypes)
        {
            Sprite[] sprites = new Sprite[spritesTypes.Length];

            for (int i = 0; i < spritesTypes.Length; i++)
            {
                ref SpriteType thing = ref spritesTypes[i];

                float angle = MathF.PI * (thing.Angle / 2048f);

                string texture = $"TILE_{thing.PicNum}";

                sprites[i] = new Sprite
                {
                    Angle = angle,
                    Location = new Point(thing.X, thing.Y),
                    Height = thing.Z,
                    TextureName = texture
                };
            }

            return sprites;
        }

        private static Dictionary<string, TextureInfo> ExtractTextures(List<ArtFile> artFiles, PaletteFile paletteFile)
        {
            Dictionary<string, TextureInfo> textures = [];

            ReadOnlySpan<BGRA> pal = ToBGRA(MemoryMarshal.Cast<byte, RGB>(paletteFile.Palette));

            Span<ArtFile> artFileSpan = CollectionsMarshal.AsSpan(artFiles);

            for (int s = 0; s < artFileSpan.Length; s++)
            {
                ArtFile artFile = artFileSpan[s];

                uint localTileNum = artFile.LocalTileStart;

                for (int j = 0; j < artFile.Tiles.Length; j++)
                {
                    TileType tile = artFile.Tiles[j];

                    if (tile.Pixels.Length != 0)
                    {
                        BGRA[] texture = new BGRA[tile.Pixels.Length];

                        int i = 0;

                        for (int y = 0; y < tile.YSize; y++)
                        {
                            for (int x = 0; x < tile.XSize; x++)
                            {
                                byte index = tile.Pixels[x * tile.YSize + y];

                                // 255th index is used for transparency
                                // can also add r >= 250 && b >= 250 && g <= 5 ?
                                if (index != byte.MaxValue)
                                {
                                    texture[i] = pal[index];
                                }

                                i++;
                            }
                        }

                        string name = $"TILE_{localTileNum}";

                        textures.Add(name, new TextureInfo(tile.XSize, tile.YSize, texture)
                        {
                            LeftOffset = tile.Properties.OffsetX,
                            TopOffset = tile.Properties.OffsetY,
                        });

                        localTileNum++;

                        // DebugTexture(tile.XSize, tile.YSize, texture, $"ART_{localTileNum}");
                    }
                }
            }

            return textures;
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

        [SkipLocalsInit]
        private static ReadOnlySpan<BGRA> ToBGRA(ReadOnlySpan<RGB> rgb)
        {
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

                    // Only 6-bits are used for color information, so each byte will need to be
                    // multiplied by 4

                    b <<= 2;
                    g <<= 2;
                    r <<= 2;

                    bgra[i] = b | g | r | Alpha;
                }

                color = ref Unsafe.Add(ref color, 1);
            }

            return bgra;
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
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"C:\\Users\\Alexa\\Downloads\\New folder\\tex2\\{textureName}.PNG");
                using (var stream = File.OpenWrite(outputPath))
                {
                    data.SaveTo(stream);
                }

                Console.WriteLine($"Image saved to {outputPath}");
            }
        }
    }
}
