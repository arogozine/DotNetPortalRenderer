using RenderingEngine.DoomMapLoader.Map;
using RenderingEngine.DoomMapLoader.Udmf;
using RenderingEngine.DoomMapLoader.Wad;
using RenderingEngine.Models.Json;
using System.Runtime.InteropServices;
using System.Text;

namespace RenderingEngine.DoomMapLoader
{
    internal static class WadReader
    {
        public static Models.Json.Map ExtractDoomMap()
        {
            WadFile test = WadReader.LoadWad(

                "C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\testmap.wad"
            // "C:\\Users\\Alexa\\source\\repos\\DoomStruct\\src\\test\\resources\\doommap.wad"
            );
            var textMap = test["TEXTMAP"];

            if (textMap is not null)
            {
                return ExtractDoomMap(textMap);
            }

            var vertexes = test["VERTEXES"];

            Span<Vertex> verticies = WadReader.ReadVertexes(vertexes);
            Span<Sidedef> sideDefs = WadReader.ReadSideDefs(test["SIDEDEFS"]);
            Span<Linedef> lineDefs = WadReader.ReadLineDefs(test["LINEDEFS"]);
            Span<Sector> sectorDefs = WadReader.ReadSectors(test["SECTORS"]);
            Span<Thing> things = WadReader.ReadThings(test["THINGS"]);

            Thing? player1Start = null;
            for (int i = 0; i < things.Length; i++)
            {
                Thing thing = things[i];

                if (thing.Type == (short)1)
                {
                    player1Start = thing;
                }
            }

            var sectorToLinedefs = WadReader.GetSectorToLineDefs(lineDefs, sideDefs);

            var sectors = new List<MapSector>();

            for (int i = 0; i < sectorDefs.Length; i++)
            {
                Sector sector = sectorDefs[i];

                if (!sectorToLinedefs.TryGetValue(i, out List <(int LineDefId, int ParentSectorId)>? lines))
                {
                    continue;
                }

                float ceiling = sector.CeilingHeight;
                float floor = sector.FloorHeight;

                MapSector mapSector = new MapSector { SectorId = i, Ceiling = ceiling, Floor = floor };

                foreach ((int lineId, int parentSectorId) in lines)
                {
                    Linedef linedef = lineDefs[lineId];
                    Vertex vertex1 = verticies[linedef.Vertex1];
                    Vertex vertex2 = verticies[linedef.Vertex2];

                    var line = new Line
                    {
                        WallId = lineId,
                        PointA = ToVector(vertex1),
                        PointB = ToVector(vertex2),
                        SectorTo = parentSectorId
                    };

                    // Debug.WriteLine($"line: {lineId}, {linedef.Vertex1} {linedef.Vertex2} {parentSectorId}");
                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            return new Models.Json.Map
            {
                Player = new MapPlayer
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
            var map = ReadTextMap(textLump);

            ReadOnlySpan<UdmfSector> sectorDefs = CollectionsMarshal.AsSpan(map.Sectors);
            ReadOnlySpan<UdmfLinedef> lineDefs = CollectionsMarshal.AsSpan(map.Linedefs);
            ReadOnlySpan<UdmfVertex> verticies = CollectionsMarshal.AsSpan(map.Vertices);
            ReadOnlySpan<UdmfThing> things = CollectionsMarshal.AsSpan(map.Things);

            UdmfThing? player1Start = null;
            for (int i = 0; i < things.Length; i++)
            {
                UdmfThing thing = things[i];

                if (thing.Type == (short)1)
                {
                    player1Start = thing;
                }
            }


            var sectorToLinedefs = GetSectorToLineDefs(map);

            var sectors = new List<MapSector>();

            for (int i = 0; i < sectorDefs.Length; i++)
            {
                UdmfSector sector = sectorDefs[i];

                if (!sectorToLinedefs.TryGetValue(i, out List<(int LineDefId, int ParentSectorId)>? lines))
                {
                    continue;
                }

                float ceiling = sector.HeightCeiling!.Value;
                float floor = sector.HeightFloor!.Value;

                MapSector mapSector = new MapSector { SectorId = i, Ceiling = ceiling, Floor = floor };

                foreach ((int lineId, int parentSectorId) in lines)
                {
                    UdmfLinedef linedef = lineDefs[lineId];
                    UdmfVertex vertex1 = verticies[linedef.V1];
                    UdmfVertex vertex2 = verticies[linedef.V2];

                    var line = new Line
                    {
                        WallId = lineId,
                        PointA = ToVector(vertex1),
                        PointB = ToVector(vertex2),
                        SectorTo = parentSectorId
                    };

                    // Debug.WriteLine($"line: {lineId}, {linedef.Vertex1} {linedef.Vertex2} {parentSectorId}");
                    mapSector.Walls.Add(line);
                }

                sectors.Add(mapSector);
            }

            return new Models.Json.Map
            {
                Player = new MapPlayer
                {
                    Angle = player1Start.Angle,
                    XPosition = player1Start.X,
                    YPosition = player1Start.Y,
                    ZPosition = 0f
                },
                Sectors = sectors
            };
        }

        private static Vector ToVector(UdmfVertex vertex)
        {
            return new Vector(vertex.X, vertex.Y);
        }

        private static Vector ToVector(Vertex vertex)
        {
            return new Vector(vertex.X, vertex.Y);
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
            uint lumpCount = BitConverter.ToUInt32(buffer4);
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

                byte[] lumpbytes = new byte[lumpSize];
                fs.Seek(lumpOffset, SeekOrigin.Begin);
                fs.ReadExactly(lumpbytes, 0, (int)lumpSize);

                wadFile.Lumps.Add(new WadLump(lumpName, lumpbytes));
            }

            return wadFile;
        }

        public static unsafe Span<Vertex> ReadVertexes(WadLump vertexLump)
        {
            if (vertexLump.Name != "VERTEXES")
            {
                throw new ArgumentException("Not a Vertex Lump", nameof(vertexLump));
            }

            Span<byte> bytes = vertexLump.Bytes;

            if (bytes.Length % sizeof(Vertex) != 0)
            {
                throw new ArgumentException("Can't deserialize", nameof(vertexLump));
            }

            return MemoryMarshal.Cast<byte, Vertex>(bytes);
        }

        public static UdmfMapData ReadTextMap(WadLump textLump)
        {
            if (textLump.Name != "TEXTMAP")
            {
                throw new ArgumentException("Not a Text Map Lump", nameof(textLump));
            }

            string text = Encoding.ASCII.GetString(textLump.Bytes);

            return UdmfParser.Parse(text);
        }

        public static unsafe Span<Sidedef> ReadSideDefs(WadLump vertexLump)
        {
            const int sideDefSize = 30;

            if (vertexLump.Name != "SIDEDEFS")
            {
                throw new ArgumentException("Not a SideDef Lump", nameof(vertexLump));
            }

            Span<byte> bytes = vertexLump.Bytes;

            if (bytes.Length % sideDefSize != 0)
            {
                throw new ArgumentException("Can't deserialize", nameof(vertexLump));
            }

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

        public static unsafe Span<Linedef> ReadLineDefs(WadLump lineDefLump)
        {
            if (lineDefLump.Name != "LINEDEFS")
            {
                throw new ArgumentException("Not a Vertex Lump", nameof(lineDefLump));
            }

            Span<byte> bytes = lineDefLump.Bytes;

            if (bytes.Length % sizeof(Linedef) != 0)
            {
                throw new ArgumentException("Can't deserialize", nameof(lineDefLump));
            }

            return MemoryMarshal.Cast<byte, Linedef>(bytes);
        }

        public static Span<Sector> ReadSectors(WadLump sectorLump)
        {
            const int sectorSize = 26;

            if (sectorLump.Name != "SECTORS")
            {
                throw new ArgumentException("Not a Vertex Lump", nameof(sectorLump));
            }

            Span<byte> bytes = sectorLump.Bytes;

            if (bytes.Length % sectorSize != 0)
            {
                throw new ArgumentException("Can't deserialize", nameof(sectorLump));
            }

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

        public unsafe static Span<Thing> ReadThings(WadLump thingsLump)
        {
            if (thingsLump.Name != "THINGS")
            {
                throw new ArgumentException("Not a Things Lump", nameof(thingsLump));
            }

            Span<byte> bytes = thingsLump.Bytes;

            if (bytes.Length % sizeof(Thing) != 0)
            {
                throw new ArgumentException("Can't deserialize", nameof(thingsLump));
            }

            return MemoryMarshal.Cast<byte, Thing>(bytes);
        }

        public static Dictionary<int, List<(int LineDefId, int ParentSectorId)>> GetSectorToLineDefs(UdmfMapData textMap)
        {
            ReadOnlySpan<UdmfLinedef> lineDefs = CollectionsMarshal.AsSpan(textMap.Linedefs);
            ReadOnlySpan<UdmfSidedef> sideDefs = CollectionsMarshal.AsSpan(textMap.Sidedefs);

            var sectorToLineDefs = new Dictionary<int, List<(int LineDefId, int SectorId)>>();

            for (int i = 0; i < lineDefs.Length; i++)
            {
                UdmfLinedef linedef = lineDefs[i];

                UdmfSidedef? leftDef = linedef.SidedefFront is int sidedefFront ? sideDefs[sidedefFront] : null;
                UdmfSidedef? rightDef = linedef.SidedefBack is int sidedefBack ? sideDefs[sidedefBack] : null;

                if (leftDef?.Sector is int leftSector)
                {
                    AddSectorLineDef(leftSector, i, rightDef?.Sector ?? -1);
                }

                if (rightDef?.Sector is int rightSector)
                {
                    AddSectorLineDef(rightSector, i, leftDef?.Sector ?? -1);
                }
            }

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<(int LineDefId, int ParentSectorId)>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add((linedefId, parentSectorId));
            }
        }

        public static Dictionary<int, List<(int LineDefId, int ParentSectorId)>> GetSectorToLineDefs(
            ReadOnlySpan<Linedef> lineDefs,
            ReadOnlySpan<Sidedef> sideDefs)
        {
            var sectorToLineDefs = new Dictionary<int, List<(int LineDefId, int SectorId)>>();

            for (int i = 0; i < lineDefs.Length; i++)
            {
                Linedef linedef = lineDefs[i];

                Sidedef? leftDef = linedef.HasSideDefLeft ? sideDefs[linedef.SidedefLeft] : null;
                Sidedef? rightDef = linedef.HasSideDefRight ? sideDefs[linedef.SidedefRight] : null;

                if (leftDef is Sidedef left)
                {
                    AddSectorLineDef(left.Sector, i, rightDef is null ? -1 : rightDef.Value.Sector);
                }

                if (rightDef is Sidedef right)
                {
                    AddSectorLineDef(right.Sector, i, leftDef is null ? - 1: leftDef.Value.Sector);
                }
            }

            return sectorToLineDefs;

            void AddSectorLineDef(int sectorId, int linedefId, int parentSectorId)
            {
                if (!sectorToLineDefs.TryGetValue(sectorId, out List<(int LineDefId, int ParentSectorId)>? sectorLineDefs))
                {
                    sectorLineDefs = [];
                    sectorToLineDefs[sectorId] = sectorLineDefs;
                }

                sectorLineDefs.Add((linedefId, parentSectorId));
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
    }
}
