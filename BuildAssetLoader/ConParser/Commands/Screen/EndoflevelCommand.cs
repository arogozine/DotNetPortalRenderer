namespace BuildAssetLoader.Con
{
    public sealed record EndoflevelCommand(int Number) : BaseEndofgameCommand(CommandList.endoflevel, Number);
}

