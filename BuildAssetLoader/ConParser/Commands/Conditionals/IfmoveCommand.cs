namespace BuildAssetLoader.Con
{
    public sealed record IfmoveCommand(string Move) : ConditionalStructure(CommandList.ifmove);
}

