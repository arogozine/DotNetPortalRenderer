using RenderingEngine.Engine;

namespace RenderingEngine.Models
{
    internal abstract class RenderableSpriteSnapshot
    {
        public required int XLeft { get; init; }
        public required int XRight { get; init; }

        public Span<float> Depth => RenderableArea.Depth;
        public Span<int> WallStart => RenderableArea.WallStart;
        public Span<int> WallEnd => RenderableArea.WallEnd;
        public Span<RenderColumnStatus> ColumnStatus => RenderableArea.ColumnStatus;

        public RenderableAreaAndZBuffer RenderableArea { get; }

        public RenderableSpriteSnapshot(RenderableAreaAndZBuffer area)
        {
            RenderableArea = area;
        }
    }

    /// <summary>
    /// Render Window Snapshot for a certain depth. For transparent wall rendering.
    /// </summary>
    internal class RenderWindowWallSnapshot : RenderableSpriteSnapshot
    {
        public RenderWindowWallSnapshot(RenderableAreaAndZBuffer area) : base(area)
        {
        }

        public required int Offset { get; init; }
        public required RenderableWall Wall { get; init; }
    }

    /// <summary>
    /// Render Window Snapshot for a certain depth. For sprite rendering.
    /// </summary>
    internal class RenderWindowSpriteSnapshot : RenderableSpriteSnapshot
    {
        public RenderWindowSpriteSnapshot(RenderableAreaAndZBuffer area) : base(area)
        {
        }

        public required int RenderDepth { get; init; }
        public required HashSet<int> RenderedSectors { get; init; }
    }
}
