namespace BuildAssetLoader.Con
{
    public record ConToken(ConTokenType ConTokenType)
    {
        public static readonly ConToken BlockStart = new(ConTokenType.BlockStart);
        public static readonly ConToken BlockEnd = new(ConTokenType.BlockEnd);
        public static readonly ConToken NewLine = new(ConTokenType.NewLine);
    }
}
