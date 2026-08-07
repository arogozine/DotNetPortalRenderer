namespace BuildAssetLoader.Con
{
    // sleeptime <count> — sets the actor's sleep counter.
    public sealed record SleeptimeCommand(string Count) : Command(CommandList.sleeptime);
}

