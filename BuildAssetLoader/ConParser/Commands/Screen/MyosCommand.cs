// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Screen Drawing (deprecated myos family) =====

    /// <summary>Deprecated, older and more limited version of <c>rotatesprite</c>; displays a tile at 320x200
    /// resolution. <c>X</c>/<c>Y</c> set the position, <c>TileNum</c> the tile, <c>Shade</c> the shading, and
    /// <c>Orientation</c> the usual drawing bitfield (translucency/masking/etc).</summary>
    [Description("myos")]
    public sealed record MyosCommand(
        string X,
        string Y,
        string TileNum,
        string Shade,
        string Orientation) : Command(CommandList.Myos);
}
