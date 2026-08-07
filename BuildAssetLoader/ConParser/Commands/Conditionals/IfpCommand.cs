namespace BuildAssetLoader.Con
{
    // ifp <condition1> [<condition2> ...] — each condition is one of the predefined p* flag names
    // (pstanding, pwalking, ...); true if any of them match (logical OR).
    public sealed record IfpCommand(string[] Conditions) : ConditionalStructure(CommandList.ifp);
}

