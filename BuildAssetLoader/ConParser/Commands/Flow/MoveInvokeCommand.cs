namespace BuildAssetLoader.Con
{
    // move <name> <moveflags...> — invocation, used inside actor code.
    public sealed record MoveInvokeCommand(string Name, string[]? MoveFlag) : Command(CommandList.move);
}

