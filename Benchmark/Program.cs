using Benchmark.Benchmarks;
using BenchmarkDotNet.Running;
using BuildAssetLoader;
using BuildAssetLoader.Texture;
using RenderingEngine.DoomMapLoader;
using RenderingEngine.Models;
using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Benchmark
{

    internal class Program
    {


        static void Main(string[] args)
        {
            PaletteFile pal = BuildFileLoader.LoadPalFile("D:\\SteamLibrary\\steamapps\\common\\Duke Nukem 3D\\gameroot\\classic\\PALETTE.DAT");
            GrpFile grp = BuildFileLoader.LoadGrpFile("D:\\SteamLibrary\\steamapps\\common\\Duke Nukem 3D\\gameroot\\classic\\DUKE3D.GRP");
            GrpReader.ExtractAllTextures(grp, pal);

            GrpReader.LoadBuildMap(grp, "");
        }

        private static void Test(List<ArtFile> artFiles,PaletteFile paletteFile)
        {
            ReadOnlySpan<BGRA> pal = ToBGRA(MemoryMarshal.Cast<byte, RGB>(paletteFile.Palette));

            int artNum = 0;

            foreach (ArtFile artFile in artFiles)
            {
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
                                // can also add r >= 250u && b >= 250u && g <= 5u ?
                                if (index != byte.MaxValue)
                                {
                                    texture[i] = pal[index];
                                }

                                i++;
                            }
                        }

                        DebugTexture(tile.XSize, tile.YSize, texture, $"ART_{artNum}");
                    }

                    artNum++;
                }
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
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"C:\\Users\\Alexa\\Downloads\\New folder\\tex2\\{textureName}.PNG");
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
    }
}
