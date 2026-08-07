namespace BuildAssetLoader.Con
{
    // setplayerangle <gamevar> — deprecated; sets the current player's angle.
    public sealed record SetplayerangleCommand(string Gamevar) : Command(CommandList.setplayerangle);
}

