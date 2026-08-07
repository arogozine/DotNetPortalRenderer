// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Casts a ray from (<c>X1</c>, <c>Y1</c>, <c>Z1</c>) in sector <c>Sect1</c>, travelling along the
    /// direction given by <c>CosAng</c>/<c>SinAng</c> (cosine/sine of the scanning angle) and <c>Zvel</c> (vertical
    /// velocity), and writes what it hits into the six return gamevars: <c>HitSectorVar</c>, <c>HitWallVar</c>,
    /// <c>HitSpriteVar</c> (id of the wall/sprite hit, or -1 if not hit) plus the hit coordinates
    /// <c>HitXVar</c>/<c>HitYVar</c>/<c>HitZVar</c>. <c>ClipMask</c> controls which kinds of objects the scan can
    /// hit.</summary>
    [Description("hitscan")]
    public sealed record HitscanCommand(
        string X1,
        string Y1,
        string Z1,
        string Sect1,
        string CosAng,
        string SinAng,
        string Zvel,
        string HitSectorVar,
        string HitWallVar,
        string HitSpriteVar,
        string HitXVar,
        string HitYVar,
        string HitZVar,
        string ClipMask)
        : Command(CommandList.Hitscan);
}
