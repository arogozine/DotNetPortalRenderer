// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Takes the sprite <c>Sprite1</c>'s own xvel/zvel and issues a <c>movesprite</c> with them, using
    /// <c>ClipMask</c> to select collision participants. Convenient for moving an object along its own stored
    /// velocity without recomputing angles.</summary>
    [Description("ssp")]
    public sealed record SspCommand(
        string Sprite1,
        string ClipMask) : Command(CommandList.Ssp);
}
