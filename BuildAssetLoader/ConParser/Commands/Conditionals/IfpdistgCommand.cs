namespace BuildAssetLoader.Con
{
    // <number> is frequently a define (e.g. SQUISHABLEDISTANCE, FROZENQUICKKICKDIST).
    public sealed record IfpdistgCommand(string Number) : ConditionalStructure(CommandList.ifpdistg);
}

