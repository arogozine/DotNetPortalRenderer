// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Tests whether there is a clear, unobstructed line of sight between point (<c>X1</c>, <c>Y1</c>,
    /// <c>Z1</c>) in sector <c>Sect1</c> and point (<c>X2</c>, <c>Y2</c>, <c>Z2</c>) in sector <c>Sect2</c>, writing 1
    /// to <c>ReturnVar</c> if visible or 0 if blocked by a sector, wall, or sprite.</summary>
    [Description("cansee")]
    public sealed record CanSeeCommand(
        string X1,
        string Y1,
        string Z1,
        string Sect1,
        string X2,
        string Y2,
        string Z2,
        string Sect2,
        string ReturnVar)
        : Command(CommandList.CanSee);
}
