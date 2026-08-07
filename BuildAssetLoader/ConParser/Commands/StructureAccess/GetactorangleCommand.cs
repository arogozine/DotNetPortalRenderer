namespace BuildAssetLoader.Con
{
    // ===== Single-Use Structure Access (deprecated shortcuts, no struct-index syntax) =====

    // getactorangle <gamevar> — deprecated; gets the current actor's angle.
    public sealed record GetactorangleCommand(string Gamevar) : Command(CommandList.getactorangle);
}

