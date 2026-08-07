// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Displays onscreen the in-game view from coordinates <c>X</c>/<c>Y</c>/<c>Z</c> in sector
    /// <c>Sector</c>, looking at <c>Angle</c> with vertical look <c>Horiz</c>, drawn between screen coordinates
    /// (<c>ScrnX1</c>,<c>ScrnY1</c>) and (<c>ScrnX2</c>,<c>ScrnY2</c>). Screen coordinates are biased towards zero
    /// so the full screen can only be covered at 320x200 (see <c>showviewunbiased</c> for the alternative
    /// scaling).</summary>
    [Description("showview")]
    public sealed record ShowViewCommand(
        string X,
        string Y,
        string Z,
        string Angle,
        string Horiz,
        string Sector,
        string ScrnX1,
        string ScrnY1,
        string ScrnX2,
        string ScrnY2)
        : Command(CommandList.ShowView);
}
