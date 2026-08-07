namespace BuildAssetLoader.Con
{
    public sealed record IfpdistlCommand(string Number) : ConditionalStructure(CommandList.ifpdistl);
}

