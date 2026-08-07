namespace BuildAssetLoader.Con
{
    public sealed record IfactioncountCommand(string Number) : ConditionalStructure(CommandList.ifactioncount);
}

