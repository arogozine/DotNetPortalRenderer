// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Activates up to 9 respawn sprites whose lotag matches the current actor's hitag.</summary>
    [Description("respawnhitag")]
    public sealed record RespawnHitagCommand() : Command(CommandList.RespawnHitag);
}
