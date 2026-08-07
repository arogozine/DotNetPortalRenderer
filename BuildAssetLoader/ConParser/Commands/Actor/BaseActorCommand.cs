// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the actor-code block structures (<c>actor</c>/<c>useractor</c>), carrying the
    /// sprite's tile number and the initial strength/action/move/moveflags used when the actor is activated. Not a
    /// CON keyword itself; see <see cref="ActorCommand"/> and <see cref="UserActorCommand"/> for the concrete
    /// commands.</summary>
    public record BaseActorCommand(CommandList Start, string PicNum, string? Stength, string? Action, string? Move, string[]? MoveFlag)
        : Structure(Start, CommandList.Enda);
}
