// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: works like <c>rotatesprite</c> but with 65536x more precision — <c>X</c>/<c>Y</c>
    /// range 0-20971520/0-13107200 instead of 0-320/0-200 (i.e. bit-shifted left by 16, hence the "16" in the
    /// name).</summary>
    [Description("rotatesprite16")]
    public sealed record RotateSprite16Command(
        string X,
        string Y,
        string Z,
        string A,
        string TileNum,
        string Shade,
        string Pal,
        string Orientation,
        string X1,
        string Y1,
        string X2,
        string Y2)
        : Command(CommandList.RotateSprite16);
}
