// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks if the actor's distance to the player is less than Number. As a side effect, if the actor
    /// is farther from the player than MAXSLEEPDIST (16384), the actor is set to "sleeping". See also
    /// <c>ifpdistg</c>.</summary>
    [Description("ifpdistl")]
    public sealed record IfPDistLCommand(
        string Number) : ConditionalStructure(CommandList.IfPDistL);
}
