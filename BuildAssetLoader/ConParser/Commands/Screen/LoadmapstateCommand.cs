// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Hub Maps =====

    /// <summary>Restores the current map to its last <c>savemapstate</c> snapshot.</summary>
    [Description("loadmapstate")]
    public sealed record LoadMapStateCommand() : Command(CommandList.LoadMapState);
}
