namespace BuildAssetLoader.Con
{
    // resizearray <array name> <new size>
    public sealed record ResizearrayCommand(string ArrayName, string NewSize) : Command(CommandList.resizearray);
}

