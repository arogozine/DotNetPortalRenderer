namespace BuildAssetLoader.Con
{
    // setactorangle <gamevar> — deprecated; sets the current actor's angle.
    public sealed record SetactorangleCommand(string Gamevar) : Command(CommandList.setactorangle);
}

