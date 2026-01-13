using DoomAssetLoader.Map;
using DoomAssetLoader.Texture;
using DoomAssetLoader.Udmf;
using DoomAssetLoader.Wad;
using System.Text;

namespace DoomAssetLoader
{
    public static class WadLumpParser
    {
        public static bool IsPng(WadLump? textureLump)
        {
            ArgumentNullException.ThrowIfNull(textureLump);

            if ((!textureLump.IsPatch && !textureLump.IsSprite && !textureLump.IsFlat) || textureLump.Bytes.Length == 0)
            {
                throw new ArgumentException("Not a patch / sprite lump");
            }

            Span<byte> bytes = textureLump.Bytes;
            scoped Span<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

            return bytes.StartsWith(pngSignature);
        }

        public static PatchHeader ReadPatchOrSprite(WadLump? patchLump)
        {
            ArgumentNullException.ThrowIfNull(patchLump);

            if ((!patchLump.IsPatch && !patchLump.IsSprite) || patchLump.Bytes.Length == 0)
            {
                throw new ArgumentException("Not a patch / sprite lump");
            }

            Span<byte> bytes = patchLump.Bytes;
            ushort width = MemoryMarshal.Read<ushort>(bytes[0..2]);
            ushort height = MemoryMarshal.Read<ushort>(bytes[2..4]);
            short leftOffset = MemoryMarshal.Read<short>(bytes[4..6]);
            short topOffset = MemoryMarshal.Read<short>(bytes[6..8]);
            uint[] columnOffsets = new uint[width];

            int offset = 8;
            for (int i = 0; i < width; i++, offset += 4)
            {
                uint columnOffset = MemoryMarshal.Read<uint>(bytes[offset..]);
                columnOffsets[i] = columnOffset;
            }

            const byte EndOfColumn = 0xFF;

            List<Post>[] columns = new List<Post>[width];

            for (int i = 0; i < width; i++)
            {
                List<Post> posts = [];
                columns[i] = posts;

                offset = (int)columnOffsets[i];
                int nextOffset = i + 1 < width ? (int)columnOffsets[i + 1] : bytes.Length;

                while (offset < nextOffset && bytes[offset] != EndOfColumn)
                {
                    byte topDelta = MemoryMarshal.Read<byte>(bytes[offset..]);
                    offset++;
                    byte length = MemoryMarshal.Read<byte>(bytes[offset..]);
                    offset++;
                    // Unused padding byte
                    offset++;
                    byte[] data = bytes[offset..(offset + length)].ToArray();
                    offset += length;
                    // Unused padding byte
                    offset++;

                    posts.Add(new Post(topDelta, length, data));
                }

            }

            var header = new PatchHeader(width, height, leftOffset, topOffset, columns);

            return header;
        }

        public static Span<TextureDefinition> ReadTexture([NotNull] WadLump? texture)
        {
            WadLumpCheck(texture, "TEXTURE");

            Span<byte> bytes = texture.Bytes;     

            // 4-byte long integer N which is the number of textures
            int numberOfTextures = MemoryMarshal.Read<int>(bytes[0..4]);

            int[] textureOffsets = new int[numberOfTextures];

            var list = new TextureDefinition[numberOfTextures];

            int offset = 4;
            for (int texNum = 0; texNum < numberOfTextures; texNum++, offset += 4)
            {
                textureOffsets[texNum] = MemoryMarshal.Read<int>(bytes[offset..(offset + 4)]);
            }

            for (int texNum = 0; texNum < numberOfTextures; texNum++)
            {
                offset = textureOffsets[texNum];

                ReadOnlySpan<byte> offsetBytes = bytes[offset..];

                string name = GetStringFromBytes(offsetBytes[0..8]);
                int masked = MemoryMarshal.Read<int>(offsetBytes[8..12]);
                short width = MemoryMarshal.Read<short>(offsetBytes[12..14]);
                short height = MemoryMarshal.Read<short>(offsetBytes[14..16]);
                int columnDirectory = MemoryMarshal.Read<int>(offsetBytes[16..20]);
                short patchCount = MemoryMarshal.Read<short>(offsetBytes[20..22]);

                var compositeTexture = new TextureDefinition(name, masked, width, height, columnDirectory, patchCount);

                offset = 22;

                for (int j = 0; j < compositeTexture.PatchCount; j++)
                {
                    PatchDescriptor patchDescriptor = MemoryMarshal.Read<PatchDescriptor>(offsetBytes[offset..(offset + 10)]);
                    compositeTexture.Patches[j] = patchDescriptor;
                    offset += 10;
                }

                list[texNum] = compositeTexture;
            }

            return list;
        }

        public static Dictionary<int, RGB[]> ReadPlaypal([NotNull] WadLump? playPalLump)
        {
            const int setSize = 256;

            WadLumpCheck(playPalLump, LumpType.PlayPal, divisor: 256);

            int numberOfSets = playPalLump.Bytes.Length / 256;
            Dictionary<int, RGB[]> colorSets = new(numberOfSets);

            ReadOnlySpan<RGB> bytes = MemoryMarshal.Cast<byte, RGB>(playPalLump.Bytes);

            for (int i = 0; i < bytes.Length; i += setSize)
            {
                colorSets[i] = bytes[i..(i + setSize)].ToArray();
            }

            return colorSets;
        }

        public static Dictionary<int, byte[]> ReadColorMap([NotNull] WadLump? colorMap)
        {
            const int setSize = 256;
            const int numberOfSets = 34;

            WadLumpCheck(colorMap, LumpType.ColorMap, setSize * numberOfSets);

            Dictionary<int, byte[]> colorSets = new(numberOfSets);

            ReadOnlySpan<byte> bytes = colorMap.Bytes;

            for (int i = 0; i < bytes.Length; i += setSize)
            {
                colorSets[i] = bytes[0..(i + setSize)].ToArray();
            }

            return colorSets;
        }

        public static Span<string> ReadPNames([NotNull] WadLump? pNameLump)
        {
            WadLumpCheck(pNameLump, LumpType.PNames);

            ReadOnlySpan<byte> bytes = pNameLump.Bytes;

            uint n = MemoryMarshal.Read<uint>(bytes[0..4]);
            string[] pNames = new string[n];

            for (int i = 0, b = 4; i < n; i++, b += 8)
            {
                pNames[i] = GetStringFromBytes(bytes[b..(b + 8)]);
            }

            return pNames;
        }

        public static unsafe Span<Vertex> ReadVertexes([NotNull] WadLump? vertexLump)
        {
            WadLumpCheck(vertexLump, LumpType.Vertexes, divisor: sizeof(Vertex));

            Span<byte> bytes = vertexLump.Bytes;

            return MemoryMarshal.Cast<byte, Vertex>(bytes);
        }

        public static UdmfMapData ReadTextMap([NotNull] WadLump? textLump)
        {
            WadLumpCheck(textLump, LumpType.TextMap);

            string text = Encoding.ASCII.GetString(textLump.Bytes.AsSpan());

            return UdmfParser.Parse(text);
        }

        public static Span<Sidedef> ReadSideDefs([NotNull] WadLump? sideDefLump)
        {
            const int sideDefSize = 30;

            WadLumpCheck(sideDefLump, LumpType.SideDefs, divisor: sideDefSize);

            Span<byte> bytes = sideDefLump.Bytes;
            Span<Sidedef> sideDefs = new Sidedef[bytes.Length / sideDefSize];

            for (int i = 0, s = 0; i < bytes.Length; i += sideDefSize, s++)
            {
                Span<byte> sideDefBytes = bytes.Slice(i, sideDefSize);

                ushort xOffSet = MemoryMarshal.Read<ushort>(sideDefBytes[..2]);
                ushort yOffSet = MemoryMarshal.Read<ushort>(sideDefBytes[2..4]);
                string upper = GetStringFromBytes(sideDefBytes[4..12]);
                string lower = GetStringFromBytes(sideDefBytes[12..20]);
                string middle = GetStringFromBytes(sideDefBytes[20..28]);
                ushort sector = MemoryMarshal.Read<ushort>(sideDefBytes[28..]);

                sideDefs[s] = new Sidedef(xOffSet, yOffSet, upper, lower, middle, sector);
            }

            return sideDefs;
        }

        public static unsafe Span<Linedef> ReadLineDefs([NotNull] WadLump? lineDefLump)
        {
            WadLumpCheck(lineDefLump, LumpType.LineDefs, divisor: sizeof(Linedef));

            return MemoryMarshal.Cast<byte, Linedef>((Span<byte>)lineDefLump.Bytes);
        }

        public static Span<Sector> ReadSectors([NotNull] WadLump? sectorLump)
        {
            const int sectorSize = 26;

            WadLumpCheck(sectorLump, LumpType.Sectors, divisor: sectorSize);

            Span<byte> bytes = sectorLump.Bytes;
            Span<Sector> sideDefs = new Sector[bytes.Length / sectorSize];

            for (int i = 0, s = 0; i < bytes.Length; i += sectorSize, s++)
            {
                Span<byte> sideDefBytes = bytes.Slice(i, sectorSize);

                short floorHeight = MemoryMarshal.Read<short>(sideDefBytes[..2]);
                short ceilingHeight = MemoryMarshal.Read<short>(sideDefBytes[2..4]);
                string floorTexture = GetStringFromBytes(sideDefBytes[4..12]);
                string ceilingTexture = GetStringFromBytes(sideDefBytes[12..20]);
                short lightLevel = MemoryMarshal.Read<short>(sideDefBytes[20..22]);
                short special = MemoryMarshal.Read<short>(sideDefBytes[22..24]);
                short tag = MemoryMarshal.Read<short>(sideDefBytes[24..26]);

                sideDefs[s] = new Sector(floorHeight, ceilingHeight, floorTexture, ceilingTexture, lightLevel, special, tag);
            }

            return sideDefs;
        }

        public static unsafe Span<Thing> ReadThings([NotNull] WadLump? thingsLump)
        {
            WadLumpCheck(thingsLump, LumpType.Things, divisor: sizeof(Thing));

            return MemoryMarshal.Cast<byte, Thing>((Span<byte>)thingsLump.Bytes);
        }

        private static void WadLumpCheck([NotNull] WadLump? wadLump, string wadName, int? fixedSize = null, int? divisor = null)
        {
            ArgumentNullException.ThrowIfNull(wadLump);

            if (!wadLump.Name.StartsWith(wadName))
            {
                throw new ArgumentException($"WadLump is not a {wadName} lump");
            }

            if (fixedSize is int expectedSide && wadLump.Bytes.Length != expectedSide)
            {
                throw new ArgumentException($"WadLump is not of expected size ({expectedSide})");
            }

            if (divisor is int mod && wadLump.Bytes.Length % mod != 0)
            {
                throw new ArgumentException($"WadLump size is not divisible by {mod}");
            }
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
