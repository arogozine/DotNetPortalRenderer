// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Calculates the on-screen dimensions that an equivalent <c>screentext</c> call would occupy, writing
    /// the results into the <c>WidthReturnVar</c>/<c>HeightReturnVar</c> gamevars. The remaining parameters mirror
    /// <c>screentext</c>'s layout parameters: <c>TileNum</c> is the start of the font tile range, <c>X</c>/<c>Y</c>
    /// are the draw position, <c>Zoom</c> scales the text, <c>BlockAngle</c> rotates the whole block, <c>Quote</c>
    /// is the text to measure, <c>Orientation</c> is a rotatesprite-style bitfield, <c>Xspace</c>/<c>Yline</c> are
    /// the space-character width and empty-line height, <c>Xbetween</c>/<c>Ybetween</c> are the gaps between
    /// characters/lines, <c>TextFlags</c> is a bitfield of text layout options, and <c>X1</c>/<c>Y1</c>/<c>X2</c>/
    /// <c>Y2</c> bound the region the text may be drawn within.</summary>
    [Description("qstrdim")]
    public sealed record QStrDimCommand(
        string WidthReturnVar,
        string HeightReturnVar,
        string TileNum,
        string X,
        string Y,
        string Zoom,
        string BlockAngle,
        int Quote,
        string Orientation,
        string XSpace,
        string YLine,
        string XBetween,
        string YBetween,
        string TextFlags,
        string X1,
        string Y1,
        string X2,
        string Y2)
        : Command(CommandList.QStrDim);
}
