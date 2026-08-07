namespace BuildAssetLoader.Con
{
    // lockplayer <gamevar> — freezes player movement for <gamevar> tics.
    public sealed record LockplayerCommand(string Gamevar) : Command(CommandList.lockplayer);
}

