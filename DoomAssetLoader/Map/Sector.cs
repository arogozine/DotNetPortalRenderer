namespace DoomAssetLoader.Map
{
    public readonly struct Sector
    {
        public readonly short FloorHeight;

        public readonly short CeilingHeight;

        public readonly string FloorTexture;

        public readonly string CeilingTexture;
        
        /// <summary>
        /// Light level in this sector (0-255).
        /// </summary>
        public readonly short LightLevel;
        
        public readonly short Special;
        
        public readonly short Tag;

        public Sector(short floorHeight, short ceilingHeight, string floorTexture, string ceilingTexture, short lightLevel, short special, short tag)
        {
            FloorHeight = floorHeight;
            CeilingHeight = ceilingHeight;
            FloorTexture = floorTexture;
            CeilingTexture = ceilingTexture;
            LightLevel = lightLevel;
            Special = special;
            Tag = tag;
        }
    }
}
