namespace BuildAssetLoader.Con
{
    // state <name> — invocation, runs a previously-defined state's code inline.
    public sealed record StateInvokeCommand(string Name) : Command(CommandList.state);
}

