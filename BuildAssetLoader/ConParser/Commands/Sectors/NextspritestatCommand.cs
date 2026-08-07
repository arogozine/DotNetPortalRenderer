// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the next sprite after <c>CurrentSpriteId</c> in the linked list of sprites sharing the same
    /// statnum, storing it into <c>NextSpriteId</c>. See <c>headspritestat</c> for more information on iterating a
    /// statnum's sprite list.</summary>
    [Description("nextspritestat")]
    public sealed record NextSpriteStatCommand(
        string NextSpriteId,
        string CurrentSpriteId) : Command(CommandList.NextSpriteStat);
}
