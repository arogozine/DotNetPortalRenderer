namespace BuildAssetLoader.Con
{
    // getplayerangle <gamevar> — deprecated; gets the current player's angle.
    public sealed record GetplayerangleCommand(string Gamevar) : Command(CommandList.getplayerangle);
}

