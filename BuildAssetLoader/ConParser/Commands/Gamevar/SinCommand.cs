namespace BuildAssetLoader.Con
{
    // sin <gamevar> <gamevar2> — gamevar = sin(gamevar2), scaled to a hypotenuse of 16384.
    public sealed record SinCommand(string Gamevar, string Angle) : Command(CommandList.sin);
}

