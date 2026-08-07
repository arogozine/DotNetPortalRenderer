namespace BuildAssetLoader.Con
{
    // tip — disables the current/closest player's weapon and shows the "tip" graphic.
    public sealed record TipCommand() : Command(CommandList.tip);
}

