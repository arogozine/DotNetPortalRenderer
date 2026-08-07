// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the current sprite's cstat bitfield to <c>Value</c> (commonly built from the CSTAT_SPRITE_*
    /// defines, e.g. blockable, translucent, x/y-flip, wall/floor-aligned). See also <c>cstator</c>, which ORs bits
    /// in instead of replacing the field.</summary>
    [Description("cstat")]
    public sealed record CStatCommand(string Value) : Command(CommandList.CStat);
}
