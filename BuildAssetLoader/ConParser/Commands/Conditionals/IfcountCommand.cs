namespace BuildAssetLoader.Con
{
    // <number> is frequently a define (e.g. SHRUNKDONECOUNT, THAWTIME) rather than a literal.
    public sealed record IfcountCommand(string Number) : ConditionalStructure(CommandList.ifcount);
}

