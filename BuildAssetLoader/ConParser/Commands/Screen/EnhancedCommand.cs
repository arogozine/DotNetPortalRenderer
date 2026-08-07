// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Obsolete CON version compatibility marker; <c>Value</c> is not meaningfully used by EDuke32.</summary>
    [Description("enhanced")]
    public sealed record EnhancedCommand(
        int Value) : Command(CommandList.Enhanced);
}
