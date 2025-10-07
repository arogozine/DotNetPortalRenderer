namespace RenderingEngine.DoomMapLoader.Map
{
    /// <summary>
    /// These are the beginning and end points for LINEDEFS and SEGS.
    /// Each vertice's record is 4 bytes in 2 short fields.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct Vertex(short X, short Y);
}
