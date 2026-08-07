namespace BuildAssetLoader.Con
{
    // gamearray <name> <size> <flags>
    public sealed record GamearrayCommand(string Name, int Size, int? Flags) : Command(CommandList.gamearray);
}

