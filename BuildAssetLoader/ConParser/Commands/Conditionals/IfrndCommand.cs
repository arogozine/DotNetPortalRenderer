namespace BuildAssetLoader.Con
{
    // ifrnd <value> — value in [-1, 255]; -1 always takes else, >=255 always takes if. Commonly a define (e.g. SWEARFREQUENCY).
    public sealed record IfrndCommand(string Value) : ConditionalStructure(CommandList.ifrnd);
}

