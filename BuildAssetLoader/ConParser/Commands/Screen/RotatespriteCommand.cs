// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Screen Drawing =====

    /// <summary>Displays a tile onscreen; the general-purpose successor to the deprecated myos family. <c>X</c>/
    /// <c>Y</c> set the position (normally 0-320/0-200), <c>Zoom</c> the scale (65536 is normal size),
    /// <c>Ang</c> the rotation angle (2048 units per 360 degrees), <c>TileNum</c> the tile, <c>Shade</c>/<c>Pal</c>
    /// the shading/palette, <c>Orientation</c> a bitfield for translucency/masking/etc, and <c>X1</c>/<c>Y1</c>/
    /// <c>X2</c>/<c>Y2</c> the screen bounds the tile may be drawn within.</summary>
    [Description("rotatesprite")]
    public sealed record RotateSpriteCommand(
        string X,
        string Y,
        string Zoom,
        string Ang,
        string TileNum,
        string Shade,
        string Pal,
        string Orientation,
        string X1,
        string Y1,
        string X2,
        string Y2)
        : Command(CommandList.RotateSprite);
}
