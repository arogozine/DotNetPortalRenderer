// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Removes every level entry under Volume, along with their par/designer time definitions and
    /// the volume's own name definition.</summary>
    [Description("undefinevolume")]
    public sealed record UndefineVolumeCommand(
        int Volume) : Command(CommandList.UndefineVolume);
}
