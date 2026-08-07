// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the actor (sprite/hittype) structure for the sprite given by
    /// <c>Id</c>. When <c>Id</c> is omitted it defaults to THISACTOR, the current sprite.</summary>
    [Description("setactor")]
    public sealed record SetActorCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetActor);
}
