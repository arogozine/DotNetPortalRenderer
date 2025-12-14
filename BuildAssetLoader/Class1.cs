using System;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BuildAssetLoader
{
    public class GrpFile
    {
        public Dictionary<string, byte[]> Files { get; }

        public GrpFile(Dictionary<string, byte[]> files)
        {
            Files = files;
        }
    }

    public class PaletteFile
    {
        public required byte[] Palette { get; init; }
        public required int NumberOfPalLookups { get; init; }
        public required byte[][] PalLookups { get; init; }
        public required byte[] TranslucentLookup { get; init; }
    }

    public class PalletteLoader
    {
        private readonly string filePath;

        public PalletteLoader(string filePath)
        {
            this.filePath = filePath;
        }

        public unsafe PaletteFile LoadPalFile()
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            byte[] buffer2 = new byte[2];

            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // The format is: Red0, Green0, Blue0, Red1, Green1, Blue1, ..., Blue255
            // The colors are based on the VGA 262,144 color palette.  The values range from
            // 0 - 63, so if you want to convert it to a windows palette you will have to
            // multiply each byte by 4.
            byte[] pallette = new byte[768];
            fs.ReadExactly(pallette, 0, pallette.Length);

            // The number of shading tables used
            fs.ReadExactly(buffer2, 0, buffer2.Length);
            int numPalLookups = BitConverter.ToInt16(buffer2);

            // The shading table.
            // The shade tables are often
            // made to go from normal brightness (shade #0) down to pitch black (shade #31)
            var palLookUps = new byte[numPalLookups][];
            for (int i = 0; i < numPalLookups; i++)
            {
                byte[] lookup = new byte[256];
                fs.ReadExactly(lookup, 0, lookup.Length);
                palLookUps[i] = lookup;
            }

            // 4K translucent lookup table.  Given any 2 colors of the palette,
            // this lookup table gives the best match of the 2 colors when mixed together.
            byte[] transluc = new byte[65536];
            fs.ReadExactly(transluc, 0, transluc.Length);


            return new PaletteFile {
                NumberOfPalLookups = numPalLookups,
                Palette = pallette,
                PalLookups = palLookUps,
                TranslucentLookup = transluc
            };
        }
    }

    public class GrpLoader
    {

        private readonly string filePath;

        public GrpLoader(string filePath)
        {
            this.filePath = filePath;
        }

        public GrpFile LoadGrpFile()
        {
            const string signature = "KenSilverman";


            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] buffer4 = new byte[4];
            byte[] buffer12 = new byte[12];


            // Read and Verify Signature
            fs.ReadExactly(buffer12, 0, buffer12.Length);
            string readSignature = GetStringFromBytes(buffer12);

            if (readSignature != signature)
            {
                throw new ArgumentException("Not a Group File", nameof(filePath));
            }

            // Get File Count
            fs.ReadExactly(buffer4, 0, buffer4.Length);
            uint fileCount = BitConverter.ToUInt32(buffer4);

            var fileMeta = new (string, uint)[unchecked((int)fileCount)];
            var files = new Dictionary<string, byte[]>(unchecked((int)fileCount));

            // Read Headers
            for (int i = 0; i < fileCount; i++)
            {
                fs.ReadExactly(buffer12, 0, buffer12.Length);
                string fileName = GetStringFromBytes(buffer12);

                fs.ReadExactly(buffer4, 0, buffer4.Length);
                uint fileSize = BitConverter.ToUInt32(buffer4);

                fileMeta[i] = (fileName, fileSize);
            }

            // Read Files
            for (int i = 0; i < fileCount; i++)
            {
                (string fileName, uint fileSize) = fileMeta[i];

                byte[] file = new byte[fileSize];
                fs.ReadExactly(file, 0, file.Length);

                files[fileName] = file;
            }

            return new GrpFile(files);
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

    // bastART 1.1 by Marijn Kentie (kentieman@tiscalimail.nl).
    // GROK helping figure out VB6 to C# for above utility
    // https://moddingwiki.shikadi.net/wiki/GRP_Format
    // https://github.com/jonof/jfbuild/blob/master/doc/buildinf.txt
    // https://moddingwiki.shikadi.net/wiki/MAP_Format_(Build)

    [Flags]
    public enum Stat : short
    {
        Parallaxing = 1,
        Sloped = 2,

        /// <summary>
        /// swap x&y
        /// </summary>
        SwapXy = 4,


        DoubleSmooshiness = 8,
        XFlip = 16,
        YFlip = 32,

        /// <summary>
        /// Align texture to first wall of sector
        /// </summary>
        AlignTexture = 64
    }


    /// <summary>
    /// 40 byte structure
    /// </summary>
    public readonly struct Sector
    {
        /// <summary>
        /// Index to first wall in sector
        /// </summary>
        private readonly short WallPtr;

        /// <summary>
        /// Number of walls in sector
        /// </summary>
        private readonly short WallNum;

        /// <summary>
        /// Z-coordinate (height) of ceiling at first point of sector
        /// </summary>
        private readonly uint CeilingZ;

        /// <summary>
        /// Z-coordinate (height) of floor at first point of sector
        /// </summary>
        private readonly uint FloorZ;

        private readonly Stat CeilingStat;
        private readonly Stat FloorStat;

        /// <summary>
        /// Ceiling texture (index into ART file)
        /// </summary>
        private readonly short CeilingPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        private readonly short CeilingHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly sbyte CeilingShade;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly byte CeilingPal;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        private readonly byte CeilingXPanning;

        /// <summary>
        /// Texture coordinate X-offset for ceiling
        /// </summary>
        private readonly byte CeilingYPanning;

        /// <summary>
        /// Floor texture (index into ART file)
        /// </summary>
        private readonly short FloorPicNum;

        /// <summary>
        /// Slope value (rise/run; 0 = parallel to floor, 4096 = 45 degrees)
        /// </summary>
        private readonly short FloorHeiNum;

        /// <summary>
        /// Shade offset
        /// </summary>
        private readonly sbyte FloorShade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        private readonly byte FloorPal;

        /// <summary>
        /// Texture coordinate X-offset for floor
        /// </summary>
        private readonly byte FloorXPanning;

        /// <summary>
        /// Texture coordinate Y-offset for floor
        /// </summary>
        private readonly byte FloorYPanning;

        /// <summary>
        /// How fast an area changes shade relative to distance
        /// </summary>
        private readonly byte Visibility;

        /// <summary>
        /// Padding byte
        /// </summary>
        private readonly byte Filler;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        private readonly short LoTag;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        private readonly short HiTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        private readonly short Extra;
    }

    public enum CStat : short
    {
        /// <summary>
        /// Blocking wall (use with clipmove, getzrange)
        /// </summary>
        BlockingWallClipmove = 1,

        /// <summary>
        /// bottoms of invisible walls swapped
        /// </summary>
        BottomsInvisibleWallsSwapped = 2,

        /// <summary>
        /// Align picture on bottom (for doors) 
        /// </summary>
        AlignPictureOnBottom = 4,

        XFlipped = 8,

        MaskingWall =  16,

        OneWayWall = 32,

        /// <summary>
        /// Blocking wall (use with hitscan / cliptype 1)
        /// </summary>
        BlockingWallHitScan = 64,

        Transluscence = 128,

        YFlipped = 256,

        TransluscenceReversing = 521

        /*
	
bit 0: 1 = Blocking wall (use with clipmove, getzrange)
bit 1: 1 = bottoms of invisible walls swapped, 0 = not
bit 2: 1 = align picture on bottom (for doors), 0 = top
bit 3: 1 = x-flipped, 0 = normal
bit 4: 1 = masking wall, 0 = not
bit 5: 1 = 1-way wall, 0 = not
bit 6: 1 = Blocking wall (use with hitscan / cliptype 1)
bit 7: 1 = Transluscence, 0 = not
bit 8: 1 = y-flipped, 0 = normal
bit 9: 1 = Transluscence reversing, 0 = normal
bits 10-15: reserved
         */
    }


    /// <summary>
    /// 32 bytes
    /// </summary>
    public readonly struct Wall
    {
        /// <summary>
        /// X-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly uint X;

        /// <summary>
        /// Y-coordinate of left side of wall (right side coordinate is obtained from the next wall's left side)
        /// </summary>
        public readonly uint Y;

        /// <summary>
        /// Index to next wall on the right (always in the same sector)
        /// </summary>
        public readonly ushort Point2;

        /// <summary>
        /// Index to wall on other side of wall (-1 if there is no sector there)
        /// </summary>
        public readonly short NextWall;

        /// <summary>
        /// Index to sector on other side of wall (-1 if there is no sector)
        /// </summary>
        public readonly short NextSector;

        public readonly CStat CStat;

        /// <summary>
        /// Texture index into ART file
        /// </summary>
        public readonly short PicNum;

        /// <summary>
        /// Texture index into ART file for masked/one-way walls
        /// </summary>
        public readonly short OverPicNum;

        /// <summary>
        /// Shade offset of wall
        /// </summary>
        public readonly sbyte Shade;

        /// <summary>
        /// Palette lookup table number (0 = standard colours)
        /// </summary>
        public readonly byte Pal;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte XRepeat;

        /// <summary>
        /// Offset for aligning textures
        /// </summary>
        public readonly byte YRepeat;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        public readonly short LoTag;

        /// <summary>
        /// Significance is game-specific (Triggers, etc.)
        /// </summary>
        public readonly short HiTag;

        /// <summary>
        /// Significance is game-specific
        /// </summary>
        public readonly short Extra;
    }
}
