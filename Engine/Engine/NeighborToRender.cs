using RenderingEngine.Models;

namespace RenderingEngine.Engine
{
    internal sealed class NeighborsToRender
    {
        public required int SectorId { get; init; }
        public Wall? Wall { get; set; }
    }
}
