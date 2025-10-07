namespace RenderingEngine.DoomMapLoader.Texture
{
    public readonly struct TextureDefinition
    {
        public readonly string Name;
        public readonly int Masked;
        public readonly short Width;
        public readonly short Height;
        public readonly int ColumnDirectory;    // Offset to column directories (patches)
        public readonly short PatchCount;       // Number of patches that compose this texture
        public readonly PatchDescriptor[] Patches;

        public TextureDefinition(string name, int masked, short width, short height, int columnDirectory, short patchCount)
        {
            Name = name;
            Masked = masked;
            Width = width;
            Height = height;
            ColumnDirectory = columnDirectory;
            PatchCount = patchCount;
            Patches = new PatchDescriptor[PatchCount];
        }
    }
}
