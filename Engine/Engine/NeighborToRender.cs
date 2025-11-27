using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class NeighborsToRender
    {
        public NeighborsToRender() {
            ParentWalls = [];
        }

        public NeighborsToRender(RenderableWall renderableWall, Span<Wall> walls)
        {
            this.RenderableWall = renderableWall;
            ParentWalls = new Wall[walls.Length + 1];
            walls.CopyTo(ParentWalls);
            ParentWalls[^1] = renderableWall.Wall;
        }

        public required int SectorId { get; init; }
        public RenderableWall? RenderableWall { get; init; }
        public Wall[] ParentWalls { get; }
        public required int Depth { get; init; }
    }
}
