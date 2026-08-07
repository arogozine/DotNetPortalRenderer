namespace BuildAssetLoader.Con
{
    // shadeto <value> — dummy command in EDuke32, does nothing.
    public sealed record ShadetoCommand(string Value) : Command(CommandList.shadeto);
}

