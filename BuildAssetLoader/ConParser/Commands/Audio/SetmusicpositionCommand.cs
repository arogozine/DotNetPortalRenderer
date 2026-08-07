namespace BuildAssetLoader.Con
{
    // setmusicposition <gamevar> — implementation-specific, discouraged.
    public sealed record SetmusicpositionCommand(string Gamevar) : Command(CommandList.setmusicposition);
}

