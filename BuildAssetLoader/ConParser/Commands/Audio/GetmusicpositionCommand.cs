namespace BuildAssetLoader.Con
{
    // getmusicposition <gamevar> — implementation-specific, discouraged.
    public sealed record GetmusicpositionCommand(string Gamevar) : Command(CommandList.getmusicposition);
}

