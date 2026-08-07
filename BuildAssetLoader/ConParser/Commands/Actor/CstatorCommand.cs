namespace BuildAssetLoader.Con
{
    // cstator <number> — bitwise-ORs <number> into the sprite's existing cstat.
    public sealed record CstatorCommand(string Value) : Command(CommandList.cstator);
}

