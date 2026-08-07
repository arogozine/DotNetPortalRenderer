// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Makes the current player perform a kick.</summary>
    [Description("pkick")]
    public sealed record PKickCommand() : Command(CommandList.PKick);
}
