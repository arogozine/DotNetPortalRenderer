// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Operates any respawn sprites sharing the given lotag.</summary>
    [Description("operaterespawns")]
    public sealed record OperateRespawnsCommand(string LotagNumber) : Command(CommandList.OperateRespawns);
}
