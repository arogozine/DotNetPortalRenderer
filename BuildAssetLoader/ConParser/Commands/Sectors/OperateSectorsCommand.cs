// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Called from actor code, triggers the elevator/lift sector given by <c>Sector</c> as if operated by
    /// <c>Actor</c>. The acting actor does not need to be located within the target sector.</summary>
    [Description("operatesectors")]
    public sealed record OperateSectorsCommand(
        string Sector,
        string Actor) : Command(CommandList.OperateSectors);
}
