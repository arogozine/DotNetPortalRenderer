// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>digitalnumber</c>, but with an added <c>DigitalScale</c> parameter controlling the size of
    /// the printed text on screen (65536 is normal size, 32768 is half-size, 131072 is double-size).</summary>
    [Description("digitalnumberz")]
    public sealed record DigitalNumberZCommand(
        string TileNum,
        string X,
        string Y,
        string Number,
        string Shade,
        string Pal,
        string Orientation,
        string X1,
        string Y1,
        string X2,
        string Y2,
        string DigitalScale)
        : Command(CommandList.DigitalNumberZ);
}
