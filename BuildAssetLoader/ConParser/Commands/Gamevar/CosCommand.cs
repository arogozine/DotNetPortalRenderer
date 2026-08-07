namespace BuildAssetLoader.Con
{
    // cos <gamevar> <gamevar2> — gamevar = cos(gamevar2), scaled to a hypotenuse of 16384.
    public sealed record CosCommand(string Gamevar, string Angle) : Command(CommandList.cos);
}

