namespace RenderingEngine.Engine
{
    internal sealed class RenderableAreaAndZBuffer
    {
        public required float[] Depth { get; set; }
        public required int[] WallStart { get; set; }
        public required int[] WallEnd { get; set; }
        public required RenderColumnStatus[] ColumnStatus { get; init; }

        [SetsRequiredMembers]
        public RenderableAreaAndZBuffer(int width)
        {
            Depth = new float[width];
            WallStart = new int[width];
            WallEnd = new int[width];
            ColumnStatus = new RenderColumnStatus[width];
        }

        public RenderableAreaAndZBuffer() { }
    }
}
