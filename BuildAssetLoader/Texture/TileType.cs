namespace BuildAssetLoader.Texture
{
    public sealed class TileType
    {
        public readonly short XSize;
        public readonly short YSize;
        public readonly PropType Properties;
        public readonly byte[] Pixels;

        public TileType(short xSize, short ySize, PropType properties, byte[] pixels)
        {
            XSize = xSize;
            YSize = ySize;
            Properties = properties;
            Pixels = pixels;
        }
    }
}
