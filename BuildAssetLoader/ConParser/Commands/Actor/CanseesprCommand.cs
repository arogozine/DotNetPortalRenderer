// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Tests whether the sprite with ID <c>SpriteId1</c> has a line of sight to the sprite with ID
    /// <c>SpriteId2</c> (checked from the base of each sprite), writing 1 to <c>ReturnVar</c> if visible or 0
    /// otherwise.</summary>
    [Description("canseespr")]
    public sealed record CanSeeSprCommand(
        string SpriteId1,
        string SpriteId2,
        string ReturnVar) : Command(CommandList.CanSeeSpr);
}
