// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the previous sprite before <c>CurrentSpriteId</c> in the linked list of sprites sharing the
    /// same statnum, storing it into <c>PrevSpriteId</c>. See <c>headspritestat</c> for more information on
    /// iterating a statnum's sprite list.</summary>
    [Description("prevspritestat")]
    public sealed record PrevSpriteStatCommand(
        string PrevSpriteId,
        string CurrentSpriteId) : Command(CommandList.PrevSpriteStat);
}
