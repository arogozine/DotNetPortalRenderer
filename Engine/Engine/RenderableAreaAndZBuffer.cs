namespace RenderingEngine.Engine
{
    internal sealed record RenderableAreaAndZBuffer(
        int[] CeilingStart,
        int[] FloorEnd,
        int[] WallEnd,
        float[] ZBuffer,
        RenderColumnStatus[] ColumnStatus);
}
