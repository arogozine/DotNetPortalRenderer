using BuildAssetLoader.Texture;
using System.Diagnostics;
using System.Text;

namespace BuildAssetLoader
{
    public static class BuildFileLoader
    {
        public static GrpFile LoadGrpFile(string filePath)
        {
            const string signature = "KenSilverman";

            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

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

        public static LookupFile LoadLookupFile(byte[] file)
        {
            using MemoryStream ms = new(file, false);

            return LoadLookupFile(ms);

        }

        public static LookupFile LoadLookupFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return LoadLookupFile(fs);
        }

        public static LookupFile LoadLookupFile(Stream stream)
        {
            int numberOfSwaps = stream.ReadByte();

            var lookupFile = new LookupFile
            {
                NumberOfSwaps = (byte)numberOfSwaps,
                PaletteSwapTables = new byte[numberOfSwaps][]
            };

            for (int i = 0; i < numberOfSwaps; i++)
            {
                byte[] swapTable = new byte[256];

                int palleteSwapIndex = stream.ReadByte() - 1;
                stream.ReadExactly(swapTable);

                lookupFile.PaletteSwapTables[palleteSwapIndex] = swapTable;
            }

            return lookupFile;
        }

        public static PaletteFile LoadPalFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException("Not Found", nameof(filePath));
            }

            byte[] buffer2 = new byte[2];

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

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


            return new PaletteFile
            {
                NumberOfPalLookups = numPalLookups,
                Palette = pallette,
                PalLookups = palLookUps,
                TranslucentLookup = transluc
            };
        }

        private static string GetStringFromBytes(scoped ReadOnlySpan<byte> asciiBytes)
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
