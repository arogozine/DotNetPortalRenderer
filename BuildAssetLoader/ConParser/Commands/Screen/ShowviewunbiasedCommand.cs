// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>showview</c>, but the screen coordinates are scaled so the greatest permissible normalized
    /// value maps to the greatest actual screen coordinate, allowing the whole screen to be covered regardless of
    /// resolution.</summary>
    [Description("showviewunbiased")]
    public sealed record ShowViewUnbiasedCommand(
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
        : Command(CommandList.ShowViewUnbiased);
}
