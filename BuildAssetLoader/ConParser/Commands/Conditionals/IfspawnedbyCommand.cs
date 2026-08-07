// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor was spawned by the given actor. If the actor
    /// wasn't spawned by another actor and was instead loaded from the map, this holds its own tile number. No
    /// longer reliable after the actor has taken damage, since the same storage is reused for the tile number of
    /// whatever caused the damage; in that case this is equivalent to <c>ifwasweapon</c>.</summary>
    [Description("ifspawnedby")]
    public sealed record IfSpawnedByCommand(
        string Actor) : ConditionalStructure(CommandList.IfSpawnedBy);
}
