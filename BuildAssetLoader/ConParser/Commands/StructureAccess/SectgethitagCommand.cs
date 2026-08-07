// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Takes the hitag of the current actor's sector and places it into the
    /// per-actor gamevar HITAG.</summary>
    [Description("sectgethitag")]
    public sealed record SectGetHitagCommand() : Command(CommandList.SectGetHitag);
}
