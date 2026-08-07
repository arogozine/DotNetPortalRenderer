// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the previous sprite before <c>CurrentSpriteId</c> in the linked list of sprites sharing the
    /// same sector, storing it into <c>PrevSpriteId</c>. See <c>headspritesect</c> for more information on
    /// iterating a sector's sprite list.</summary>
    [Description("prevspritesect")]
    public sealed record PrevSpriteSectCommand(
        string PrevSpriteId,
        string CurrentSpriteId) : Command(CommandList.PrevSpriteSect);
}
