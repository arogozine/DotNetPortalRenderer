using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class NeighborsToRender
    {
        public NeighborsToRender() { }

        public NeighborsToRender(RenderableWall renderableWall)
        {
            this.RenderableWall = renderableWall;
            ParentWalls.Add(renderableWall.Wall);
        }

        public required int SectorId { get; init; }
        public RenderableWall? RenderableWall { get; init; }
        public List<Wall> ParentWalls { get; } = [];
    }
}
