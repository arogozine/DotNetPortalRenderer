using BuildAssetLoader.Con;

namespace BuildAssetLoader.ConParser
{
    public abstract record Command(CommandList Start);

    public abstract record Structure(CommandList Start, CommandList End) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    } 

    public record DefineCommand(string Name, int Number) : Command(CommandList.define);

    public record ActionCommand(string Name, int Startframe, int[] Frames, int ViewType, int IncValue, int Delay);

    public record ActorCommand(string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag) : Structure(CommandList.actor, CommandList.enda);
}
