// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a per-actor custom gamevar (a gamevar flagged per-actor, effectively a member of the actor's
    /// own storage) for the sprite given by <c>Id</c> into <c>Gamevar</c>. When <c>Id</c> is omitted it defaults to
    /// THISACTOR, the current sprite.</summary>
    [Description("getactorvar")]
    public sealed record GetActorVarCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetActorVar);
}
