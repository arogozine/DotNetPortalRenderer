using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class NeighborsToRender
    {
        public required int SectorId { get; init; }
        public RenderableWall? RenderableWall { get; init; }
        public List<Wall> ParentWalls { get; set; } = [];
    }
}
