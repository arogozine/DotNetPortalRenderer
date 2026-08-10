// AI Assisted
namespace DoomAssetLoader.Decorate
{
    public sealed record DecorateToken(DecorateTokenType Type, string Text)
    {
        public static readonly DecorateToken EndOfLine = new(DecorateTokenType.EndOfLine, "\n");
        public static readonly DecorateToken EndOfFile = new(DecorateTokenType.EndOfFile, string.Empty);
    }
}
