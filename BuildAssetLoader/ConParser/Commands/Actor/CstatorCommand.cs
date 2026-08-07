// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Bitwise-ORs <c>Value</c> into the current sprite's existing cstat bitfield instead of replacing it,
    /// making it convenient to add flags on top of whatever cstat is already set. See also <c>cstat</c>.</summary>
    [Description("cstator")]
    public sealed record CStatOrCommand(string Value) : Command(CommandList.CStatOr);
}
