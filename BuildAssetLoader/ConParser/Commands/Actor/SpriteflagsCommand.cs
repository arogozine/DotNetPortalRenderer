// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets SFLAG_* behavior flags to <c>Value</c>. Outside actor code, with <c>PicNum</c> given, it sets
    /// the per-tile flags for that tile (equivalent to gameflags); inside actor code, with <c>PicNum</c> omitted, it
    /// sets the per-sprite flags for the current sprite (equivalent to htflags). At runtime the per-tile and
    /// per-sprite values are XORed together, so setting one can reverse the other.</summary>
    [Description("spriteflags")]
    public sealed record SpriteFlagsCommand(
        string? PicNum,
        string Value) : Command(CommandList.SpriteFlags);
}
