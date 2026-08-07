// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Moves the sprite identified by <c>SpriteId</c> along velocity (<c>XVel</c>, <c>YVel</c>, <c>ZVel</c>)
    /// using <c>ClipMask</c> to select what it collides with. <c>ReturnVar</c> receives htmovflag: 0 if nothing was
    /// hit, otherwise the id of what was hit (add 16384 to distinguish a sprite hit). Note that <c>ZVel</c> tends to
    /// require <c>fall</c> to be called immediately beforehand, and mixing this with <c>move</c> is unreliable.</summary>
    [Description("movesprite")]
    public sealed record MoveSpriteCommand(
        string SpriteId,
        string XVel,
        string YVel,
        string ZVel,
        string ClipMask,
        string ReturnVar)
        : Command(CommandList.MoveSprite);
}
