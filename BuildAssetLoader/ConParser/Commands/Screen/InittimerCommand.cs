namespace BuildAssetLoader.Con
{
    // inittimer <rate> — changes gameplay speed (default 120); usable for "bullet time" effects.
    public sealed record InittimerCommand(int Rate) : Command(CommandList.inittimer);
}

