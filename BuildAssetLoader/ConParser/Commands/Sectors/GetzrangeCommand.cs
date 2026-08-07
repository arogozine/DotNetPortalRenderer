// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>From a starting point (<c>X</c>, <c>Y</c>, <c>Z</c>) in sector <c>Sector</c>, casts a quadratic,
    /// floor-aligned sprite of side length <c>2 * WallDist</c> straight up and down to find what it would first
    /// hit. Writes the hit z coordinates into <c>CeilingZ</c>/<c>FloorZ</c> and encoded hit sector/sprite ids into
    /// <c>CeilingHit</c>/<c>FloorHit</c> (16384 + sectnum for a sector/floor hit, 49152 + spritenum for a sprite
    /// hit). <c>ClipMask</c> controls which kinds of objects can be hit, using the same encoding as
    /// <c>hitscan</c>.</summary>
    [Description("getzrange")]
    public sealed record GetZRangeCommand(
        string X,
        string Y,
        string Z,
        string Sector,
        string CeilingZ,
        string CeilingHit,
        string FloorZ,
        string FloorHit,
        string WallDist,
        string ClipMask)
        : Command(CommandList.GetZRange);
}
