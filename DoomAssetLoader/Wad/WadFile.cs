namespace DoomAssetLoader.Wad
{
    public sealed class WadFile
    {
        public WadType Type { get; set; }

        public List<WadLump> Lumps { get; } = [];

        public WadLump? this[string wadName]
        {
            get
            {
                foreach (WadLump wadLump in Lumps)
                {
                    if (wadLump.Name.Equals(wadName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return wadLump;
                    }
                }

                return default;
            }
        }
    }
}
