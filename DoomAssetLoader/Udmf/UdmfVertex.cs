namespace DoomAssetLoader.Udmf
{
    public sealed class UdmfVertex : UdmfObject
    {
        public const string POSITION_X = "x";
        public const string POSITION_Y = "y";

        public float X => GetRequiredValue<float>(POSITION_X);
        public float Y => GetRequiredValue<float>(POSITION_Y);
    }
}
