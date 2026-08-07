// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: prints a defined quote to the screen. <c>TileNum</c> is the first tile of a character
    /// sequence, <c>X</c>/<c>Y</c> the draw position (X is halved, so x=319 starts at screen center), <c>Quote</c>
    /// the quote id to print, <c>Shade</c>/<c>Pal</c> shading/palette, <c>Orientation</c> the usual drawing
    /// bitfield, and <c>X1</c>/<c>Y1</c>/<c>X2</c>/<c>Y2</c> the drawing bounds. Only works during screen-drawing
    /// events.</summary>
    [Description("gametext")]
    public sealed record GameTextCommand(
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
        string Y2)
        : Command(CommandList.GameText);
}
