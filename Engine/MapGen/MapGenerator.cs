using RenderingEngine.Models.Json;

namespace RenderingEngine.MapGen
{
    public class MapGenerator
    {
        private int wallId = 0;
        private int sectorId = 0;

        private MapPlayer? player = null;
        private readonly Dictionary<int, MapSector> sectors = [];

        public int AddWall(int sectorId, Vector pointA, Vector pointB, int? toSectorId = null)
        {
            if (!sectors.TryGetValue(sectorId, out MapSector? sector))
            {
                throw new ArgumentException("Sector Not Found", nameof(sectorId));
            }

            if (toSectorId is int x && !sectors.TryGetValue(x, out MapSector? toSector))
            {
                throw new ArgumentException("Sector Not Found", nameof(toSectorId));
            }

            int currentId = wallId++;
            sector.Walls.Add(new Line { PointA = pointA, PointB = pointB, WallId = currentId, SectorTo = toSectorId });


            return currentId;
        }

        public void AddChildSector(int sectorId, int childSectorId)
        {
            if (!sectors.TryGetValue(sectorId, out MapSector? parent))
            {
                throw new ArgumentException("Sector Not Found", nameof(sectorId));
            }

            if (!sectors.ContainsKey(childSectorId))
            {
                throw new ArgumentException("Sector Not Found", nameof(childSectorId));
            }

            parent.Children.Add(childSectorId);
        }

        public int AddSector(float floor, float ceiling)
        {
            int currentId = sectorId++;

            sectors.Add(currentId, new MapSector
            {
                Ceiling = ceiling,
                Floor = floor,
                SectorId = currentId
            });

            return currentId;
        }

        public void AddPlayer(XyzTuple where, float angle)
        {
            player = new MapPlayer
            {
                Angle = angle,
                XPosition = where.X,
                YPosition = where.Y,
                ZPosition = where.Z,
            };
        }
        
        public Map GetMap()
        {
            if (player is null)
            {
                throw new NotSupportedException("Player Required");
            }

            return new Map { Player = player, Sectors = [.. sectors.Values] };
        }
    }
}
