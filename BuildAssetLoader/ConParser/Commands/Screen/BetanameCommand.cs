namespace BuildAssetLoader.Con
{
    // ===== Deprecated =====

    // betaname <string> — obsolete, never referenced.
    public sealed record BetanameCommand(string Value) : Command(CommandList.betaname);
}

