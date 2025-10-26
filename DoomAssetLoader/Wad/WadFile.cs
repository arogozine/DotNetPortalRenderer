namespace DoomAssetLoader.Wad
{
    public sealed class WadFile
    {
        public WadType Type { get; set; }

        public List<WadLump> Lumps { get; } = [];

        public WadLump? this[string lumpName]
        {
            get
            {
                foreach (WadLump wadLump in Lumps)
                {
                    if (wadLump.Name.Equals(lumpName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return wadLump;
                    }
                }

                return default;
            }
        }

        public WadLump? GetMapLump(string mapName, string lumpName)
        {
            foreach (WadLump wadLump in Lumps)
            {
                if (
                    wadLump.IsMap &&
                    wadLump.Name.Equals(lumpName, StringComparison.InvariantCultureIgnoreCase) &&
                    wadLump.MapName!.Equals(mapName, StringComparison.InvariantCultureIgnoreCase)
                    )
                {
                    return wadLump;
                }
            }

            return default;
        }

        public WadFile LoadRequired(WadFile wadFile)
        {
            if (this[LumpType.ColorMap] is null && wadFile[LumpType.ColorMap] is WadLump lump)
            {
                Lumps.Add(lump);
            }

            Lumps.AddRange(wadFile.Lumps.Where(x => x.IsPatch));
            return this;
        }
    }
}
