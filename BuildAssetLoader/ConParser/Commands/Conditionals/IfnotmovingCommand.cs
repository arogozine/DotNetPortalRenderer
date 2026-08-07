// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the current actor has stopped moving. Mostly useful for detecting whether an actor has
    /// hit a wall.</summary>
    [Description("ifnotmoving")]
    public sealed record IfNotMovingCommand() : ConditionalStructure(CommandList.IfNotMoving);
}
