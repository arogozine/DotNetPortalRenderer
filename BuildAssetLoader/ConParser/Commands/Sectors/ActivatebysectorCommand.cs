// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Triggers all ACTIVATOR sprites contained in the given sector; if none are found, activates the
    /// effect defined by that sector's tag instead. Unlike <c>operateactivators</c> it does not trigger
    /// ACTIVATORLOCKED sprites. <c>SpriteNum</c> is only used in the tag-fallback case, where its meaning depends
    /// on the sector's tag (e.g. sound origin sprite, or the player sprite to transport for a warp elevator).</summary>
    [Description("activatebysector")]
    public sealed record ActivateBySectorCommand(
        string SectNum,
        string SpriteNum) : Command(CommandList.ActivateBySector);
}
