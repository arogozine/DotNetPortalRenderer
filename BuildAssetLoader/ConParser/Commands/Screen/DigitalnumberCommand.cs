// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Prints a number to the screen using the same style as the HUD's health/ammo counters.
    /// <c>TileNum</c> is the first tile of a 0-9 digit sequence (e.g. DIGITALNUM or THREEBYFIVE). <c>X</c>/<c>Y</c>
    /// are the draw position, <c>Number</c> is the gamevar holding the value to print, <c>Shade</c>/<c>Pal</c> set
    /// shading/palette, <c>Orientation</c> is the usual drawing bitfield, and <c>X1</c>/<c>Y1</c>/<c>X2</c>/<c>Y2</c>
    /// bound the region the number may be drawn in. Only works during screen-drawing events.</summary>
    [Description("digitalnumber")]
    public sealed record DigitalNumberCommand(
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
        string Y2)
        : Command(CommandList.DigitalNumber);
}
