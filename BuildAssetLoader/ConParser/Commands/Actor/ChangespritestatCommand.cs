// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the statnum of the sprite identified by <c>SpriteId</c> to <c>Statnum</c> (e.g. moving it
    /// between the actor/moveactors and other status lists).</summary>
    [Description("changespritestat")]
    public sealed record ChangeSpriteStatCommand(
        string SpriteId,
        string Statnum) : Command(CommandList.ChangeSpriteStat);
}
