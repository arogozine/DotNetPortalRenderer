namespace BuildAssetLoader.Con
{
    public sealed record IfstrengthCommand(string Strength) : ConditionalStructure(CommandList.ifstrength);
}

