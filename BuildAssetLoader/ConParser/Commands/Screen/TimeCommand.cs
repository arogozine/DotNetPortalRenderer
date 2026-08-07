namespace BuildAssetLoader.Con
{
    // time <gamevar> — compiles but does nothing, like nullop.
    public sealed record TimeCommand(string Gamevar) : Command(CommandList.time);
}

