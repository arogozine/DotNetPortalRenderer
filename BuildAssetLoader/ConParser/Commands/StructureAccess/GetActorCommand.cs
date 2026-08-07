// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Structure Access (Getters & Setters) =====
    // get<struct>[<id>].<member> <gamevar> / set<struct>[<id>].<member> <value>
    // <id> may be omitted, implying THISACTOR. "...var" siblings access per-<struct> custom gamevar members.
    // One pair per struct: actor, actorvar, input, player, playervar, projectile, sector, thisprojectile, tspr, userdef, wall.

    /// <summary>Reads a member of the actor (sprite/hittype) structure for the sprite given by <c>Id</c> into
    /// <c>Gamevar</c>. When <c>Id</c> is omitted it defaults to THISACTOR, the current sprite.</summary>
    [Description("getactor")]
    public sealed record GetActorCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetActor);
}
