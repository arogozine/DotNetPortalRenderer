namespace BuildAssetLoader.Con
{
    // money <number> — spawns dollar bills at the current actor.
    public sealed record MoneyCommand(string Number) : Command(CommandList.money);
}

