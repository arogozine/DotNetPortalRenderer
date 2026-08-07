// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if there is a line of sight between the current actor and the player.
    /// Unlike <c>ifcanseetarget</c>, this checks whether the player can see any part of the actor's tile, rather
    /// than whether the actor has "eye contact" with the player.</summary>
    [Description("ifcansee")]
    public sealed record IfCanSeeCommand() : ConditionalStructure(CommandList.IfCanSee);
}
