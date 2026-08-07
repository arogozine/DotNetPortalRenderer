namespace BuildAssetLoader.Con
{
    public record BaseActorCommand(CommandList Start, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : Structure(Start, CommandList.enda);
}

