namespace BuildAssetLoader.Con
{
    public sealed record ValueToken(string Value) : ConToken(ConTokenType.Value);
}
