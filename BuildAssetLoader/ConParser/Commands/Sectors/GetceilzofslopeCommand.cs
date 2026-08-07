// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Returns the z coordinate of the ceiling at (<c>X</c>, <c>Y</c>) within sector <c>Sectnum</c> into
    /// <c>ReturnVar</c>, accounting for ceiling slopes. If the sector has no ceiling slope, this simply returns the
    /// sector's ceilingz. For accurate results, use in conjunction with <c>updatesector</c>.</summary>
    [Description("getceilzofslope")]
    public sealed record GetCeilZOfSlopeCommand(
        string Sectnum,
        string X,
        string Y,
        string ReturnVar) : Command(CommandList.GetCeilZOfSlope);
}
