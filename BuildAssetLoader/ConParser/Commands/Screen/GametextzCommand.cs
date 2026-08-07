// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>gametext</c>, but with an added <c>TextScale</c> parameter controlling text size
    /// (65536 is full size, 32768 is half size, 131072 is double-size).</summary>
    [Description("gametextz")]
    public sealed record GameTextZCommand(
        string TileNum,
        string X,
        string Y,
        int Quote,
        string Shade,
        string Pal,
        string Orientation,
        string X1,
        string Y1,
        string X2,
        string Y2,
        string TextScale)
        : Command(CommandList.GameTextZ);
}
