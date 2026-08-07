// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Snapshots the current map's state for a later <c>loadmapstate</c>.</summary>
    [Description("savemapstate")]
    public sealed record SaveMapStateCommand() : Command(CommandList.SaveMapState);
}
