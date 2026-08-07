// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stores the id of the first sprite in the linked list of sprites sharing statnum <c>Statnum</c> into
    /// <c>Sprite</c>, for scanning through all sprites of a certain type. Use <c>nextspritestat</c>/
    /// <c>prevspritestat</c> to continue walking the list. See also <c>headspritesect</c>, which is identical but
    /// keyed by sectnum instead of statnum.</summary>
    [Description("headspritestat")]
    public sealed record HeadSpriteStatCommand(
        string Sprite,
        string Statnum) : Command(CommandList.HeadSpriteStat);
}
