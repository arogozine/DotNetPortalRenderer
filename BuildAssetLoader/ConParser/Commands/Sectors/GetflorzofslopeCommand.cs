// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Returns the z coordinate of the floor at (<c>X</c>, <c>Y</c>) within sector <c>Sectnum</c> into
    /// <c>ReturnVar</c>, accounting for floor slopes. If the sector has no floor slope, this simply returns the
    /// sector's floorz. For accurate results, use in conjunction with <c>updatesector</c> or
    /// <c>updatesectorz</c>.</summary>
    [Description("getflorzofslope")]
    public sealed record GetFlorZOfSlopeCommand(
        string Sectnum,
        string X,
        string Y,
        string ReturnVar) : Command(CommandList.GetFlorZOfSlope);
}
