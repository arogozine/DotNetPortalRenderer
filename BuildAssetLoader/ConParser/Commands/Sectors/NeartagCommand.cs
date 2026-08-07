// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the nearest sector, wall, and sprite whose hitag and/or lotag are set, searching outward from
    /// starting position (<c>X</c>, <c>Y</c>, <c>Z</c>) in sector <c>Sect</c> at angle <c>Ang</c>, up to distance
    /// <c>NearTagRange</c>. Writes the found ids into <c>NearTagSector</c>/<c>NearTagWall</c>/<c>NearTagSprite</c>
    /// and the actual distance into <c>NearTagHitDist</c>. <c>TagSearch</c> selects which tags to match: 1 for lotag
    /// only, 2 for hitag only, 3 for both.</summary>
    [Description("neartag")]
    public sealed record NearTagCommand(
        string X,
        string Y,
        string Z,
        string Sect,
        string Ang,
        string NearTagSector,
        string NearTagWall,
        string NearTagSprite,
        string NearTagHitDist,
        string NearTagRange,
        string TagSearch)
        : Command(CommandList.NearTag);
}
