namespace BuildAssetLoader.Con
{
    // ===== Array Operations =====

    // getarraysize <array name> <return>
    public sealed record GetarraysizeCommand(string ArrayName, string ReturnVar) : Command(CommandList.getarraysize);
}

