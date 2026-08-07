// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stores the id of the first sprite in the linked list of sprites belonging to sector <c>Sect</c>
    /// into <c>Sprite</c>, for scanning through all sprites in that sector. Use <c>nextspritesect</c>/
    /// <c>prevspritesect</c> to continue walking the list. See also <c>headspritestat</c>, which is identical but
    /// keyed by statnum instead of sectnum.</summary>
    [Description("headspritesect")]
    public sealed record HeadSpriteSectCommand(
        string Sprite,
        string Sect) : Command(CommandList.HeadSpriteSect);
}
