namespace BuildAssetLoader.Con
{
    public record CommandToken(CommandList Command) : ConToken(ConTokenType.Command);
}
