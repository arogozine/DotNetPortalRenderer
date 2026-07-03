namespace BuildAssetLoader.Con
{
    public abstract record Command(CommandList Start);

    public abstract record Structure(CommandList Start, CommandList End) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    } 

    public record DefineCommand(string Name, int Number) : Command(CommandList.define);

    public record ActionCommand(string Name, int? Startframe, int? Frames, int? ViewType, int? IncValue, int? Delay)
         : Command(CommandList.action);

    public record BaseActorCommand(CommandList Start, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : Structure(Start, CommandList.enda);

    public record ActorCommand(string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.actor, PicNum, Stength, Action, Move, MoveFlag);

    public record UserActorCommand(string Type, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : BaseActorCommand(CommandList.useractor, PicNum, Stength, Action, Move, MoveFlag);

    public sealed record AiCommand(string Name, string? Action, string? Move, string[]? MoveFlag)
        : Command(CommandList.action);

    public sealed record CActor(string Name) : Command(CommandList.cactor);
}
