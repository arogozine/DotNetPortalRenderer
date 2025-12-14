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

            var test =ExtractBuildMap(map[0], mapName);

            throw new NotImplementedException();
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
            Span<SectorType> sectors = mapFile.Sectors;
            Span<WallType> walls = mapFile.Walls;

            int ij = 0;

            for (int i = 0; i < sectors.Length; i++)
            {
                ref SectorType sector = ref sectors[i];

                Span<WallType> sectorWalls = sector.GetSectorWalls(walls);

                float ceiling = sector.CeilingZ;
                float floor = sector.FloorZ;

                string floorTexture = $"TILE_{sector.FloorPicNum}";
                string ceilingTexture = $"TILE_{sector.CeilingPicNum}";

                MapSector mapSector = new MapSector
                {
                    Id = i,
                    Ceiling = ceiling,
                    Floor = floor,
                    FloorTexture = new Models.TextureInfo { Name = floorTexture },
                    CeilingTexture = new Models.TextureInfo { Name = ceilingTexture },
                    LightLevel = byte.MaxValue
                };

                for (int j = 0; j < sectorWalls.Length; j++)
                {
                    ref WallType wallType = ref sectorWalls[j];

                    /*
                    var line = new Line {
                        Id = ij,
                        a
                    };
                    */
                    ij++;
                }
            }

            throw new NotImplementedException();
        }

        private static Span<WallType> GetSectorWalls(ref this SectorType sector, Span<WallType> walls)
        {
            return walls[sector.WallPtr..(sector.WallPtr + sector.WallNum)];
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
