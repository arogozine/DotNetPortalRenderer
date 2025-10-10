namespace RenderingEngine.DoomMapLoader.Map
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Linedef
    {
        public readonly short Vertex1;

        public readonly short Vertex2;
        
        public readonly LinedefFlags Flags;
        
        public readonly short Type;
        
        public readonly short Tag;
        
        public readonly short SidedefRight;

        public readonly short SidedefLeft;

        public readonly bool HasSideDefLeft => SidedefLeft != -1;

        public readonly bool HasSideDefRight => SidedefRight != -1;

        public Linedef(short vertex1, short vertex2, LinedefFlags flags, short type, short tag, short sidedefLeft, short sidedefRight)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Flags = flags;
            Type = type;
            Tag = tag;
            SidedefRight = sidedefRight;
            SidedefLeft = sidedefLeft;
        }
    }
}
