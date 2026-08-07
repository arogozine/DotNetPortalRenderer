// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // Number is frequently a define (e.g. SQUISHABLEDISTANCE, FROZENQUICKKICKDIST).

    /// <summary>Checks if the actor's distance to the player is greater than Number. As a side effect, if the
    /// actor is farther from the player than MAXSLEEPDIST (16384), the actor is set to "sleeping". See also
    /// <c>ifpdistl</c>.</summary>
    [Description("ifpdistg")]
    public sealed record IfPDistGCommand(
        string Number) : ConditionalStructure(CommandList.IfPDistG);
}
