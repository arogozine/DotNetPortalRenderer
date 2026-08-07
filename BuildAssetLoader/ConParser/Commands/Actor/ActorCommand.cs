namespace BuildAssetLoader.Con
{
    public record ActorCommand(string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.actor, PicNum, Stength, Action, Move, MoveFlag);
}

