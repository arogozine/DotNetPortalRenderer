using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class WallToRender
    {
        public required int FromX { get; init; }
        public required int ToX { get; init; }
        public required Wall Wall { get; init; }

    }
    internal sealed class NeighborsToRender
    {
        public required int SectorId { get; init; }

        public int? FromX { get; set; }
        public int? ToX { get; set; }

        public List<WallToRender> Walls { get; } = [];

        public (int FromX, int ToX) GetXCoordinates()
        {
            return (FromX ?? Walls.Min(x => x.FromX), ToX ?? Walls.Max(x => x.ToX));
        }
    }
}
