namespace BuildAssetLoader.Con
{
    // addkills <number> — adds to the closest player's kill score; also clears the current actor's stayput flag.
    public sealed record AddkillsCommand(string Number) : Command(CommandList.addkills);
}

