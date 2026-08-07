// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Gets the current actor's hitag into the per-actor gamevar HITAG.</summary>
    [Description("spgethitag")]
    public sealed record SpGetHitagCommand() : Command(CommandList.SpGetHitag);
}
