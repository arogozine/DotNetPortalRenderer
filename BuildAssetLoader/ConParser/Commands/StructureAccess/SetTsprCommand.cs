// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the tsprite structure (a sprite's per-frame display
    /// properties) for the sprite given by <c>Id</c>. Only usable within EVENT_ANIMATESPRITES on sprites whose
    /// mdflags include flag 16. When <c>Id</c> is omitted it defaults to THISACTOR, the current sprite.</summary>
    [Description("settspr")]
    public sealed record SetTsprCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetTspr);
}
