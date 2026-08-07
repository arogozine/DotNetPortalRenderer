// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the next sprite after <c>CurrentSpriteId</c> in the linked list of sprites sharing the same
    /// sector, storing it into <c>NextSpriteId</c>. See <c>headspritesect</c> for more information on iterating a
    /// sector's sprite list.</summary>
    [Description("nextspritesect")]
    public sealed record NextSpriteSectCommand(
        string NextSpriteId,
        string CurrentSpriteId) : Command(CommandList.NextSpriteSect);
}
