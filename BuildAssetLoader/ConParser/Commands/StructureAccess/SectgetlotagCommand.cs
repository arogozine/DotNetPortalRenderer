// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Takes the lotag of the current actor's sector and places it into the
    /// per-actor gamevar LOTAG.</summary>
    [Description("sectgetlotag")]
    public sealed record SectGetLotagCommand() : Command(CommandList.SectGetLotag);
}
