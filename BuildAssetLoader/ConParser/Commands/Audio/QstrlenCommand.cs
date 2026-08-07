namespace BuildAssetLoader.Con
{
    // qstrlen <gamevar> <quote> — measures a quote's length into a gamevar.
    public sealed record QstrlenCommand(string Gamevar, int Quote) : Command(CommandList.qstrlen);
}

