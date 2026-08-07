// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Draws HUD text with fine control over layout; the modern replacement for gametext/minitext/
    /// digitalnumber. <c>TileNum</c> is the start of the font's tile range, <c>X</c>/<c>Y</c> the position,
    /// <c>Zoom</c> the scale, <c>BlockAngle</c>/<c>CharacterAngle</c> the block/per-character rotation,
    /// <c>Quote</c> the quote to print, <c>Shade</c>/<c>Pal</c> the shading/palette, <c>Orientation</c> the
    /// drawing bitfield, <c>Alpha</c> the translucence, <c>Xspace</c>/<c>Yline</c>/<c>Xbetween</c>/<c>Ybetween</c>
    /// the character/line spacing, <c>TextFlags</c> a bitfield of text layout options, and <c>X1</c>/<c>Y1</c>/
    /// <c>X2</c>/<c>Y2</c> the drawing bounds.</summary>
    [Description("screentext")]
    public sealed record ScreenTextCommand(
        string TileNum,
        string X,
        string Y,
        string Zoom,
        string BlockAngle,
        string CharacterAngle,
        int Quote,
        string Shade,
        string Pal,
        string Orientation,
        string Alpha,
        string Xspace,
        string Yline,
        string Xbetween,
        string Ybetween,
        string TextFlags,
        string X1,
        string Y1,
        string X2,
        string Y2)
        : Command(CommandList.ScreenText);
}
