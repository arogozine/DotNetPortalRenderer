namespace BuildAssetLoader.Con
{
    public record UserActorCommand(string Type, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.useractor, PicNum, Stength, Action, Move, MoveFlag);
}

