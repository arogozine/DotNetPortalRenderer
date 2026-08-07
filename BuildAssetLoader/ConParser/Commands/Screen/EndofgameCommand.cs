namespace BuildAssetLoader.Con
{
    public sealed record EndofgameCommand(int Number) : BaseEndofgameCommand(CommandList.endofgame, Number);
}

