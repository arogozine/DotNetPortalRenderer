// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Gets the current actor's lotag into the per-actor gamevar LOTAG.</summary>
    [Description("spgetlotag")]
    public sealed record SpGetLotagCommand() : Command(CommandList.SpGetLotag);
}
