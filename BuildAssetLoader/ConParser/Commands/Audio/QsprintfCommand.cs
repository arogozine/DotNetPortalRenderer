namespace BuildAssetLoader.Con
{
    // qsprintf <destination quote> <source quote> <parameter 1> [...] [parameter 32] — %d/%ld/%s substitution.
    public sealed record QsprintfCommand(int DestinationQuote, int SourceQuote, string[] Parameters) : Command(CommandList.qsprintf);
}

