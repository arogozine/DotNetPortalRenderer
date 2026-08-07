// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Meta-Settings =====

    /// <summary>Lone-keyword primitive that enables the dynamic tile remapping system, letting subsequent
    /// <c>define</c> statements relocate hard-coded tiles to new positions. Must appear at the very start of
    /// the root CON file, outside any actor/state/event block.</summary>
    [Description("dynamicremap")]
    public sealed record DynamicRemapCommand() : Command(CommandList.DynamicRemap);
}
