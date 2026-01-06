namespace RenderingEngine.Engine
{
    internal sealed record RenderableAreaAndZBuffer(
        int[] CeilingStart,
        int[] FloorEnd,
        float[] ZBuffer,
        RenderColumnStatus[] ColumnStatus);
}
